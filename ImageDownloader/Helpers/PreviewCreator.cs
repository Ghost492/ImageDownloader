using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageDownloader.Interfaces;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageDownloader.Helpers
{
    public class PreviewCreator :IPreviewCreator
    {
        private const int previewWidth = 100;
        private const int previewHeight = 100;
        public void CreatePreview(Stream fullImage, Stream outputStream)
        {
            var callback =
                new Image.GetThumbnailImageAbort(() => false);
            var image = Image.FromStream(fullImage);
            var thumbnailImage = image.GetThumbnailImage(previewWidth, previewHeight, callback, IntPtr.Zero);
            thumbnailImage.Save(outputStream,ImageFormat.Jpeg);
        }
    }
}
