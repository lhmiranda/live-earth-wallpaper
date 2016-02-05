using LEWP.Common;
using LEWP.Himawari;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LEWP.Core
{
    class TrayIconProccess : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip MainContextMenu;
        private ToolStripMenuItem ExitMenu;
        private CancellationTokenSource cts;
        private Task service;
        const string appName = "Live Earth Wallpaper";

        public TrayIconProccess()
        {
            MainContextMenu = new ContextMenuStrip();
            MainContextMenu.Opening += OnMenuOpening;

            ExitMenu = new ToolStripMenuItem("Exit");
            ExitMenu.Click += KillApp;

            MainContextMenu.Items.Add(ExitMenu);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = Properties.Resources.appico;
            trayIcon.Text = appName;
            trayIcon.Visible = true;
            trayIcon.ContextMenuStrip = MainContextMenu;
            trayIcon.BalloonTipTitle = appName;


            this.ThreadExit += OnCloseListener;

            cts = new CancellationTokenSource();
            IPhotoService work = new HimawariService(Notify);

            service = Task.Run(() => work.Start(TimeSpan.FromMinutes(60), cts.Token), cts.Token);
        }

        private void OnMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // From this event you can control which menu items appear (visibility or disabled) or 
            // even cancel the event and prevent the context menu from appearing at all
        }

        private void KillApp(object sender, System.EventArgs e)
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

                    MessageBox.Show("Unexpected error found.\n\n" + ex.Message,
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
