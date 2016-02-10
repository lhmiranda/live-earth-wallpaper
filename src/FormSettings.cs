using System;
using System.Windows.Forms;

namespace LEWP.Core
{
    public partial class FormSettings : Form
    {
        private int Interval;
        private int Difference;

        public FormSettings()
        {
            InitializeComponent();
            this.Interval = Properties.Settings.Default.Interval;
            this.Difference = Properties.Settings.Default.Difference;
        }

        private void FormSettingsOnLoad(object sender, EventArgs e)
        {
            txtInterval.Value = Interval;
            txtDifference.Value = Difference;
        }

        private void BtnCloseOnClick(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnSaveOnClick(object sender, EventArgs e)
        {
            if (txtInterval.Value != this.Interval || txtDifference.Value != this.Difference)
            {
                Properties.Settings.Default.Interval = (int)txtInterval.Value;
                Properties.Settings.Default.Difference= (int)txtDifference.Value;
                Properties.Settings.Default.Save();
                BtnSave.Enabled = false;
            }
        }

        private void OnChange(object sender, EventArgs e)
        {
            BtnSave.Enabled = (txtInterval.Value != this.Interval || txtDifference.Value != this.Difference);
        }
    }
}
