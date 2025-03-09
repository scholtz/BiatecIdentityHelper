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
        public Task<string[]> ListVersions(string objectKey);
    }
}
