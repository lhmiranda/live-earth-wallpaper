using LEWP.Common;
using LEWP.Himawari;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LEWP.Core
{
    public class TrayIconProccess : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip MainContextMenu;
        private ToolStripMenuItem ExitMenu;
        private ToolStripMenuItem SettingsMenu;
        private ToolStripMenuItem ForceStartMenu;
        private CancellationTokenSource cts;
        private Task service;
        const string appName = "Live Earth Wallpaper";
        IPhotoService work;

        public TrayIconProccess()
        {
            MainContextMenu = new ContextMenuStrip();
            MainContextMenu.Opening += OnMenuOpening;

            ExitMenu = new ToolStripMenuItem("Exit");
            ExitMenu.Click += KillApp;
            SettingsMenu = new ToolStripMenuItem("Settings...");
            SettingsMenu.Click += OpenSettings;
            ForceStartMenu = new ToolStripMenuItem("Force start");
            ForceStartMenu.Click += ForceStart;
            ForceStartMenu.Font = new Font(ForceStartMenu.Font, ForceStartMenu.Font.Style | FontStyle.Bold);

            MainContextMenu.Items.Add(ForceStartMenu);
            MainContextMenu.Items.Add(SettingsMenu);
            MainContextMenu.Items.Add(new ToolStripSeparator());
            MainContextMenu.Items.Add(ExitMenu);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = Properties.Resources.appico;
            trayIcon.Text = appName;
            trayIcon.Visible = true;
            trayIcon.ContextMenuStrip = MainContextMenu;
            trayIcon.BalloonTipTitle = appName;


            this.ThreadExit += OnCloseListener;

            cts = new CancellationTokenSource();
            work = new HimawariService(Notify);

            service = Task.Run(() => work.Start(cts.Token), cts.Token);
        }

        private void ForceStart(object sender, EventArgs e)
        {
            work.ForceStart();
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            var win = new FormSettings();
            win.ShowDialog();
            Properties.Settings.Default.Reload();
        }

        private void OnMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ForceStartMenu.Enabled = work.CanForce();
            if (service.IsCanceled)
            {
                ForceStartMenu.Enabled = SettingsMenu.Enabled = ExitMenu.Enabled = false;
            }            
        }

        private void KillApp(object sender, EventArgs e)
        {
            cts.Cancel();
            Notify(NotifificationType.Info, "Exiting...");
            try
            {
                service.Wait();
            }
            catch (AggregateException aEx)
            {
                foreach (var ex in aEx.InnerExceptions)
                {
                    var exception = ex as TaskCanceledException;
                    if (exception != null)
                    {
                        // Skip token cancellation messages.
                        continue;
                    }

                    MessageBox.Show("Unexpected error.\n\n" + ex.Message,
                        appName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                Application.Exit();
            }
        }

        private void OnCloseListener(object sender, System.EventArgs e)
        {
            trayIcon.Visible = false;
        }

        private void Notify(NotifificationType type, string message)
        {
            trayIcon.BalloonTipText = message;
            trayIcon.BalloonTipIcon = type == NotifificationType.Error ? ToolTipIcon.Error :
                (type == NotifificationType.Warning ? ToolTipIcon.Warning : ToolTipIcon.Info);
            trayIcon.ShowBalloonTip(6000);
        }
    }
}
