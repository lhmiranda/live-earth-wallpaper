using System;
using System.Windows.Forms;

using LEWP.Core.Properties;

namespace LEWP.Core
{
    public partial class FormSettings : Form
    {
        private readonly int _difference;
        private readonly int _interval;

        public FormSettings()
        {
            InitializeComponent();
            _interval = Settings.Default.Interval;
            _difference = Settings.Default.Difference;
        }

        private void FormSettingsOnLoad(object sender, EventArgs e)
        {
            txtInterval.Value = _interval;
            txtDifference.Value = _difference;
        }

        private void BtnCloseOnClick(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnSaveOnClick(object sender, EventArgs e)
        {
            if (txtInterval.Value != _interval || txtDifference.Value != _difference)
            {
                Settings.Default.Interval = (int) txtInterval.Value;
                Settings.Default.Difference = (int) txtDifference.Value;
                Settings.Default.Save();
                BtnSave.Enabled = false;
            }
        }

        private void OnChange(object sender, EventArgs e)
        {
            BtnSave.Enabled = txtInterval.Value != _interval || txtDifference.Value != _difference;
        }
    }
}