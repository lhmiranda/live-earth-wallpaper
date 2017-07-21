using System;
using System.Windows.Forms;

using LEWP.Core.Properties;
using System.Threading;
using LEWP.Himawari;
using LEWP.Common;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LEWP.Core
{
    public partial class FormSettings : Form
    {
        private readonly int _savedOffset;
        private readonly int _savedInterval;
        private int previousOffset;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _oldCts;
        private Action<NotifificationType, string> _notify;

        public FormSettings(Action<NotifificationType, string> notify)
        {
            InitializeComponent();
            _savedInterval = Settings.Default.Interval;
            _savedOffset = previousOffset = Settings.Default.Difference;
            UpdateTracker();
            _notify = notify;
            ImagePanel.BackgroundImageLayout = ImageLayout.Stretch;
            lblMin.Text = TrackbarOffset.Minimum.ToString();
            lblMax.Text = TrackbarOffset.Maximum.ToString();
        }

        private void UpdateTracker()
        {
            if (TrackbarOffset.Value == 0)
            {
                lblCur.Text = "Current time";
            }
            else
            {
                lblCur.Text = $"-{TrackbarOffset.Value * -1} hour{(TrackbarOffset.Value != -1 ? "s" : "")}";
            }
        }

        private void FormSettingsOnLoad(object sender, EventArgs e)
        {
            txtInterval.Value = _savedInterval;
            TrackbarOffset.Value = _savedOffset;
            UpdatePreview();
        }

        private void BtnCloseOnClick(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnSaveOnClick(object sender, EventArgs e)
        {
            if (txtInterval.Value != _savedInterval || TrackbarOffset.Value != _savedOffset)
            {
                Settings.Default.Interval = (int) txtInterval.Value;
                Settings.Default.Difference = TrackbarOffset.Value;
                Settings.Default.Save();
            }

            Close();
        }

        private void TxtTrackbar_ValueChanged(object sender, EventArgs e)
        {
            UpdateTracker();
        }

        private void TxtTrackbar_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (TrackbarOffset.Value != previousOffset)
            {
                previousOffset = TrackbarOffset.Value;
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            ImagePanel.BackgroundImage = null;
            LblPreview.Visible = true;
            if (_oldCts != null)
            {
                _oldCts.Cancel();
            }

            _cts = new CancellationTokenSource();
            var svc = new HimawariService(_notify);
            var offset = TrackbarOffset.Value;
            Task.Run(() => svc.GetPreview(_cts.Token, offset), _cts.Token).ContinueWith((task) => 
            {
                try
                {
                    var img = task.Result;
                    if (img != null)
                    {
                        ImagePanel.BackgroundImage = img;
                    }

                    LblPreview.Visible = false;
                }
                catch (Exception ex)
                {
                    _notify(NotifificationType.Error, "Error loading the preview: " + ex.Message);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());

            _oldCts = _cts;
        }
    }
}