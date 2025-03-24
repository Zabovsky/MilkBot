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

        // ��� �������� ����� ��������� ���������� ����� (���� �� ��� ������� �����)
        // �������� ������ ��� ������ �����:
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.Token))
            {
                textBoxToken.Text = Properties.Settings.Default.Token;
            }
        }

        // ���������� ������ ����� ������� ����:
        private async void buttonStart_Click(object sender, EventArgs e)
        {
            string token = textBoxToken.Text.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("������� ���������� ����� ��� ������� ����.");
                return;
            }

            try
            {
                _botService = new TelegramBotService(token, this);
                await _botService.StartAsync();

                Properties.Settings.Default.Token = token;
                Properties.Settings.Default.Save();

                labelStatus.Text = "��� �������";
                labelStatus.ForeColor = System.Drawing.Color.Green;
                buttonStop.Enabled = true;
                buttonStart.Enabled = false;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"������ ������� ����: {ex.Message}");
                labelStatus.Text = "������ ������� ����";
                labelStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        // ����� ��� ��������� ������������ ������ ����� (�����)
        public decimal GetCartonAmount()
        {
            if (decimal.TryParse(textBoxCarton.Text, out decimal value))
                return value;
            return 1m;
        }


        // ���������� ���������� �� ��������� ���� � ��������� �� ������������� � �������� ������
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            DateTime selectedDate = dateTimePicker.Value.Date;
            DataTable summary = DataAccess.GetDailySummary(selectedDate);
            dataGridViewSummary.DataSource = summary;
        }

        // ������ �� ��������� ������
        private void buttonWeek_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-6);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        // ������ �� ��������� ����� (30 ����)
        private void buttonMonth_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-29);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        // ������ �� ��������� ��� (365 ����)
        private void buttonYear_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-364);
            DataTable summary = DataAccess.GetPeriodSummary(startDate, today);
            dataGridViewSummary.DataSource = summary;
        }

        // ��������� ������ ���� ��� �������� �����
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
                labelStatus.Text = "��� ����������";
                labelStatus.ForeColor = System.Drawing.Color.Red;
                buttonStop.Enabled = false;
                buttonStart.Enabled = true;
            }
            else
            {
                MessageBox.Show("��� ��� ���������� ��� �� �������.");
            }
        }
    }
}