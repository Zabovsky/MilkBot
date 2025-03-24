using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MilkBot
{
    public partial class MainForm : Form
    {
        private TelegramBotService _botService;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // ��������� ���������� �����, ���� �� ����
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.Token))
            {
                textBoxToken.Text = Properties.Settings.Default.Token;
            }
            labelStatus.Text = "��� �� �������";
            labelStatus.ForeColor = Color.Red;

            // ��������� ����� ������ ������������
            buttonAutoStart.Text = IsAutoStartEnabled() ? "������� �� ������������" : "�������� � ������������";

            // ���� ���������� �������� � ���������� /minimized, �������� �����
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1].Equals("/minimized", StringComparison.OrdinalIgnoreCase))
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }
        }

        // ��������� ��������� ������� �����: ��� ������������ �������� ����
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
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("������� ���������� ����� ��� ������� ����.");
                return;
            }

            try
            {
                _botService = new TelegramBotService(token, this);
                await _botService.StartAsync();

                // ��������� �����
                Properties.Settings.Default.Token = token;
                Properties.Settings.Default.Save();

                labelStatus.Text = "��� �������";
                labelStatus.ForeColor = Color.Green;

                buttonStart.Enabled = false;
                buttonStop.Enabled = true;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"������ ������� ����: {ex.Message}");
            }
        }

        private async void buttonStop_Click(object sender, EventArgs e)
        {
            if (_botService != null)
            {
                await _botService.StopAsync();
                _botService = null;
                labelStatus.Text = "��� ����������";
                labelStatus.ForeColor = Color.Red;
                buttonStart.Enabled = true;
                buttonStop.Enabled = false;
            }
        }

        // ������ ������������: ����������/�������� ������ � �������
        private void buttonAutoStart_Click(object sender, EventArgs e)
        {
            bool isAutoStart = IsAutoStartEnabled();
            if (isAutoStart)
            {
                SetAutoStart(false);
                MessageBox.Show("���������� ������� �� ������������.");
                buttonAutoStart.Text = "�������� � ������������";
            }
            else
            {
                SetAutoStart(true);
                MessageBox.Show("���������� ��������� � ������������.");
                buttonAutoStart.Text = "������� �� ������������";
            }
        }

        // �������� ������������ ����� ������
        private bool IsAutoStartEnabled()
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, false))
            {
                return key.GetValue(Application.ProductName) != null;
            }
        }

        // ���������/�������� ������������
        private void SetAutoStart(bool enable)
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true))
            {
                if (enable)
                {
                    // ��������� �������� /minimized, ����� ���������� ����������� ���������
                    key.SetValue(Application.ProductName, "\"" + Application.ExecutablePath + "\" /minimized");
                }
                else
                {
                    key.DeleteValue(Application.ProductName, false);
                }
            }
        }

        // ��������� �������� ����� �� NotifyIcon � ��������������� ����
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

        // ����� ��� �������������� �����
        private void ShowMainForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        // ��������� ������ ��� ������ ����, ���������� � �.�.

        public decimal GetCartonAmount()
        {
            if (decimal.TryParse(textBoxCarton.Text, out decimal value))
                return value;
            return 1m;
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
    }
}
