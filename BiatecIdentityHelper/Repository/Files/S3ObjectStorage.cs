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
            var request = new ListObjectsV2Request
            {
                BucketName = _options.Value.Bucket,
                Prefix = objectKey
            };

            var result = new List<string>();
            ListObjectsV2Response response;

            RegionEndpoint linodeRegionEndpoint = RegionEndpoint.EUCentral1;
            AmazonS3Config awsConfig = new AmazonS3Config()
            {
                RegionEndpoint = linodeRegionEndpoint,
                ServiceURL = _options.Value.Host
            };
            var awsCredentials = new BasicAWSCredentials(_options.Value.Key, _options.Value.Secret);
            var awsClient = new AmazonS3Client(awsCredentials, awsConfig);
            do
            {
                response = await awsClient.ListObjectsV2Async(request);
                foreach (var obj in response.S3Objects)
                {
                    result.Add(obj.Key);
                }

                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return result.ToArray();
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
