using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageDownloader.Interfaces
{
    public interface IPreviewCreator
    {
        void CreatePreview(Stream fullImage, Stream outputStream);
    }
}
