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

using Newtonsoft.Json;
using LEWP.Core.Properties;

namespace LEWP.Himawari
{
    public class HimawariService : IImageSource
    {
        private readonly Action<NotifificationType, string> _notify;

        public HimawariService(Action<NotifificationType, string> notify)
        {
            _notify = notify;
        }

        public string GetImage(CancellationToken token)
        {
            Settings.Default.Reload();
            var imageInfo = GetLatestImageInfo();
            if (imageInfo == null)
            {
                return null;
            }

            var image = AssembleImageFrom(imageInfo);

            return SaveImage(image);
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
                Parallel.For(0, imageInfo.NumBlocks, y =>
                {
                    Parallel.For(0, imageInfo.NumBlocks, x =>
                    {
                        var cUrl = $"{url}_{x}_{y}.png";
                        var request = WebRequest.Create(cUrl);
                        var response = (HttpWebResponse)request.GetResponse();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (var imagePart = Image.FromStream(response.GetResponseStream()))
                            {
                                lock(canvas)
                                {
                                    canvas.DrawImage(imagePart, x * imageInfo.Width, y * imageInfo.Width, imageInfo.Width, imageInfo.Width);
                                }
                            }
                        }

                        response.Close();
                    });
                });
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

        private string SaveImage(Image finalImage)
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