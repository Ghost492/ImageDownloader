using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageDownloader.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageDownloader.Helpers
{
    public class FileStorageOnDrive : IFileStorage
    {
        private readonly ILogger _logger;
        private readonly string _imagesDirectory;

        public FileStorageOnDrive(ILogger<FileStorageOnDrive> logger, string imagesDirectory)
        {
            _logger = logger;
            _imagesDirectory = imagesDirectory;
        }

        private void CheckImagesDirectoryExist()
        {
            if (!Directory.Exists(_imagesDirectory))
            {
                Directory.CreateDirectory(_imagesDirectory);
            }
        }
        public string SaveFile(Stream file, string fileName)
        {
            try
            {
                CheckImagesDirectoryExist();
                var filePath = GetFilePath(fileName);
                using var fileStream = File.OpenWrite(filePath);
                file.Seek(0, SeekOrigin.Begin);
                file.CopyTo(fileStream);
                return Path.GetFileName(filePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw e;
            }
        }

        public string SaveFile(byte[] file, string fileName)
        {
            try
            {
                CheckImagesDirectoryExist();
                var filePath = GetFilePath(fileName);
                using var fileStream = File.OpenWrite(filePath);
               fileStream.Write(file,0,file.Length);
               return Path.GetFileName(filePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw e;
            }
        }

        public Stream GetFile(string fileName)
        {
            var fullPath = Path.Combine(_imagesDirectory, fileName);
            if (File.Exists(fullPath))
            {
                return File.OpenRead(fullPath);
            }
            throw new ApplicationException($"File not found. File name: {fileName}");
        }

        private string GetFilePath(string fileName)
        {
            var filePath = Path.Combine(_imagesDirectory,fileName);
            if (File.Exists(filePath))
            {
                filePath = Path.Combine(_imagesDirectory,
                    $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.UtcNow.Ticks}{Path.GetExtension(fileName)}");
            }

            return filePath;
        }
    }
}
