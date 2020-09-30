using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageDownloader.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ImageDownloader.Controllers
{
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IFileStorage _fileStorage;
        private readonly IPreviewCreator _previewCreator;

        public ImagesController(ILogger<ImagesController> logger, IFileStorage fileStorage, IPreviewCreator previewCreator)
        {
            _logger = logger;
            _fileStorage = fileStorage;
            _previewCreator = previewCreator;
        }
       
        [HttpPost("download")]
        public async Task<ActionResult> Download()
        {
            try
            {
                if (HttpContext.Request.ContentType == null)
                {
                    throw new ApplicationException("Unknown Content Type");
                }
                List<string> imageNames;
                if (HttpContext.Request.ContentType.Contains("text/html"))
                {
                    imageNames = await SaveFromUrl(HttpContext.Request.Body);
                }
                else if (HttpContext.Request.ContentType.Contains("application/json"))
                {
                    imageNames = await SaveFromBase64(HttpContext.Request.Body);
                }
                else if (HttpContext.Request.ContentType.Contains("multipart/form-data"))
                {
                    imageNames = await SaveFromFiles(HttpContext.Request.Form.Files);
                }
                else
                {
                    throw new ApplicationException("Unsupported Content Type");
                }

                foreach (var imageName in imageNames)
                {
                    CreatePreview(imageName);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest();
            }

            return StatusCode((int)HttpStatusCode.OK);
        }
        private async Task<string> GetBody(Stream stream)
        {
            using (var test = HttpContext.Request.Body)
            {
                using (var mStream = new MemoryStream())
                {
                    await test.CopyToAsync(mStream);
                    var bytes = mStream.ToArray();
                    return Encoding.UTF8.GetString(bytes);
                }
            }

        }

        private async Task<List<string>> SaveFromFiles(IFormFileCollection files)
        {
            var result = new List<string>(files.Count);
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!IsImageExtension(extension?.TrimStart('.')))
                {
                    throw new ApplicationException($"Unexpected file format: {extension}.");
                }
                await using var stream = file.OpenReadStream();
                var savedFileName =_fileStorage.SaveFile(stream, file.FileName);
                result.Add(savedFileName);
            }
            return result;
        }

        private async Task<List<string>> SaveFromBase64(Stream body)
        {
            var str = await GetBody(body);
            var array = JsonConvert.DeserializeObject<string[]>(str);
            var result = new List<string>(array.Length);
            foreach (var imgString in array)
            {
                var strings = imgString.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
                if (strings.Length != 2)
                {
                    throw new ApplicationException("It is not base64 image format");
                }

                var extensionInfo = strings[0];
                var base64Data = strings[1];
                var extension = Regex.Match(extensionInfo, "data:image/(?<extension>\\w*);base64")
                    .Groups["extension"];
                if (!extension.Success || string.IsNullOrEmpty(extension.Value))
                {
                    throw new ApplicationException("Extension not found");
                }
                if (!IsImageExtension(extension.Value))
                {
                    throw new ApplicationException("Is not image");
                }
                var encodedData = Convert.FromBase64String(base64Data);
                var savedFileName = _fileStorage.SaveFile(encodedData, $"{Guid.NewGuid()}.{extension.Value}");
               result.Add(savedFileName);
            }

            return result;
        }

        private async Task<List<string>> SaveFromUrl(Stream body)
        {
            var result = new List<string>();
            var str = await GetBody(body);
            if (Uri.TryCreate(str, UriKind.Absolute, out var uriResult))
            {
                var fileExt = Path.GetExtension(uriResult.LocalPath);
                if (!IsImageExtension(fileExt.TrimStart('.')))
                {
                    throw new ApplicationException("Url not contains image");
                }
                var fileName = Path.GetFileName(uriResult.LocalPath);
                var file = await DownloadImageByUrl(uriResult);
                var savedFileName = _fileStorage.SaveFile(file, fileName);
                result.Add(savedFileName);
            }
            return result;
        }

        private async Task<byte[]> DownloadImageByUrl(Uri uri)
        {
            using (var webClient = new WebClient())
            {
                return webClient.DownloadData(uri);
            }
        }
        private static Lazy<string[]> imageFormats = new Lazy<string[]>(() =>
        {
            return typeof(System.Drawing.Imaging.ImageFormat)
                .GetProperties()
                .Select(x => x.Name)
                .Union(new []{"jpg"})
                .ToArray();
        });
        private bool IsImageExtension(string ext)
        {
            return imageFormats.Value.Any(x => x.Equals(ext, StringComparison.InvariantCultureIgnoreCase));
        }
        private void CreatePreview(string fileName)
        {
            using var fullSizeImageStream = _fileStorage.GetFile(fileName);
            using var mStream = new MemoryStream();
            _previewCreator.CreatePreview(fullSizeImageStream, mStream);
            var previewFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_preview.jpg";
            _fileStorage.SaveFile(mStream, previewFileName);
        }
    }
}
