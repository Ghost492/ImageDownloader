using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace ImageDownloader.Interfaces
{
    public interface IFileStorage
    {
        string SaveFile(Stream file, string fileName);
        string SaveFile(byte[] file, string fileName);
        Stream GetFile(string fileName);
    }
}
