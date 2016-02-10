namespace LEWP.Core
{
    public class AppSettings : IAppSettings
    {
        public int Interval
        {
            get
            {
                return Properties.Settings.Default.Interval;
            }
        }

        public int Difference 
        {
            get
            {
                return Properties.Settings.Default.Difference;
            }
        }
    }   
}
