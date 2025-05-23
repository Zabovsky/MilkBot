﻿namespace MilkBot
{
    partial class MainFormNew
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelToken;
        private System.Windows.Forms.TextBox textBoxToken;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.TextBox textBoxCarton;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
        private System.Windows.Forms.DataGridView dataGridViewSummary;
        private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.Button buttonWeek;
        private System.Windows.Forms.Button buttonMonth;
        private System.Windows.Forms.Button buttonYear;
        private System.Windows.Forms.Button buttonAutoStart;

        // Элементы для трей-иконки
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripTray;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemOpen;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemExit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFormNew));
            labelToken = new Label();
            textBoxToken = new TextBox();
            buttonStart = new Button();
            buttonStop = new Button();
            labelStatus = new Label();
            textBoxCarton = new TextBox();
            dateTimePicker = new DateTimePicker();
            dataGridViewSummary = new DataGridView();
            buttonRefresh = new Button();
            buttonWeek = new Button();
            buttonMonth = new Button();
            buttonYear = new Button();
            buttonAutoStart = new Button();
            contextMenuStripTray = new ContextMenuStrip(components);
            toolStripMenuItemOpen = new ToolStripMenuItem();
            toolStripMenuItemExit = new ToolStripMenuItem();
            notifyIcon = new NotifyIcon(components);
            labelMilk = new Label();
            textBoxIdAdmin = new TextBox();
            label1 = new Label();
            buttonUser = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridViewSummary).BeginInit();
            contextMenuStripTray.SuspendLayout();
            SuspendLayout();
            // 
            // labelToken
            // 
            resources.ApplyResources(labelToken, "labelToken");
            labelToken.Name = "labelToken";
            // 
            // textBoxToken
            // 
            resources.ApplyResources(textBoxToken, "textBoxToken");
            textBoxToken.Name = "textBoxToken";
            // 
            // buttonStart
            // 
            resources.ApplyResources(buttonStart, "buttonStart");
            buttonStart.Name = "buttonStart";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // buttonStop
            // 
            resources.ApplyResources(buttonStop, "buttonStop");
            buttonStop.Name = "buttonStop";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // labelStatus
            // 
            resources.ApplyResources(labelStatus, "labelStatus");
            labelStatus.ForeColor = Color.Red;
            labelStatus.Name = "labelStatus";
            // 
            // textBoxCarton
            // 
            resources.ApplyResources(textBoxCarton, "textBoxCarton");
            textBoxCarton.Name = "textBoxCarton";
            // 
            // dateTimePicker
            // 
            resources.ApplyResources(dateTimePicker, "dateTimePicker");
            dateTimePicker.Name = "dateTimePicker";
            // 
            // dataGridViewSummary
            // 
            resources.ApplyResources(dataGridViewSummary, "dataGridViewSummary");
            dataGridViewSummary.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewSummary.Name = "dataGridViewSummary";
            // 
            // buttonRefresh
            // 
            resources.ApplyResources(buttonRefresh, "buttonRefresh");
            buttonRefresh.Name = "buttonRefresh";
            buttonRefresh.UseVisualStyleBackColor = true;
            buttonRefresh.Click += buttonRefresh_Click;
            // 
            // buttonWeek
            // 
            resources.ApplyResources(buttonWeek, "buttonWeek");
            buttonWeek.Name = "buttonWeek";
            buttonWeek.UseVisualStyleBackColor = true;
            buttonWeek.Click += buttonWeek_Click;
            // 
            // buttonMonth
            // 
            resources.ApplyResources(buttonMonth, "buttonMonth");
            buttonMonth.Name = "buttonMonth";
            buttonMonth.UseVisualStyleBackColor = true;
            buttonMonth.Click += buttonMonth_Click;
            // 
            // buttonYear
            // 
            resources.ApplyResources(buttonYear, "buttonYear");
            buttonYear.Name = "buttonYear";
            buttonYear.UseVisualStyleBackColor = true;
            buttonYear.Click += buttonYear_Click;
            // 
            // buttonAutoStart
            // 
            resources.ApplyResources(buttonAutoStart, "buttonAutoStart");
            buttonAutoStart.Name = "buttonAutoStart";
            buttonAutoStart.UseVisualStyleBackColor = true;
            buttonAutoStart.Click += buttonAutoStart_Click;
            // 
            // contextMenuStripTray
            // 
            resources.ApplyResources(contextMenuStripTray, "contextMenuStripTray");
            contextMenuStripTray.Items.AddRange(new ToolStripItem[] { toolStripMenuItemOpen, toolStripMenuItemExit });
            contextMenuStripTray.Name = "contextMenuStripTray";
            // 
            // toolStripMenuItemOpen
            // 
            resources.ApplyResources(toolStripMenuItemOpen, "toolStripMenuItemOpen");
            toolStripMenuItemOpen.Name = "toolStripMenuItemOpen";
            toolStripMenuItemOpen.Click += toolStripMenuItemOpen_Click;
            // 
            // toolStripMenuItemExit
            // 
            resources.ApplyResources(toolStripMenuItemExit, "toolStripMenuItemExit");
            toolStripMenuItemExit.Name = "toolStripMenuItemExit";
            toolStripMenuItemExit.Click += toolStripMenuItemExit_Click;
            // 
            // notifyIcon
            // 
            resources.ApplyResources(notifyIcon, "notifyIcon");
            notifyIcon.ContextMenuStrip = contextMenuStripTray;
            notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            // 
            // labelMilk
            // 
            resources.ApplyResources(labelMilk, "labelMilk");
            labelMilk.Name = "labelMilk";
            // 
            // textBoxIdAdmin
            // 
            resources.ApplyResources(textBoxIdAdmin, "textBoxIdAdmin");
            textBoxIdAdmin.Name = "textBoxIdAdmin";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // buttonUser
            // 
            resources.ApplyResources(buttonUser, "buttonUser");
            buttonUser.Name = "buttonUser";
            buttonUser.UseVisualStyleBackColor = true;
            buttonUser.Click += buttonUser_Click;
            // 
            // MainFormNew
            // 
            resources.ApplyResources(this, "$this");
            Controls.Add(buttonUser);
            Controls.Add(label1);
            Controls.Add(textBoxIdAdmin);
            Controls.Add(labelMilk);
            Controls.Add(buttonYear);
            Controls.Add(buttonMonth);
            Controls.Add(buttonWeek);
            Controls.Add(buttonRefresh);
            Controls.Add(dataGridViewSummary);
            Controls.Add(dateTimePicker);
            Controls.Add(textBoxCarton);
            Controls.Add(labelStatus);
            Controls.Add(buttonAutoStart);
            Controls.Add(buttonStop);
            Controls.Add(buttonStart);
            Controls.Add(textBoxToken);
            Controls.Add(labelToken);
            Name = "MainFormNew";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Resize += MainForm_Resize;
            ((System.ComponentModel.ISupportInitialize)dataGridViewSummary).EndInit();
            contextMenuStripTray.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelMilk;
        private TextBox textBoxIdAdmin;
        private Label label1;
        private Button buttonUser;
    }
}
