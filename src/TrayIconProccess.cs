using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using LEWP.Common;
using LEWP.Core.Properties;
using LEWP.Himawari;

namespace LEWP.Core
{
    public class TrayIconProccess : ApplicationContext
    {
        private readonly Version version = Assembly.GetExecutingAssembly().GetName().Version;
        private readonly string _appName;
        private readonly CancellationTokenSource _cts;
        private readonly ToolStripMenuItem _exitMenu;
        private readonly ToolStripMenuItem _forceStartMenu;
        private readonly Task _service;
        private readonly ToolStripMenuItem _settingsMenu;
        private readonly NotifyIcon _trayIcon;
        private readonly IPhotoService _work;

        public TrayIconProccess()
        {
            var mainContextMenu = new ContextMenuStrip();
            mainContextMenu.Opening += OnMenuOpening;

            _appName = $"Live Earth Wallpaper v{version.Major}.{version.Minor} build {version.Build}";

            _exitMenu = new ToolStripMenuItem("Exit");
            _exitMenu.Click += KillApp;
            _settingsMenu = new ToolStripMenuItem("Settings...");
            _settingsMenu.Click += OpenSettings;
            _forceStartMenu = new ToolStripMenuItem("Update now");
            _forceStartMenu.Click += ForceStart;
            _forceStartMenu.Font = new Font(_forceStartMenu.Font, _forceStartMenu.Font.Style | FontStyle.Bold);

            mainContextMenu.Items.Add(_forceStartMenu);
            mainContextMenu.Items.Add(_settingsMenu);
            mainContextMenu.Items.Add(new ToolStripSeparator());
            mainContextMenu.Items.Add(_exitMenu);

            _trayIcon = new NotifyIcon
            {
                Icon = Resources.appico,
                Text = _appName,
                Visible = true,
                ContextMenuStrip = mainContextMenu,
                BalloonTipTitle = _appName
            };

            ThreadExit += OnCloseListener;

            _cts = new CancellationTokenSource();
            _work = new HimawariService(Notify);

            _service = Task.Run(() => _work.Start(_cts.Token), _cts.Token);
        }

        private void ForceStart(object sender, EventArgs e)
        {
            _work.ForceStart();
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            var win = new FormSettings();
            win.ShowDialog();
            Settings.Default.Reload();
        }

        private void OnMenuOpening(object sender, CancelEventArgs e)
        {
            _forceStartMenu.Enabled = _work.CanForce();
            if (_service.IsCanceled)
            {
                _forceStartMenu.Enabled = _settingsMenu.Enabled = _exitMenu.Enabled = false;
            }
        }

        private void KillApp(object sender, EventArgs e)
        {
            _cts.Cancel();
            Notify(NotifificationType.Info, "Exiting...");
            try
            {
                _service.Wait();
            }
            catch (AggregateException aEx)
            {
                foreach (var ex in aEx.InnerExceptions
                    .Select(ex => new { ex, exception = ex as TaskCanceledException })
                    .Where(@t => @t.exception == null)
                    .Select(@t => @t.ex))
                {
                    MessageBox.Show("Unexpected error.\n\n" + ex.Message,
                        _appName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                Application.Exit();
            }
        }

        private void OnCloseListener(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
        }

        private void Notify(NotifificationType type, string message)
        {
            _trayIcon.BalloonTipText = message;
            _trayIcon.BalloonTipIcon = type == NotifificationType.Error
                ? ToolTipIcon.Error
                : (type == NotifificationType.Warning ? ToolTipIcon.Warning : ToolTipIcon.Info);
            _trayIcon.ShowBalloonTip(6000);
        }
    }
}