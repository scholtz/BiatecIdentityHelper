using BiatecIdentityHelper.Controllers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace BiatecIdentityHelper.Repository.Files
{
    public class FilesystemStorage : IFileStorage
    {
        private readonly ILogger<FilesystemStorage> _logger;
        private readonly IOptions<Model.ObjectStorage> _options;
        public FilesystemStorage(
            ILogger<FilesystemStorage> logger,
            IOptions<Model.ObjectStorage> options)
        {
            logger.LogInformation("Using filesystem storage");
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// List documents in folder
        /// 
        /// for example on input folder1 and filter .txt, the response lists all documents matching filter
        /// 
        /// >> 
        /// folder1/file.txt
        /// folder1/file.txt.1741519100.archive
        /// folder1/file.txt.1741519158.archive
        /// folder2/file.txt
        /// 
        /// returns
        /// >>
        /// 
        /// folder1/file.txt
        /// 
        /// </summary>
        /// <param name="folder">folder</param>
        /// <param name="filter">Filter extension - file must end on this text. If not defined the filter is not applied</param>
        /// <returns>list of files in folder</returns>
        public async Task<string[]> ListDocumentsInFolder(string folder, string filter)
        {
            var bucket = _options.Value.Bucket;
            if (!folder.EndsWith("/"))
            {
                folder = folder + "/";
            }
            if (!Directory.Exists(bucket))
                throw new DirectoryNotFoundException($"Folder not found: {bucket}");
            var files = Directory.GetFiles(bucket+"/"+ folder);
            if (string.IsNullOrEmpty(filter))
            {
                return files.Select(f => $"{folder}{Path.GetFileName(f)}").ToArray();
            }
            return
                files.Where(file => Path.GetFileName(file).EndsWith(filter))
                .Select(f => $"{folder}{Path.GetFileName(f)}")
                .ToArray();
        }

        /// <summary>
        /// List versions of the object key
        /// 
        /// for example on input file.txt the response may be
        /// 
        /// file.txt
        /// file.txt.1741519100.archive
        /// file.txt.1741519158.archive
        /// 
        /// indicating that the current document is file.txt, and it was modified twice at unix timestamps 1741519100 and 1741519158
        /// 
        /// it is possible to fetch the version from the load method
        /// </summary>
        /// <param name="objectKey"></param>
        /// <returns></returns>
        public async Task<string[]> ListVersions(string objectKey)
        {
            var rootFolder = "";
            var folder = _options.Value.Bucket;
            var file = objectKey;
            if (objectKey.Contains("/"))
            {
                var pos = objectKey.LastIndexOf("/");
                var path = objectKey.Substring(0, pos);

                folder = $"{_options.Value.Bucket}/{path}";
                rootFolder = path + "/";
                file = objectKey.Substring(pos + 1);
            }
            var fileName = Path.GetFileName(file);
            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException($"Folder not found: {folder}");

            return Directory.GetFiles(folder)
                            .Where(file => Path.GetFileName(file).StartsWith(fileName))
                            .Select(f => $"{rootFolder}{Path.GetFileName(f)}")
                            .ToArray();
        }

        /// <summary>
        /// Load file
        /// </summary>
        /// <param name="objectKey"></param>
        /// <returns></returns>
        public async Task<byte[]> Load(string objectKey)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.Value.Bucket))
                {
                    throw new Exception("Bucket not defined");
                }

                if (!Directory.Exists(_options.Value.Bucket))
                {
                    throw new Exception("Directory does not exists");
                }
                return await File.ReadAllBytesAsync($"{_options.Value.Bucket}/{objectKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while storing data in filesystem");
                throw;
            }
        }

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="objectKey"></param>
        /// <param name="fileBytes"></param>
        /// <returns></returns>
        public async Task<bool> Upload(string objectKey, byte[] fileBytes, string contentType = "application/x-binary", string acl = "private")
        {
            // if current object key exists create archive file first
            try
            {
                if (objectKey.EndsWith(".archive")) throw new Exception("Cannot archive the archive");
                var data = await Load(objectKey);
                if (data?.Length > 0)
                {
                    if (data.SequenceEqual(fileBytes))
                    {
                        // the file is already stored this way, no need to create archive
                        return true;
                    }
                    var archiveObjectKey = $"{objectKey}.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.archive";
                    await Upload(archiveObjectKey, data, contentType, acl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Current objectKey does not exists yet");
            }

            try
            {
                if (string.IsNullOrEmpty(_options.Value.Bucket))
                {
                    throw new Exception("Bucket not defined");
                }

                if (!Directory.Exists(_options.Value.Bucket))
                {
                    Directory.CreateDirectory(_options.Value.Bucket);
                }
                if (objectKey.Contains("/"))
                {
                    var pos = objectKey.LastIndexOf("/");
                    var path = objectKey.Substring(0, pos);

                    if (!Directory.Exists($"{_options.Value.Bucket}/{path}"))
                    {
                        Directory.CreateDirectory($"{_options.Value.Bucket}/{path}");
                    }
                }
                var file = $"{_options.Value.Bucket}/{objectKey}";
                await File.WriteAllBytesAsync(file, fileBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while storing data in filesystem");
                return false;
            }
            return true;
        }
    }
}
