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

        private ImageSettings GetLatestImageInfo()
        {
            ImageSettings iSettings = null;
            try
            {
                using (var wc = new WebClient())
                {
                    var json = wc.DownloadString("http://himawari8-dl.nict.go.jp/himawari8/img/D531106/latest.json?" + Guid.NewGuid());
                    var iInfo = JsonConvert.DeserializeObject<ImageInfo>(json);
                    iSettings = new ImageSettings
                    {
                        Width = 550,
                        Level = "4d",
                        NumBlocks = 4,
                        TimeString = iInfo.Date.AddHours(Settings.Default.Difference).ToString("yyyy/MM/dd/HHmmss", CultureInfo.InvariantCulture)
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

        private Bitmap AssembleImageFrom(ImageSettings imageInfo)
        {
            var url = $"http://himawari8-dl.nict.go.jp/himawari8/img/D531106/{imageInfo.Level}/{imageInfo.Width}/{imageInfo.TimeString}";
            var finalImage = new Bitmap(imageInfo.Width*imageInfo.NumBlocks, imageInfo.Width*imageInfo.NumBlocks + 100);
            var canvas = Graphics.FromImage(finalImage);
            canvas.Clear(Color.Black);
            try
            {
                for (var y = 0; y < imageInfo.NumBlocks; y++)
                {
                    for (var x = 0; x < imageInfo.NumBlocks; x++)
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