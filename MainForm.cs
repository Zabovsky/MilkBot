using Microsoft.Win32;
using System.Data;

namespace MilkBot
{
    public partial class MainFormNew : Form
    {
        private TelegramBotService _botService;

        public MainFormNew()
        {
            InitializeComponent();
        }
        private ConnectionMonitor _connectionMonitor;

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Загружаем сохранённые настройки
            string token = Properties.Settings.Default.Token?.Trim();
            string adminIdText = Properties.Settings.Default.AdminId?.Trim();

            if (!string.IsNullOrWhiteSpace(token))
                textBoxToken.Text = token;

            if (!string.IsNullOrWhiteSpace(adminIdText))
                textBoxIdAdmin.Text = adminIdText;

            labelStatus.Text = "Бот не запущен";
            labelStatus.ForeColor = Color.Red;

            // Обновляем надпись на кнопке автозапуска
            buttonAutoStart.Text = IsAutoStartEnabled()
                ? "Удалить из автозагрузки"
                : "Добавить в автозагрузку";

            // Всегда пытаемся запустить бота, если токен указан
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    StartBot(token, adminIdText);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при автоматическом запуске бота:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Проверка аргументов — скрытие в трей
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1].Equals("/minimized", StringComparison.OrdinalIgnoreCase))
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }
        }


        // Обработка изменения размера формы: при сворачивании скрываем окно
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon.Visible = true;
            }
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            string token = textBoxToken.Text.Trim();
            string adminIdText = textBoxIdAdmin.Text.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("Введите корректный токен для запуска бота.");
                return;
            }
            if (!long.TryParse(adminIdText, out long adminId))
            {
                MessageBox.Show("Введите корректный Telegram ID администратора.");
                return;
            }
            try
            {
                StartBot(token, adminIdText);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Ошибка запуска бота: {ex.Message}");
            }
        }
        public async void StartBot(string token, string adminIdText)
        {
            try
            {


                // Сохраняем настройки
                Properties.Settings.Default.Token = token;
                Properties.Settings.Default.AdminId = adminIdText;
                Properties.Settings.Default.Save();

                // запускаем бота
                _botService = new TelegramBotService(token, this, Convert.ToInt64(adminIdText));
                await _botService.StartAsync();


                // Запуск мониторинга соединения
                _connectionMonitor = new ConnectionMonitor(_botService);
                _connectionMonitor.Start();

                labelStatus.Text = "Бот запущен";
                labelStatus.ForeColor = Color.Green;

                buttonStart.Enabled = false;
                buttonStop.Enabled = true;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Ошибка запуска бота: {ex.Message}");
            }
        }
        private async void buttonStop_Click(object sender, EventArgs e)
        {
            if (_botService != null)
            {
                await _botService.StopAsync();
                _botService = null;
                labelStatus.Text = "Бот остановлен";
                labelStatus.ForeColor = Color.Red;
                buttonStart.Enabled = true;
                buttonStop.Enabled = false;
            }
        }

        // Кнопка автозагрузки: добавление/удаление записи в реестре
        private void buttonAutoStart_Click(object sender, EventArgs e)
        {
            bool isAutoStart = IsAutoStartEnabled();
            if (isAutoStart)
            {
                SetAutoStart(false);
                MessageBox.Show("Приложение удалено из автозагрузки.");
                buttonAutoStart.Text = "Добавить в автозагрузку";
            }
            else
            {
                SetAutoStart(true);
                MessageBox.Show("Приложение добавлено в автозагрузку.");
                buttonAutoStart.Text = "Удалить из автозагрузки";
            }
        }

        // Проверка автозагрузки через реестр
        private bool IsAutoStartEnabled()
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, false))
            {
                return key.GetValue(Application.ProductName) != null;
            }
        }

        // Установка/удаление автозагрузки
        private void SetAutoStart(bool enable)
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true))
            {
                if (enable)
                {
                    // Добавляем параметр /minimized, чтобы приложение запускалось свернутым
                    key.SetValue(Application.ProductName, "\"" + Application.ExecutablePath + "\" /minimized");
                }
                else
                {
                    key.DeleteValue(Application.ProductName, false);
                }
            }
        }

        // Обработка двойного клика по NotifyIcon — восстанавливаем окно
        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainForm();
        }

        private void toolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            ShowMainForm();
        }

        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Метод для восстановления формы
        private void ShowMainForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        // Остальные методы для работы бота, статистики и т.д.

        public decimal GetCartonAmount()
        {
            if (decimal.TryParse(textBoxCarton.Text, out decimal value))
                return value;
            return 1m;
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_botService != null)
                await _botService.StopAsync();

            _connectionMonitor?.Stop(); // безопасно остановим монитор
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            DateTime selectedDate = dateTimePicker.Value.Date;
            DataTable summary = DataAccess.GetDailySummary(selectedDate);
            dataGridViewSummary.DataSource = summary;
        }

        private void buttonWeek_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-6);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        private void buttonMonth_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-29);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        private void buttonYear_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-364);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_botService != null)
                await _botService.StopAsync();
        }

        private void buttonUser_Click(object sender, EventArgs e)
        {
            var users = DataAccess.GetAllUsers();
            dataGridViewSummary.DataSource = users;
        }
    }
}
