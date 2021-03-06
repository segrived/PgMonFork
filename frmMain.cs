﻿using System;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;
using Microsoft.Win32.TaskScheduler;
using CommandLine;
using System.Drawing;
using System.IO;

namespace PgMonFork
{
    public partial class frmMain : Form
    {
        public enum ServiceStatus
        {
            Start, Stop, Restart
        }

        private Icon StartingIcon;
        private Icon StartedIcon;
        private Icon StoppedIcon;

        private ServiceController pgservice;

        public frmMain()
        {
            InitializeComponent();
            InitApp();
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        private void RegisterTask()
        {
            var currentFn = System.Reflection.Assembly.GetEntryAssembly().Location;
            using (var ts = new TaskService()) {
                var task = ts.NewTask();
                task.Triggers.Add(Trigger.CreateTrigger(TaskTriggerType.Logon));
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Principal.RunLevel = TaskRunLevel.Highest;
                task.Settings.IdleSettings.StopOnIdleEnd = false;
                task.Settings.ExecutionTimeLimit = TimeSpan.FromDays(730);
                task.Actions.Add(new ExecAction(currentFn, "", null));
                ts.RootFolder.RegisterTaskDefinition(@"PgMonFork", task);
            }
        }

        private string GetExecutableDirectory()
        {
            var currentFn = System.Reflection.Assembly.GetEntryAssembly().Location;
            var exeDirectory = Path.GetDirectoryName(currentFn);
            return exeDirectory;
        }

        private void InitApp()
        {
            try {
                string iconsDirectory = Path.Combine(GetExecutableDirectory(), "Icons");
                StartingIcon = new Icon(Path.Combine(iconsDirectory, "starting.ico"));
                StartedIcon = new Icon(Path.Combine(iconsDirectory, "started.ico"));
                StoppedIcon = new Icon(Path.Combine(iconsDirectory, "stopped.ico"));
            } catch (Exception e) {
                MessageBox.Show($"Error while loading icons: {e.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            string[] args = Environment.GetCommandLineArgs();
            var result = Parser.Default.ParseArguments<CmdOptions>(args);

            int refreshEvery = 2;

            result.WithParsed(p => {
                if(p.InstallTask) {
                    RegisterTask();
                }
                if(p.Interval >= 1) {
                    refreshEvery = p.Interval;
                }
            }).WithNotParsed(p => MessageBox.Show($"Invalid arguments, using defaults"));

            timerScanService.Interval = refreshEvery * 1000;
            this.updateIntervalToolStripMenuItem.Text = $"Refresh interval: {refreshEvery} seconds";

            try {
                var services = ServiceController.GetServices();
                pgservice = services.First(s => s.ServiceName.StartsWith("postgresql-"));
                RefreshTrayIcon();
            } catch (InvalidOperationException) {
                MessageBox.Show("Service not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            timerScanService.Enabled = true;
        }

        public void RefreshTrayIcon()
        {
            GC.Collect();
            pgservice.Refresh();

            if (pgservice.Status == ServiceControllerStatus.Running) {
                traynotifyIcon.Icon = StartedIcon;
            } else if (pgservice.Status == ServiceControllerStatus.Stopped) {
                traynotifyIcon.Icon = StoppedIcon;
            } else if(pgservice.Status == ServiceControllerStatus.StartPending 
                || pgservice.Status == ServiceControllerStatus.StopPending) {
                traynotifyIcon.Icon = StartingIcon;
            } 
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            RefreshTrayIcon();
        }

        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) {
                Hide();
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.TaskManagerClosing) {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
                traynotifyIcon.Visible = true;
            }
        }

        private void ToggleServiceStatus()
        {
            if (pgservice.Status == ServiceControllerStatus.Running || pgservice.Status == ServiceControllerStatus.StartPending) {
                pgservice.Stop();
            }

            if (pgservice.Status == ServiceControllerStatus.Stopped || pgservice.Status == ServiceControllerStatus.StopPending) {
                pgservice.Start();
            }
        }

        private void traynotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle) {
                return;
            }
            try {
                ToggleServiceStatus();
                RefreshTrayIcon();
            } catch {
                //...
            }
        }
    }
}