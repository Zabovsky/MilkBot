namespace MilkBot
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelToken;
        private System.Windows.Forms.TextBox textBoxToken;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.TextBox textBoxCarton;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
        private System.Windows.Forms.DataGridView dataGridViewSummary;
        private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.Button buttonWeek;
        private System.Windows.Forms.Button buttonMonth;
        private System.Windows.Forms.Button buttonYear;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        private void InitializeComponent()
        {
            labelToken = new Label();
            textBoxToken = new TextBox();
            buttonStart = new Button();
            textBoxCarton = new TextBox();
            dateTimePicker = new DateTimePicker();
            dataGridViewSummary = new DataGridView();
            buttonRefresh = new Button();
            buttonWeek = new Button();
            buttonMonth = new Button();
            buttonYear = new Button();
            label2 = new Label();
            labelStatus = new Label();
            buttonStop = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridViewSummary).BeginInit();
            SuspendLayout();
            // 
            // labelToken
            // 
            labelToken.AutoSize = true;
            labelToken.Location = new Point(12, 15);
            labelToken.Name = "labelToken";
            labelToken.Size = new Size(41, 15);
            labelToken.TabIndex = 0;
            labelToken.Text = "Token:";
            // 
            // textBoxToken
            // 
            textBoxToken.Location = new Point(70, 12);
            textBoxToken.Name = "textBoxToken";
            textBoxToken.Size = new Size(219, 23);
            textBoxToken.TabIndex = 1;
            // 
            // buttonStart
            // 
            buttonStart.Location = new Point(297, 12);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(75, 23);
            buttonStart.TabIndex = 2;
            buttonStart.Text = "Запуск";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // textBoxCarton
            // 
            textBoxCarton.Location = new Point(110, 51);
            textBoxCarton.Name = "textBoxCarton";
            textBoxCarton.Size = new Size(100, 23);
            textBoxCarton.TabIndex = 3;
            textBoxCarton.Text = "1.0";
            // 
            // dateTimePicker
            // 
            dateTimePicker.Location = new Point(12, 80);
            dateTimePicker.Name = "dateTimePicker";
            dateTimePicker.Size = new Size(200, 23);
            dateTimePicker.TabIndex = 5;
            // 
            // dataGridViewSummary
            // 
            dataGridViewSummary.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewSummary.Location = new Point(12, 109);
            dataGridViewSummary.Name = "dataGridViewSummary";
            dataGridViewSummary.Size = new Size(360, 200);
            dataGridViewSummary.TabIndex = 6;
            // 
            // buttonRefresh
            // 
            buttonRefresh.Location = new Point(28, 315);
            buttonRefresh.Name = "buttonRefresh";
            buttonRefresh.Size = new Size(114, 30);
            buttonRefresh.TabIndex = 7;
            buttonRefresh.Text = "Обновить (день)";
            buttonRefresh.UseVisualStyleBackColor = true;
            buttonRefresh.Click += buttonRefresh_Click;
            // 
            // buttonWeek
            // 
            buttonWeek.Location = new Point(148, 315);
            buttonWeek.Name = "buttonWeek";
            buttonWeek.Size = new Size(100, 30);
            buttonWeek.TabIndex = 8;
            buttonWeek.Text = "Неделя";
            buttonWeek.UseVisualStyleBackColor = true;
            buttonWeek.Click += buttonWeek_Click;
            // 
            // buttonMonth
            // 
            buttonMonth.Location = new Point(254, 315);
            buttonMonth.Name = "buttonMonth";
            buttonMonth.Size = new Size(100, 30);
            buttonMonth.TabIndex = 9;
            buttonMonth.Text = "Месяц";
            buttonMonth.UseVisualStyleBackColor = true;
            buttonMonth.Click += buttonMonth_Click;
            // 
            // buttonYear
            // 
            buttonYear.Location = new Point(28, 355);
            buttonYear.Name = "buttonYear";
            buttonYear.Size = new Size(100, 30);
            buttonYear.TabIndex = 10;
            buttonYear.Text = "Год";
            buttonYear.UseVisualStyleBackColor = true;
            buttonYear.Click += buttonYear_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(7, 54);
            label2.Name = "label2";
            label2.Size = new Size(97, 15);
            label2.TabIndex = 12;
            label2.Text = "В пачке молока:";
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Location = new Point(275, 54);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(90, 15);
            labelStatus.TabIndex = 13;
            labelStatus.Text = "Бот выключен!";
            // 
            // buttonStop
            // 
            buttonStop.Enabled = false;
            buttonStop.Location = new Point(291, 80);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(81, 23);
            buttonStop.TabIndex = 14;
            buttonStop.Text = "Остановить";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // MainForm
            // 
            ClientSize = new Size(384, 391);
            Controls.Add(buttonStop);
            Controls.Add(labelStatus);
            Controls.Add(label2);
            Controls.Add(buttonYear);
            Controls.Add(buttonMonth);
            Controls.Add(buttonWeek);
            Controls.Add(buttonRefresh);
            Controls.Add(dataGridViewSummary);
            Controls.Add(dateTimePicker);
            Controls.Add(textBoxCarton);
            Controls.Add(buttonStart);
            Controls.Add(textBoxToken);
            Controls.Add(labelToken);
            Name = "MainForm";
            Text = "Milk Bot";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridViewSummary).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label2;
        private Label labelStatus;
        private Button buttonStop;
    }
}
