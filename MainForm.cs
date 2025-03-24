using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MilkBot
{
    public partial class MainForm : Form
    {
        private TelegramBotService _botService;

        public MainForm()
        {
            InitializeComponent();
        }

        // При загрузке формы загружаем сохранённый токен (если он был сохранён ранее)
        // Загрузка токена при старте формы:
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.Token))
            {
                textBoxToken.Text = Properties.Settings.Default.Token;
            }
        }

        // Сохранение токена после запуска бота:
        private async void buttonStart_Click(object sender, EventArgs e)
        {
            string token = textBoxToken.Text.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("Введите корректный токен для запуска бота.");
                return;
            }

            try
            {
                _botService = new TelegramBotService(token, this);
                await _botService.StartAsync();

                Properties.Settings.Default.Token = token;
                Properties.Settings.Default.Save();

                labelStatus.Text = "Бот запущен";
                labelStatus.ForeColor = System.Drawing.Color.Green;
                buttonStop.Enabled = true;
                buttonStart.Enabled = false;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Ошибка запуска бота: {ex.Message}");
                labelStatus.Text = "Ошибка запуска бота";
                labelStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        // Метод для получения настроенного объёма пачки (литры)
        public decimal GetCartonAmount()
        {
            if (decimal.TryParse(textBoxCarton.Text, out decimal value))
                return value;
            return 1m;
        }


        // Обновление статистики за выбранный день с разбивкой по пользователям и итоговой суммой
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            DateTime selectedDate = dateTimePicker.Value.Date;
            DataTable summary = DataAccess.GetDailySummary(selectedDate);
            dataGridViewSummary.DataSource = summary;
        }

        // Сводка за последнюю неделю
        private void buttonWeek_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-6);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        // Сводка за последний месяц (30 дней)
        private void buttonMonth_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-29);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        // Сводка за последний год (365 дней)
        private void buttonYear_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-364);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        // Остановка работы бота при закрытии формы
        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_botService != null)
                await _botService.StopAsync();
        }

        private async void buttonStop_Click(object sender, EventArgs e)
        {
            if (_botService != null)
            {
                await _botService.StopAsync();
                _botService = null;
                labelStatus.Text = "Бот остановлен";
                labelStatus.ForeColor = System.Drawing.Color.Red;
                buttonStop.Enabled = false;
                buttonStart.Enabled = true;
            }
            else
            {
                MessageBox.Show("Бот уже остановлен или не запущен.");
            }
        }
    }
}