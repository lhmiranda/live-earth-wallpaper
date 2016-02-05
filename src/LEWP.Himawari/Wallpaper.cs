using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace LEWP.Himawari
{
    public sealed class Wallpaper
    {
        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public enum Style : int
        {
            Tile,
            Center,
            Stretch,
            Fit
        }

        public static void Set(string file, Style style)
        {
            RegistryKey wpKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            if (style == Style.Stretch)
            {
                wpKey.SetValue(@"WallpaperStyle", 2.ToString());
                wpKey.SetValue(@"TileWallpaper", 0.ToString());
            }

            if (style == Style.Center)
            {
                wpKey.SetValue(@"WallpaperStyle", 1.ToString());
                wpKey.SetValue(@"TileWallpaper", 0.ToString());
            }

            if (style == Style.Tile)
            {
                wpKey.SetValue(@"WallpaperStyle", 1.ToString());
                wpKey.SetValue(@"TileWallpaper", 1.ToString());
            }

            if (style == Style.Fit)
            {
                wpKey.SetValue(@"WallpaperStyle", 6.ToString());
                wpKey.SetValue(@"TileWallpaper", 0.ToString());
            }

            RegistryKey bgKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", true);
            bgKey.SetValue(@"Background", "0 0 0");

            SystemParametersInfo(SPI_SETDESKWALLPAPER,
                0,
                file,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}
