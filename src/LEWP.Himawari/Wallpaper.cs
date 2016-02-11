using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace LEWP.Himawari
{
    public sealed class Wallpaper
    {
        public enum Style
        {
            Tile,
            Center,
            Stretch,
            Fit
        }

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public static void Set(string file, Style style)
        {
            var wpKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            if (style == Style.Stretch)
            {
                wpKey?.SetValue(@"WallpaperStyle", 2.ToString());
                wpKey?.SetValue(@"TileWallpaper", 0.ToString());
            }

            if (style == Style.Center)
            {
                wpKey?.SetValue(@"WallpaperStyle", 1.ToString());
                wpKey?.SetValue(@"TileWallpaper", 0.ToString());
            }

            if (style == Style.Tile)
            {
                wpKey?.SetValue(@"WallpaperStyle", 1.ToString());
                wpKey?.SetValue(@"TileWallpaper", 1.ToString());
            }

            if (style == Style.Fit)
            {
                wpKey?.SetValue(@"WallpaperStyle", 6.ToString());
                wpKey?.SetValue(@"TileWallpaper", 0.ToString());
            }

            var bgKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", true);
            bgKey?.SetValue(@"Background", "0 0 0");

            SystemParametersInfo(SPI_SETDESKWALLPAPER,
                0,
                file,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}