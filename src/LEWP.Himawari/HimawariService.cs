using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using LEWP.Common;
using LEWP.Core.Properties;

using Newtonsoft.Json;
using System.Diagnostics;

namespace LEWP.Himawari
{
    public class HimawariService : IPhotoService
    {
        private CancellationTokenSource _internalTokenSource;
        private readonly Action<NotifificationType, string> _notify;

        public HimawariService(Action<NotifificationType, string> notify)
        {
            _notify = notify;
        }

        public async Task Start(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Settings.Default.Reload();
                var imageInfo = GetLatestImageInfo();
                if (imageInfo != null)
                {
                    var image = AssembleImageFrom(imageInfo);
                    var imageFile = SaveImage(image);
                    Wallpaper.Set(imageFile, Wallpaper.Style.Fit);
                }

                if (Settings.Default.Interval > 0)
                {
                    _internalTokenSource = new CancellationTokenSource();
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_internalTokenSource.Token, token))
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(Settings.Default.Interval), linkedCts.Token);
                        }
                        catch
                        {
                            // ignore exception raised by token cancellation
                        }
                    }
                }
            }
        }

        public void ForceStart()
        {
            _internalTokenSource?.Cancel();
        }

        public bool CanForce()
        {
            return _internalTokenSource != null && !_internalTokenSource.Token.IsCancellationRequested;
        }

        public async Task<Image> GetPreview(CancellationToken token, int offset)
        {
            Image image = null;
            var processTask = Task.Run(() =>
            {
                var imageInfo = GetLatestImageInfo(1, offset);
                if (imageInfo != null)
                {
                    return AssembleImageFrom(imageInfo, true);
                }

                return null;
            });
            await processTask.ContinueWith((task) =>
             {
                 image = task.Result;
             });

            return image;
        }

private ImageSettings GetLatestImageInfo(int zoom = 4, int offset = 1)
        {
            if (!new int[]{ 1,4,8,16,20}.Contains(zoom))
            {
                throw new ArgumentOutOfRangeException("zoom", "Zoom level must be 1, 4, 8, 16 or 20.");
            }

            if (offset == 1)
            {
                offset = Settings.Default.Difference;
            }

            ImageSettings iSettings = null;
            try
            {
                using (var wc = new WebClient())
                {
                    var json = wc.DownloadString("http://himawari8-dl.nict.go.jp/himawari8/img/D531106/latest.json?" + Guid.NewGuid());
                    // http://himawari8-dl.nict.go.jp/himawari8/img/D531106/4d/550/2017/07/19/191000_3_3.png
                    var iInfo = JsonConvert.DeserializeObject<ImageInfo>(json);
                    iSettings = new ImageSettings
                    {
                        Width = 550,
                        ZoomLevel = $"{zoom}d",  //Level can be 4d, 8d, 16d, 20d
                        BlockCount = zoom, // Keep this number the same as level
                        TimeString = iInfo.Date.AddHours(offset).ToString("yyyy/MM/dd/HHmmss", CultureInfo.InvariantCulture)
                    };
                }
            }
            catch (WebException ex)
            {
                _notify(NotifificationType.Error, "Error receiving image information: " + ex.Message);
            }
            catch (Exception ex)
            {
                _notify(NotifificationType.Error, "Unknown error receiving image information: " + ex.Message);
                throw;
            }

            return iSettings;
        }

        private Bitmap AssembleImageFrom(ImageSettings imageInfo, bool isPreview = false)
        {
            var url = $"http://himawari8-dl.nict.go.jp/himawari8/img/D531106/{imageInfo.ZoomLevel}/{imageInfo.Width}/{imageInfo.TimeString}";
            var finalImage = isPreview 
                ? new Bitmap(imageInfo.Width * imageInfo.BlockCount, imageInfo.Width * imageInfo.BlockCount) 
                : new Bitmap(imageInfo.Width*imageInfo.BlockCount, imageInfo.Width*imageInfo.BlockCount + 100);
            var canvas = Graphics.FromImage(finalImage);
            canvas.Clear(Color.Black);
            try
            {
                for (var y = 0; y < imageInfo.BlockCount; y++)
                {
                    for (var x = 0; x < imageInfo.BlockCount; x++)
                    {
                        var cUrl = $"{url}_{x}_{y}.png";
                        var request = WebRequest.Create(cUrl);
                        var response = (HttpWebResponse) request.GetResponse();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (var imagePart = Image.FromStream(response.GetResponseStream()))
                            {
                                canvas.DrawImage(imagePart, x*imageInfo.Width, y*imageInfo.Width, imageInfo.Width, imageInfo.Width);
                            }
                        }

                        response.Close();
                    }
                }
            }
            catch (WebException ex)
            {
                _notify(NotifificationType.Error, "Error downloading image: " + ex.Message);
            }
            catch (Exception ex)
            {
                _notify(NotifificationType.Error, "Unknown error downloading image: " + ex.Message);
                throw;
            }

            return finalImage;
        }

        private string SaveImage(Bitmap finalImage)
        {
            var eParams = new EncoderParameters(1)
            {
                Param = {[0] = new EncoderParameter(Encoder.Quality, 95L)}
            };
            var jpegCodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");
            var pathName = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Earth\latest.jpg";
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(pathName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(pathName));
                }

                if (jpegCodecInfo != null) finalImage.Save(pathName, jpegCodecInfo, eParams);
            }
            catch (Exception ex)
            {
                _notify(NotifificationType.Error, "Error saving the image: " + ex.Message);
                throw;
            }
            finally
            {
                finalImage.Dispose();
            }

            return pathName;
        }
    }
}