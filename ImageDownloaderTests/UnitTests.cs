using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using ImageDownloader.Helpers;
using ImageDownloader.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ImageDownloaderTests
{
    public class UnitTests
    {
        [SetUp]
        public void Setup()
        {
        }
        [Test]
        public void TestFileStorage()
        {
            var logger = new Mock<ILogger<FileStorageOnDrive>>(MockBehavior.Strict);
            var fileStorage = new FileStorageOnDrive(logger.Object, "storage");
            var filename = "1.jpg";
            using var file = File.OpenRead($"TestImages/{filename}");
            var savedFileName = fileStorage.SaveFile(file, filename);
            using var savedFile = fileStorage.GetFile(savedFileName);
            Assert.AreEqual(file.Length,savedFile.Length);
        }
        [Test]
        public void TestPreviewCreator()
        {
            IPreviewCreator previewCreator = new PreviewCreator();
            var filename = "1.jpg";
            using var file = File.OpenRead($"TestImages/{filename}");
            using var preview = new MemoryStream();
            previewCreator.CreatePreview(file, preview);
            Assert.Greater(preview.Length, 0);
            Assert.AreNotEqual(file.Length,preview.Length);
        }
    }
}
