using Microsoft.Win32;
using MilkBot.TelegramMarkup;
using System;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TelegramBotWindowsService.Service
{
    public class BotConfig
    {
        public string Token { get; set; } = string.Empty;
        public long AdminId { get; set; }
        public decimal CartonAmount { get; set; } = 1.0m;
        public bool AutoStart { get; set; } = false;
    }

    public class TelegramBotService
    {
        private static readonly string CONFIG_FILE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot_config.json");
        private const string REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string SERVICE_NAME = "TelegramMilkBot";
        private const string SC_EXE = "sc.exe";
        private const string CALLBACK_LOG_FILE = "callback_log.json";
        private const int CALLBACK_EXPIRY_HOURS = 48; // Telegram callback expiry time

        private readonly ITelegramBotClient _botClient;
        private readonly long _adminId;
        private readonly decimal _cartonAmount;
        private CancellationTokenSource _cts;
        private bool _wasRestartedOnce = false;
        private bool _isManualRestarting = false;
        private string _editTargetUserId;
        private long _editAdminUserId;
        private int? _lastAdminMessageId;
        private string _lastAdminMessageText;
        private readonly Dictionary<string, DateTime> _lastBuyTime = new();
        private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(1);
        private string _lastHandledCallbackId;

        private class CallbackLogEntry
        {
            public string CallbackId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string Data { get; set; } = string.Empty;
        }

        private List<CallbackLogEntry> _callbackLog = new();
        private readonly object _callbackLock = new();

        public TelegramBotService()
        {
            try
            {
                Logger.LogStartup();
                var config = LoadOrCreateConfig();
                if (string.IsNullOrEmpty(config.Token))
                {
                    var error = "Токен не может быть пустым. Пожалуйста, настройте файл конфигурации bot_config.json и добавьте токен бота.";
                    Logger.LogError(error);
                    throw new ArgumentException(error);
                }

                if (config.AdminId == 0)
                {
                    var warning = "ВНИМАНИЕ: ID администратора не настроен (AdminId = 0). Некоторые функции бота будут недоступны. Добавьте ваш Telegram ID в файл конфигурации.";
                    Logger.LogWarning(warning);
                    Console.WriteLine(warning);
                }

                _botClient = new TelegramBotClient(config.Token);
                _adminId = config.AdminId;
                _cartonAmount = config.CartonAmount;

                // Загружаем лог callback-запросов при старте
                LoadCallbackLog();

                Logger.LogInfo($"Конфигурация бота успешно загружена\nКоличество литров в покупке: {_cartonAmount}\nAdminId: {_adminId}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при инициализации бота", ex);
                throw;
            }
        }

        private BotConfig LoadOrCreateConfig()
        {
            try
            {
                if (File.Exists(CONFIG_FILE))
                {
                    string json = File.ReadAllText(CONFIG_FILE);
                    var config = JsonSerializer.Deserialize<BotConfig>(json);
                    if (config != null)
                    {
                        if (string.IsNullOrEmpty(config.Token))
                        {
                            Console.WriteLine("В конфигурации отсутствует токен бота. Пожалуйста, добавьте токен в файл bot_config.json");
                            throw new InvalidOperationException("Токен бота не настроен. Добавьте токен в файл bot_config.json");
                        }
                        return config;
                    }
                }

                // Создаем новый конфиг с пояснениями
                var defaultConfig = new BotConfig
                {
                    Token = "YOUR_BOT_TOKEN_HERE", // Замените на реальный токен
                    AdminId = 0, // ID администратора в Telegram
                    CartonAmount = 1.0m, // Количество литров в одной покупке
                    AutoStart = false // Автозапуск при старте Windows
                };

                string defaultJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                // Добавляем комментарии в начало файла
                string configWithComments = @"// Конфигурация Telegram бота
                    // Token - токен вашего бота, полученный от @BotFather
                    // AdminId - ваш Telegram ID (можно узнать у @userinfobot)
                    // CartonAmount - количество литров в одной покупке
                    // AutoStart - автозапуск при старте Windows (true/false)

                    " + defaultJson;

                File.WriteAllText(CONFIG_FILE, configWithComments);
                Console.WriteLine($"Создан новый файл конфигурации: {CONFIG_FILE}");
                Console.WriteLine("Пожалуйста, отредактируйте файл bot_config.json и добавьте токен бота и ID администратора");

                throw new InvalidOperationException(
                    "Создан новый файл конфигурации. " +
                    "Пожалуйста, отредактируйте bot_config.json и добавьте токен бота и ID администратора.");
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException)
                    throw;
                
                Console.WriteLine($"Ошибка при загрузке конфигурации: {ex.Message}");
                throw new InvalidOperationException(
                    "Не удалось загрузить или создать конфигурацию. " +
                    "Проверьте права доступа к файлу bot_config.json", ex);
            }
        }

        public async Task SaveConfig(BotConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(CONFIG_FILE, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при сохранении конфигурации");
                throw;
            }
        }

        public async Task StartAsync()
        {
            try
            {
                Logger.LogServiceEvent("Starting", "Attempting to start the bot service");
                var me = await _botClient.GetMe();
                Logger.LogInfo($"Бот {me.Username} запущен");

                _cts = new CancellationTokenSource();
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                };

                _botClient.StartReceiving(
                    updateHandler: UpdateHandler,
                    errorHandler: ErrorHandler,
                    receiverOptions: receiverOptions,
                    cancellationToken: _cts.Token
                );
                Logger.LogServiceEvent("Started", "Bot service successfully started");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при запуске бота", ex);
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                Logger.LogServiceEvent("Stopping", "Attempting to stop the bot service");
                _cts.Cancel();
                Logger.LogServiceEvent("Stopped", "Bot service successfully stopped");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при остановке бота", ex);
            }
            await Task.CompletedTask;
        }

        private async Task UpdateHandler(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Text != null)
            {
                string text = update.Message.Text.Trim();
                long chatId = update.Message.Chat.Id;
                long userId = update.Message.From.Id;
                string userName = update.Message.From.FirstName;
                if (!string.IsNullOrEmpty(update.Message.From.LastName))
                    userName += " " + update.Message.From.LastName;

                // Создаем основную клавиатуру
                var mainKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { 
                        new KeyboardButton("🛒 Купил 1 л молока"),
                        new KeyboardButton("📊 Статистика")
                    }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = false
                };

                // Обработка команды покупки в любом контексте
                if (text.Contains("купил", StringComparison.OrdinalIgnoreCase) && 
                    text.Contains("молок", StringComparison.OrdinalIgnoreCase))
                {
                    if (_lastBuyTime.TryGetValue(userId.ToString(), out var lastTime))
                    {
                        if (DateTime.Now - lastTime < _cooldown)
                        {
                            await _botClient.SendMessage(
                                chatId,
                                "⏳ Подождите немного перед следующей покупкой.",
                                replyMarkup: MarkupAdapter.ToTelegramReply(mainKeyboard)
                            );
                            return;
                        }
                    }
                    _lastBuyTime[userId.ToString()] = DateTime.Now;

                    DataAccess.AddTransaction(userId.ToString(), userName, "BUY", _cartonAmount, DateTime.Now);

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { 
                            InlineKeyboardButton.WithCallbackData("❌ Отменить покупку", $"CANCEL_BUY:{userId}")
                        }
                    });

                    await _botClient.SendMessage(
                        chatId,
                        $"✅ *Покупка зафиксирована*\n\n" +
                        $"• Количество: {_cartonAmount} л\n" +
                        $"• Пользователь: {userName}\n" +
                        $"• Время: {DateTime.Now:HH:mm:ss}\n\n" +
                        $"Для отмены нажмите кнопку ниже:",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: MarkupAdapter.ToTelegramInline(keyboard)
                    );
                    return;
                }

                if (text == "/start" || text == "Вернуться на главную")
                {
                    await _botClient.SendMessage(
                        chatId,
                        $"👋 *Привет, {userName}!*\n\n" +
                        $"Я бот для учета покупок молока.\n\n" +
                        $"*Доступные действия:*\n" +
                        $"• 🛒 Купить молоко\n" +
                        $"• 📊 Посмотреть статистику\n\n" +
                        $"Выберите действие или напишите сообщение:",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: MarkupAdapter.ToTelegramReply(mainKeyboard)
                    );
                }
                else if (text == "🛒 Купил 1 л молока" || text == "Купил 1 л молока")
                {
                    if (_lastBuyTime.TryGetValue(userId.ToString(), out var lastTime))
                    {
                        if (DateTime.Now - lastTime < _cooldown)
                        {
                            await _botClient.SendMessage(
                                chatId,
                                "⏳ Подождите немного перед следующей покупкой.",
                                replyMarkup: MarkupAdapter.ToTelegramReply(mainKeyboard)
                            );
                            return;
                        }
                    }
                    _lastBuyTime[userId.ToString()] = DateTime.Now;

                    DataAccess.AddTransaction(userId.ToString(), userName, "BUY", _cartonAmount, DateTime.Now);

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { 
                            InlineKeyboardButton.WithCallbackData("❌ Отменить покупку", $"CANCEL_BUY:{userId}")
                        }
                    });

                    await _botClient.SendMessage(
                        chatId,
                        $"✅ *Покупка зафиксирована*\n\n" +
                        $"• Количество: {_cartonAmount} л\n" +
                        $"• Пользователь: {userName}\n" +
                        $"• Время: {DateTime.Now:HH:mm:ss}\n\n" +
                        $"Для отмены нажмите кнопку ниже:",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: MarkupAdapter.ToTelegramInline(keyboard)
                    );
                }
                else if (text == "📊 Статистика" || text == "Статистика")
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] {
                            InlineKeyboardButton.WithCallbackData("За день", "DAY"),
                            InlineKeyboardButton.WithCallbackData("📆 За неделю", "WEEK")
                        },
                        new[] {
                            InlineKeyboardButton.WithCallbackData("📊 За месяц", "MONTH"),
                            InlineKeyboardButton.WithCallbackData("📈 За год", "YEAR")
                        }
                    });

                    await _botClient.SendMessage(
                        chatId,
                        "📊 *Выберите период статистики:*",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: MarkupAdapter.ToTelegramInline(keyboard)
                    );
                }
                else if ( ((text.Contains("/admin", StringComparison.OrdinalIgnoreCase)) || text == "//admin") && userId == _adminId)
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] {
                            InlineKeyboardButton.WithCallbackData("🔄 Перезапустить бота", "RESTART_BOT"),
                            InlineKeyboardButton.WithCallbackData("♻ Перезапустить приложение", "RESTART_APP")
                        },
                        new[] {
                            InlineKeyboardButton.WithCallbackData("⚙️ Управление автозагрузкой", "TOGGLE_AUTOSTART"),
                            InlineKeyboardButton.WithCallbackData("❌ Удалить бота", "CONFIRM_REMOVE")
                        }
                    });

                    await _botClient.SendMessage(
                        chatId,
                        "🛠 *Панель администратора*\n\n" +
                        "Выберите действие:",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: MarkupAdapter.ToTelegramInline(keyboard)
                    );
                }
                else
                {
                    // Если сообщение не распознано, показываем подсказку
                    await _botClient.SendMessage(
                        chatId,
                        "ℹ️ *Подсказка*\n\n" +
                        "Вы можете:\n" +
                        "• Написать 'купил молока' для фиксации покупки\n" +
                        "• Использовать кнопки меню\n" +
                        "• Посмотреть статистику\n\n" +
                        "Выберите действие:",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: MarkupAdapter.ToTelegramReply(mainKeyboard)
                    );
                }
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                var callback = update.CallbackQuery;
                var data = callback.Data;

                if (data == "RESTART_BOT")
                {
                    if (_adminId > 0 && callback.From.Id == _adminId)
                    {
                        if (_isManualRestarting)
                        {
                            await _botClient.AnswerCallbackQuery(callback.Id, "⏳ Уже выполняется перезапуск.");
                            return;
                        }
                        else if (IsDuplicateCallback(callback.Id))
                        {
                            Logger.LogInfo("Callback уже обработан ранее — RESTART_BOT");
                            return;
                        }

                        try
                        {
                            _isManualRestarting = true;
                            await _botClient.AnswerCallbackQuery(callback.Id);
                            await SendToAdminIfChanged("🔁 Перезапускаю бота...");
                            await RestartBot();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Ошибка при перезапуске бота", ex);
                            await SendToAdminIfChanged($"❌ Ошибка при перезапуске бота: {ex.Message}");
                        }
                        finally
                        {
                            _isManualRestarting = false;
                        }
                    }
                    return;
                }
                else if (data == "RESTART_APP")
                {
                    if (_adminId > 0 && callback.From.Id == _adminId)
                    {
                        if (IsDuplicateCallback(callback.Id))
                        {
                            Logger.LogInfo("Callback уже обработан ранее — RESTART_APP");
                            return;
                        }

                        try
                        {
                            await _botClient.EditMessageText(
                                callback.Message.Chat.Id,
                                callback.Message.MessageId,
                                "🔄 *Перезапуск службы*\n\n" +
                                "Начинаю процесс перезапуска...\n" +
                                "Это может занять несколько секунд.",
                                parseMode: ParseMode.Markdown
                            );

                            await _botClient.AnswerCallbackQuery(callback.Id);
                            await RestartApplication();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Ошибка при перезапуске службы", ex);
                            await _botClient.AnswerCallbackQuery(
                                callback.Id,
                                $"❌ Ошибка при перезапуске: {ex.Message}",
                                showAlert: true
                            );
                        }
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQuery(
                            callback.Id,
                            "❌ У вас нет прав для выполнения этой команды",
                            showAlert: true
                        );
                    }
                }
                else if (data == "TOGGLE_AUTOSTART")
                {
                    if (_adminId > 0 && callback.From.Id == _adminId)
                    {
                        try
                        {
                            bool currentState = IsServiceAutoStart();
                            bool success = await SetAutoStart(!currentState);

                            if (success)
                            {
                                await _botClient.EditMessageText(
                                    callback.Message.Chat.Id,
                                    callback.Message.MessageId,
                                    "🔄 *Управление автозагрузкой службы*\n\n" +
                                    $"Статус изменен: {(!currentState ? "✅ Автозапуск включен" : "❌ Автозапуск отключен")}",
                                    parseMode: ParseMode.Markdown
                                );
                            }
                            else
                            {
                                await _botClient.AnswerCallbackQuery(
                                    callback.Id,
                                    "❌ Не удалось изменить статус автозагрузки службы",
                                    showAlert: true
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Ошибка при изменении автозапуска службы", ex);
                            await _botClient.AnswerCallbackQuery(
                                callback.Id,
                                $"❌ Ошибка: {ex.Message}",
                                showAlert: true
                            );
                        }
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQuery(
                            callback.Id,
                            "❌ У вас нет прав для выполнения этой команды",
                            showAlert: true
                        );
                    }
                }
                else if (data == "CONFIRM_REMOVE")
                {
                    if (_adminId > 0 && callback.From.Id == _adminId)
                    {
                        try
                        {
                            await _botClient.EditMessageText(
                                callback.Message.Chat.Id,
                                callback.Message.MessageId,
                                "🔄 *Удаление службы*\n\n" +
                                "Начинаю процесс удаления...\n" +
                                "Это может занять несколько секунд.",
                                parseMode: ParseMode.Markdown
                            );

                            await RemoveService();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Ошибка при удалении службы", ex);
                            await _botClient.AnswerCallbackQuery(
                                callback.Id,
                                $"❌ Ошибка при удалении службы: {ex.Message}",
                                showAlert: true
                            );
                        }
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQuery(
                            callback.Id,
                            "❌ У вас нет прав для выполнения этой команды",
                            showAlert: true
                        );
                    }
                }
                else if (data == "CANCEL_REMOVE")
                {
                    await _botClient.EditMessageText(
                        callback.Message.Chat.Id,
                        callback.Message.MessageId,
                        "❌ Удаление бота отменено"
                    );
                }
                else if (data.StartsWith("CANCEL_BUY:"))
                {
                    string userId = data.Split(':')[1];
                    if (callback.From.Id.ToString() != userId)
                    {
                        await _botClient.AnswerCallbackQuery(callback.Id, "Эта кнопка не для вас.", showAlert: true);
                        return;
                    }

                    bool success = DataAccess.CancelLastBuy(userId);
                    if (success)
                    {
                        await _botClient.EditMessageText(callback.Message.Chat.Id, callback.Message.MessageId, "✅ Последняя покупка отменена.");
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQuery(callback.Id, "❌ Отмена недоступна. Возможно, прошло больше часа.", showAlert: true);
                    }
                }
                else if (data.StartsWith("EDIT:"))
                {
                    _editTargetUserId = data.Substring("EDIT:".Length);
                    _editAdminUserId = callback.From.Id;

                    await _botClient.SendMessage(callback.Message.Chat.Id,
                        $"Введите новое количество литров для пользователя {_editTargetUserId} (например: `2.5`)",
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    await HandleStatistics(data, callback);
                }

                await _botClient.AnswerCallbackQuery(callback.Id);
            }
        }

        private void LoadCallbackLog()
        {
            try
            {
                if (File.Exists(CALLBACK_LOG_FILE))
                {
                    string json = File.ReadAllText(CALLBACK_LOG_FILE);
                    var log = JsonSerializer.Deserialize<List<CallbackLogEntry>>(json);
                    if (log != null)
                    {
                        _callbackLog = log;
                        CleanupOldCallbacks(); // Очищаем старые записи при загрузке
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при загрузке лога callback-запросов", ex);
            }
        }

        private void SaveCallbackLog()
        {
            try
            {
                lock (_callbackLock)
                {
                    string json = JsonSerializer.Serialize(_callbackLog, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(CALLBACK_LOG_FILE, json);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при сохранении лога callback-запросов", ex);
            }
        }

        private void CleanupOldCallbacks()
        {
            try
            {
                lock (_callbackLock)
                {
                    var cutoffTime = DateTime.Now.AddHours(-CALLBACK_EXPIRY_HOURS);
                    _callbackLog.RemoveAll(entry => entry.Timestamp < cutoffTime);
                    SaveCallbackLog();
                    Logger.LogInfo($"Очищено {_callbackLog.Count} старых callback-запросов");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при очистке старых callback-запросов", ex);
            }
        }

        private bool IsDuplicateCallback(string callbackId)
        {
            if (string.IsNullOrEmpty(callbackId))
                return false;

            try
            {
                lock (_callbackLock)
                {
                    // Проверяем в памяти
                    if (_callbackLog.Any(entry => entry.CallbackId == callbackId))
                    {
                        Logger.LogInfo($"Callback уже обработан ранее: {callbackId}");
                        return true;
                    }

                    // Добавляем новую запись
                    _callbackLog.Add(new CallbackLogEntry
                    {
                        CallbackId = callbackId,
                        Timestamp = DateTime.Now
                    });

                    // Если лог стал слишком большим, очищаем старые записи
                    if (_callbackLog.Count > 1000)
                    {
                        CleanupOldCallbacks();
                    }
                    else
                    {
                        SaveCallbackLog();
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Не удалось проверить или сохранить callback ID", ex);
                return false;
            }
        }

        public async Task SendMessageToAdmin(string text)
        {
            if (_adminId > 0)
            {
                try
                {
                    await _botClient.SendMessage(
                        chatId: _adminId,
                        text: text,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Ошибка отправки админу]: " + ex.Message);
                }
            }
        }

        private bool IsServiceRunning()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = SC_EXE,
                    Arguments = $"query {SERVICE_NAME}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return false;
                
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                Logger.LogInfo($"Статус службы: {output}");
                return output.Contains("RUNNING");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при проверке статуса службы", ex);
                return false;
            }
        }

        private bool IsServiceAutoStart()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = SC_EXE,
                    Arguments = $"qc {SERVICE_NAME}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return false;
                
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                Logger.LogInfo($"Настройки автозапуска службы: {output}");
                return output.Contains("AUTO_START");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при проверке автозапуска службы", ex);
                return false;
            }
        }

        private async Task<bool> ExecuteServiceCommand(string command, string description)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = SC_EXE,
                    Arguments = command,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new Exception($"Не удалось запустить процесс для команды: {command}");
                }

                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Команда завершилась с кодом: {process.ExitCode}");
                }

                Logger.LogInfo($"Успешно выполнена команда: {description}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при выполнении команды {description}", ex);
                await SendToAdminIfChanged($"❌ Ошибка при {description.ToLower()}: {ex.Message}");
                return false;
            }
        }

        private async Task RestartBot()
        {
            try
            {
                Logger.LogServiceEvent("Restarting", "Attempting to restart the bot");
                await StopAsync();
                await Task.Delay(1000); // Даем время на корректное завершение
                
                if (!IsServiceRunning())
                {
                    var error = "Служба не запущена после остановки";
                    Logger.LogError(error);
                    throw new Exception(error);
                }
                
                await StartAsync();
                _wasRestartedOnce = false;
                
                Logger.LogServiceEvent("Restarted", "Bot successfully restarted");
                await SendToAdminIfChanged("✅ Бот успешно перезапущен");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при перезапуске бота", ex);
                await SendToAdminIfChanged($"❌ Ошибка при перезапуске бота: {ex.Message}");
                throw;
            }
        }

        private async Task RestartApplication()
        {
            try
            {
                Logger.LogServiceEvent("Restarting", "Attempting to restart the application");
                // Останавливаем службу
                if (!await ExecuteServiceCommand($"stop {SERVICE_NAME}", "остановка службы"))
                {
                    var error = "Не удалось остановить службу";
                    Logger.LogError(error);
                    throw new Exception(error);
                }

                // Ждем 2 секунды
                await Task.Delay(2000);

                // Запускаем службу
                if (!await ExecuteServiceCommand($"start {SERVICE_NAME}", "запуск службы"))
                {
                    var error = "Не удалось запустить службу";
                    Logger.LogError(error);
                    throw new Exception(error);
                }

                Logger.LogServiceEvent("Restarted", "Application successfully restarted");
                await SendToAdminIfChanged("✅ Служба успешно перезапущена");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при перезапуске службы", ex);
                await SendToAdminIfChanged($"❌ Ошибка при перезапуске службы: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> SetAutoStart(bool enable)
        {
            try
            {
                string startType = enable ? "auto" : "demand";
                if (!await ExecuteServiceCommand($"config {SERVICE_NAME} start= {startType}", 
                    $"изменение автозапуска на {startType}"))
                {
                    return false;
                }

                // Проверяем, что изменения применились
                bool currentState = IsServiceAutoStart();
                if (currentState != enable)
                {
                    throw new Exception($"Не удалось подтвердить изменение автозапуска. Текущее состояние: {currentState}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при изменении автозапуска службы", ex);
                return false;
            }
        }

        private async Task RemoveService()
        {
            try
            {
                Logger.LogServiceEvent("Removing", "Attempting to remove the service");
                // Останавливаем службу
                if (!await ExecuteServiceCommand($"stop {SERVICE_NAME}", "остановка службы"))
                {
                    var error = "Не удалось остановить службу";
                    Logger.LogError(error);
                    throw new Exception(error);
                }

                // Удаляем службу
                if (!await ExecuteServiceCommand($"delete {SERVICE_NAME}", "удаление службы"))
                {
                    var error = "Не удалось удалить службу";
                    Logger.LogError(error);
                    throw new Exception(error);
                }

                Logger.LogServiceEvent("Removed", "Service successfully removed");
                await SendToAdminIfChanged("✅ Служба успешно удалена");
                
                // Даем время на отправку сообщения
                await Task.Delay(1000);
                
                Logger.LogShutdown("Service removed by admin request");
                // Завершаем процесс
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при удалении службы", ex);
                await SendToAdminIfChanged($"❌ Ошибка при удалении службы: {ex.Message}");
                throw;
            }
        }

        private async Task HandleStatistics(string data, CallbackQuery callback)
        {
            DateTime today = DateTime.Today;
            DataTable dt = null;
            string header = "";

            if (data == "DAY")
            {
                dt = DataAccess.GetDailySummary(today);
                header = "Дневная статистика";
            }
            else if (data == "WEEK")
            {
                dt = DataAccess.GetPeriodSummary(today.AddDays(-6), today);
                header = "Статистика за неделю";
            }
            else if (data == "MONTH")
            {
                dt = DataAccess.GetPeriodSummary(today.AddDays(-29), today);
                header = "Статистика за месяц";
            }
            else if (data == "YEAR")
            {
                dt = DataAccess.GetPeriodSummary(today.AddDays(-364), today);
                header = "Статистика за год";
            }

            if (dt != null)
            {
                string summary = FormatSummary(dt, header);
                await _botClient.SendMessage(callback.Message.Chat.Id, summary);
            }
        }

        private async Task SendToAdminIfChanged(string text)
        {
            if (_adminId == 0) return;

            if (_lastAdminMessageText == text)
                return;

            var msg = await _botClient.SendMessage(_adminId, text);
            _lastAdminMessageId = msg.MessageId;
            _lastAdminMessageText = text;
        }

        private async Task ErrorHandler(ITelegramBotClient _, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.Now;
            var errorType = exception.GetType().Name;
            var fullMessage = exception.ToString();

            // Логируем ошибку
            Console.WriteLine($"PollingError: {errorType}: {exception.Message}");
            Logger.LogError($"PollingError: {errorType}: {exception.Message}");

            // Проверяем, является ли ошибка связанной с устаревшим callback
            if (exception.Message.Contains("query is too old") || 
                exception.Message.Contains("query ID is invalid"))
            {
                // Это нормальная ситуация, просто логируем
                Logger.LogInfo($"Игнорируем устаревший callback: {exception.Message}");
                return;
            }

            // Для других ошибок пытаемся уведомить админа
            if (_adminId > 0)
            {
                try
                {
                    // Проверяем, что бот все еще работает
                    var me = await _botClient.GetMe(cancellationToken);
                    if (me != null)
                    {
                        var keyboard = MarkupAdapter.ToTelegramInline(new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🔁 Перезапустить бота", "RESTART_BOT"),
                                InlineKeyboardButton.WithCallbackData("♻ Перезапустить приложение", "RESTART_APP")
                            }
                        }));

                        var userMessage = $"❌ *Ошибка при работе с Telegram API*\n" +
                                        $"*Тип:* `{errorType}`\n" +
                                        $"*Время:* `{timestamp:yyyy-MM-dd HH:mm:ss}`\n" +
                                        $"```\n{exception.Message}```";

                        await _botClient.SendMessage(
                            chatId: _adminId,
                            text: userMessage,
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    // Если не удалось отправить сообщение админу, логируем это
                    Logger.LogError("Не удалось уведомить админа об ошибке", ex);
                    Console.WriteLine($"Не удалось уведомить админа об ошибке: {ex.Message}");
                }
            }
        }

        private string FormatSummary(DataTable dt, string header)
        {
            if (dt.Rows.Count == 0)
                return $"{header}:\nНет данных.";

            string result = $"📊 *{header}*\n\n";
            decimal total = 0;

            foreach (DataRow row in dt.Rows)
            {
                string userName = row["UserName"].ToString();
                decimal amount = Convert.ToDecimal(row["TotalBought"]);
                total += amount;
                result += $"• {userName}: {amount} л\n";
            }

            result += $"\n *Итого:* {total} л";
            return result;
        }
    }
}
