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
using System.Collections.Generic;

namespace LEWP.DSCOVR
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
            var imageInfo = GetLatestImageInfo();
            if (imageInfo == null)
            {
                return null;
            }

            var image = AssembleImageFrom(imageInfo);

            return SaveImage(image);
        }

        private ImageInfo GetLatestImageInfo()
        {
            var date = DateTime.Now.AddDays(1).ToUniversalTime();
            var images = new List<ImageInfo>();
            var tries = 0;
            do
            {
                tries++;
                date = date.AddDays(-1);
                var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var cUrl = $"http://epic.gsfc.nasa.gov/api/images.php?date={dateString}&{Guid.NewGuid()}";
                try
                {
                    using (var wc = new WebClient())
                    {
                        var json = wc.DownloadString(cUrl);
                        images = JsonConvert.DeserializeObject<List<ImageInfo>>(json);
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
            } while (images.Count() == 0 || tries < 10);

            if (images.Count() == 0)
            {
                _notify(NotifificationType.Error, "Could not find any image to set as background.");
                return null;
            }

            return images.Last();
        }

        private Image AssembleImageFrom(ImageInfo imageInfo)
        {
            var url = $"http://epic.gsfc.nasa.gov/epic-archive/jpg/{imageInfo.Image}.jpg";
            Image finalImage = null;
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    var data = webClient.DownloadData(url);

                    using (MemoryStream mem = new MemoryStream(data))
                    {
                        finalImage = Image.FromStream(mem);
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