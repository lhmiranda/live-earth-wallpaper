using LEWP.Common;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace LEWP.Himawari
{
    public class HimawariService : IPhotoService
    {
        private Action<NotifificationType, string> Notify;

        public HimawariService(Action<NotifificationType, string> notify)
        {
            Notify = notify;
        }

        public async Task Start(TimeSpan interval, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var imageInfo = GetLatestImageInfo();

                if (imageInfo != null)
                {
                    var image = AssembleImageFrom(imageInfo);
                    var imageFile = SaveImage(image);
                    Wallpaper.Set(imageFile, Wallpaper.Style.Fit);
                }

                if (interval > TimeSpan.Zero)
                {
                    await Task.Delay(interval, token);
                }
            }
        }

        private ImageSettings GetLatestImageInfo()
        {
            ImageSettings iSettings = null;
            try
            {
                using (var wc = new WebClient())
                {
                    var json = wc.DownloadString("http://himawari8-dl.nict.go.jp/himawari8/img/D531106/latest.json?" + Guid.NewGuid().ToString());
                    var iInfo = JsonConvert.DeserializeObject<ImageInfo>(json);
                    iSettings = new ImageSettings
                    {
                        Width = 550,
                        Level = "4d",
                        NumBlocks = 4,
                        TimeString = iInfo.Date.ToString("yyyy/MM/dd/HHmmss")
                    };
                }
            }
            catch (WebException ex)
            {
                Notify(NotifificationType.Error, "Error receiving latest image information: " + ex.Message);
            }
            catch (Exception ex)
            {
                Notify(NotifificationType.Error, "Unknown error receiving latest image information: " + ex.Message);
                throw;
            }

            return iSettings;
        }

        private Bitmap AssembleImageFrom(ImageSettings imageInfo)
        {
            Bitmap finalImage = null;
            var url = string.Format("http://himawari8-dl.nict.go.jp/himawari8/img/D531106/{0}/{1}/{2}", imageInfo.Level, imageInfo.Width, imageInfo.TimeString);
            finalImage = new Bitmap(imageInfo.Width * imageInfo.NumBlocks, imageInfo.Width * imageInfo.NumBlocks);
            var canvas = Graphics.FromImage(finalImage);
            canvas.Clear(Color.Black);
            try
            {
                for (var y = 0; y < imageInfo.NumBlocks; y++)
                {
                    for (var x = 0; x < imageInfo.NumBlocks; x++)
                    {
                        var cUrl = url + "_" + x + "_" + y + ".png";
                        var request = WebRequest.Create(cUrl);
                        var response = (HttpWebResponse)request.GetResponse();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (var imagePart = Image.FromStream(response.GetResponseStream()))
                            {
                                canvas.DrawImage(imagePart, x * imageInfo.Width, y * imageInfo.Width, imageInfo.Width, imageInfo.Width);
                            }
                        }

                        response.Close();
                    }
                }
            }
            catch (WebException ex)
            {
                Notify(NotifificationType.Error, "Error downloading latest image: " + ex.Message);
            }
            catch (Exception ex)
            {
                Notify(NotifificationType.Error, "Unknown error downloading latest image: " + ex.Message);
                throw;
            }

            return finalImage;
        }

        private string SaveImage(Bitmap finalImage)
        {
            var eParams = new EncoderParameters(1);
            eParams.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
            var jpegCodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");
            var pathName = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Earth\latest.jpg";
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(pathName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(pathName));
                }

                finalImage.Save(pathName, jpegCodecInfo, eParams);
            }
            catch (Exception ex)
            {
                Notify(NotifificationType.Error, "Error saving the image: " + ex.Message);
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
