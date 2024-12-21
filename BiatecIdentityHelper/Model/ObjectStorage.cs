namespace BiatecIdentityHelper.Model
{
    public class ObjectStorage
    {
        /// <summary>
        /// AWS for S3 storage
        /// Filesystem for local filesystem
        /// </summary>
        public string Type { get; set; } = "AWS";
        /// <summary>
        /// S3 host
        /// </summary>
        public string Host { get; set; } = string.Empty;
        /// <summary>
        /// S3 Bucker or folder in filesystem
        /// </summary>
        public string Bucket { get; set; } = string.Empty;
        /// <summary>
        /// S3 key
        /// </summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// S3 secret
        /// </summary>
        public string Secret { get; set; } = string.Empty;
    }
}
