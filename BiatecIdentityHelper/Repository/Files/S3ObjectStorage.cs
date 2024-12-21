using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BiatecIdentityHelper.Controllers;
using Microsoft.Extensions.Options;
using System.Net.Mime;

namespace BiatecIdentityHelper.Repository.Files
{
    public class S3ObjectStorage : IFileStorage
    {
        private readonly ILogger<S3ObjectStorage> _logger;
        private readonly IOptions<Model.ObjectStorage> _options;
        public S3ObjectStorage(
            ILogger<S3ObjectStorage> logger,
            IOptions<Model.ObjectStorage> options)
        {
            logger.LogInformation($"Using S3 object storage {options.Value.Host}");
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
                RegionEndpoint linodeRegionEndpoint = RegionEndpoint.EUCentral1;
                //.GetBySystemName("eu-central-1.linodeobjects.com");
                AmazonS3Config awsConfig = new AmazonS3Config()
                {
                    RegionEndpoint = linodeRegionEndpoint,
                    ServiceURL = _options.Value.Host
                };
                var awsCredentials = new BasicAWSCredentials(_options.Value.Key, _options.Value.Secret);
                var awsClient = new AmazonS3Client(awsCredentials, awsConfig);
                var getRequest = new GetObjectRequest
                {
                    BucketName = _options.Value.Bucket,
                    Key = objectKey
                };

                var response = await awsClient.GetObjectAsync(getRequest);
                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Error while loading data from s3. Response code is {response.HttpStatusCode}");
                }
                // return data from response
                using var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while loading data from s3");
                throw;
            }
        }

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="objectKey"></param>
        /// <param name="fileBytes"></param>
        /// <returns></returns>
        public async Task<bool> Upload(
            string objectKey,
            byte[] fileBytes,
            string contentType = "application/x-binary",
            string acl = "private")
        {
            try
            {
                RegionEndpoint linodeRegionEndpoint = RegionEndpoint.EUCentral1;
                //.GetBySystemName("eu-central-1.linodeobjects.com");
                AmazonS3Config awsConfig = new AmazonS3Config()
                {
                    RegionEndpoint = linodeRegionEndpoint,
                    ServiceURL = _options.Value.Host
                };
                var awsCredentials = new BasicAWSCredentials(_options.Value.Key, _options.Value.Secret);
                var awsClient = new AmazonS3Client(awsCredentials, awsConfig);
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _options.Value.Bucket,
                    Key = objectKey,
                    ContentType = contentType,
                    InputStream = new MemoryStream(fileBytes),
                    CannedACL = acl
                };

                var response = await awsClient.PutObjectAsync(putRequest);
                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Error while storing data in filesystem. Response code is {response.HttpStatusCode}");
                }
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while storing data in filesystem");
                return false;
            }
        }
    }
}
