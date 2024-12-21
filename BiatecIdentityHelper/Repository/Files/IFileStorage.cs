using BiatecIdentityHelper.Model;

namespace BiatecIdentityHelper.Repository.Files
{
    public interface IFileStorage
    {
        /// <summary>
        /// Loads data from file storage
        /// </summary>
        /// <param name="objectKey">path</param>
        /// <returns></returns>
        public Task<byte[]> Load(string objectKey);

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="objectKey"></param>
        /// <param name="fileBytes"></param>
        /// <returns></returns>
        public Task<bool> Upload(
            string objectKey,
            byte[] fileBytes,
            string contentType = "application/x-binary",
            string acl = "private");
    }
}
