using MilkBot;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;  // для Application.Restart

public class ConnectionMonitor : IDisposable
{
    private readonly TelegramBotService _botService;
    private readonly string _errorLogPath;
    private readonly string _offlineLogPath;
    private System.Threading.Timer _timer;
    private bool _isConnected;            // Текущее состояние связи
    private bool _restartTriggered = false; // Флаг, что рестарт уже выполнялся (чтобы не повторять)
    private readonly object _checkLock = new object(); // для защиты от одновременных проверок

    // Конструктор класса. Пути к логам можно передать явно или использовать значения по умолчанию.
    public ConnectionMonitor(TelegramBotService botService, string errorLogPath = "bot_errors.log", string offlineLogPath = "offline_errors.log")
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _errorLogPath = errorLogPath;
        _offlineLogPath = offlineLogPath;
        _isConnected = true;  // Предполагаем, что при старте приложения связь есть
    }

    // Метод запуска мониторинга
    public void Start(int checkIntervalMs = 10000)
    {
        // Инициализируем таймер, если еще не создан
        if (_timer == null)
        {
            // TimerCallback делегат на метод CheckConnection
            _timer = new System.Threading.Timer(CheckConnection, null, 0, checkIntervalMs);
        }
    }

    // Метод остановки мониторинга
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    // Основной метод, вызываемый таймером, для проверки соединения
    private void CheckConnection(object state)
    {
        // Защита от параллельного выполнения: не позволим двум потокам одновременно войти
        if (!Monitor.TryEnter(_checkLock))
            return; // предыдущая проверка еще не завершилась, пропускаем эту итерацию
        try
        {
            bool reachable = IsTelegramReachable();
            if (reachable && !_isConnected)
            {
                // Соединение восстановилось после периода офлайна
                _isConnected = true;
                _restartTriggered = false; // сбрасываем флаг рестарта, можно снова использовать при следующем отключении
                // Прочитать накопленные ошибки из offline_errors.log
                if (File.Exists(_offlineLogPath))
                {
                    string allErrors = File.ReadAllText(_offlineLogPath);
                    if (!string.IsNullOrEmpty(allErrors))
                    {
                        // Обрезаем сообщение до 3500 символов, если оно длиннее
                        string messageToAdmin = allErrors;
                        if (messageToAdmin.Length > 3500)
                        {
                            messageToAdmin = messageToAdmin.Substring(messageToAdmin.Length - 3500);
                        }
                        // Отправляем администратору через сервис бота
                        try
                        {
                            _botService.SendMessageToAdmin($"⚠️ Отчет об ошибках за время офлайна:\n{messageToAdmin}");
                        }
                        catch (Exception ex)
                        {
                            // Если не удалось отправить сообщение админу, логируем в основной файл
                            File.AppendAllText(_errorLogPath, $"{DateTime.Now}: Не удалось отправить сообщение админу: {ex}\n");
                        }
                    }
                    // Удаляем временный файл с ошибками
                    try { File.Delete(_offlineLogPath); } catch { /* игнорируем ошибки удаления */ }
                }
                // (Опционально можно залогировать восстановление связи в bot_errors.log)
            }
            else if (!reachable && _isConnected)
            {
                // Соединение только что потеряно
                _isConnected = false;
                // Записываем в основной лог сообщение о потере связи
                string logEntry = $"{DateTime.Now}: 🛑 Связь с Telegram потеряна\n";
                File.AppendAllText(_errorLogPath, logEntry);
                // После потери связи начинаем дублировать ошибки в offline_errors.log.
                // (Предполагается, что остальная часть приложения вызывает логирование ошибок, 
                // и мы можем тут либо переключить механизм логирования, либо просто пометить флаг. 
                // Для простоты можно считать, что другие части программы сами проверяют состояние соединения 
                // через ConnectionMonitor.IsConnected или схожий флаг, и при false пишут ошибки в offline_errors.log.)
                // Перезапускаем приложение (только один раз, если еще не перезапускали)
                if (!_restartTriggered)
                {
                    _restartTriggered = true;
                    Application.Restart();
                    // После этого строки кода ниже, скорее всего, не выполнятся, так как процесс перезапустится.
                }
            }
            // Если reachable == _isConnected (оба true или оба false), значит состояние не изменилось:
            // просто продолжаем (в офлайне продолжаем накапливать ошибки, онлайн — все нормально).
        }
        finally
        {
            Monitor.Exit(_checkLock);
        }
    }


    // Метод проверки доступности Telegram API
    private bool IsTelegramReachable()
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                // Отправляем HTTP GET запрос к базовому URL Telegram API
                var response = client.GetAsync("https://api.telegram.org").Result;
                // Если удалось получить ответ (даже 404), значит до сервера достучались
                return true;
            }
        }
        catch
        {
            // Любые ошибки (таймаут, отсутствие сети, DNS и пр.) трактуем как недоступность
            return false;
        }
    }

    // Реализация IDisposable для безопасной остановки таймера
    public void Dispose()
    {
        Stop();
    }

    // Дополнительно можно свойство только для чтения, указывающее статус соединения:
    public bool IsConnected => _isConnected;
}
