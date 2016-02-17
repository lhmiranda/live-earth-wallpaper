using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using LEWP.Common;

using Newtonsoft.Json;
using System.Collections.Generic;
using LEWP.Core.Properties;

namespace LEWP.DSCOVR
{
    public class DscovrService : IImageSource
    {
        private readonly Action<NotifificationType, string> _notify;

        public DscovrService(Action<NotifificationType, string> notify)
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

            return AssembleImageFrom(imageInfo);
        }

        private ImageInfo GetLatestImageInfo()
        {
            var date = DateTime.Now.AddDays(-21).ToUniversalTime();
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

            var imgNumber = Settings.Default.ImageNumber - 1;
            ImageInfo img = null;

            if (images.Count <= imgNumber)
            {
                img = images[imgNumber];
            }
            else
            {
                img = images.Last();
            }

            return img;
        }

        private string AssembleImageFrom(ImageInfo imageInfo)
        {
            var url = $"http://epic.gsfc.nasa.gov/epic-archive/jpg/{imageInfo.Image}.jpg";
            var pathName = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Earth\dscovr-latest.jpg";
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(pathName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(pathName));
                }

                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(url, pathName);
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

            return pathName;
        }
    }
}