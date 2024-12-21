using BiatecIdentityHelper.Controllers;
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
                await File.WriteAllBytesAsync($"{_options.Value.Bucket}/{objectKey}", fileBytes);
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
