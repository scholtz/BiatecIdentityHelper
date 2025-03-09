// #define TestS3

using BiatecIdentityHelper.Controllers;
using BiatecIdentityHelper.Repository.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace BiatecIdentityHelper.Tests
{
    [TestFixture]
    public class FilesystemStorageTests
    {
#if TestS3
        private Mock<ILogger<S3ObjectStorage>> _mockLogger;
        private S3ObjectStorage _filesystemStorage;
#else
        private Mock<ILogger<FilesystemStorage>> _mockLogger;
        private FilesystemStorage _filesystemStorage;
#endif
        private Mock<IOptions<Model.ObjectStorage>> _mockOptions;
        private string _testBucketPath;

        [SetUp]
        public void Setup()
        {
#if TestS3
            _mockLogger = new Mock<ILogger<S3ObjectStorage>>();
#else
            _mockLogger = new Mock<ILogger<FilesystemStorage>>();
#endif
            _mockOptions = new Mock<IOptions<Model.ObjectStorage>>();

            // Setup a test bucket path
            _testBucketPath = Path.Combine(Path.GetTempPath(), "TestBucket");

#if TestS3
            _mockOptions.Setup(o => o.Value).Returns(new Model.ObjectStorage { Bucket = "aledger", Type = "AWS", Host = "https://eu-central-1.linodeobjects.com", Key = "ORB", Secret = "" });
            _filesystemStorage = new S3ObjectStorage(_mockLogger.Object, _mockOptions.Object);
#else
            _mockOptions.Setup(o => o.Value).Returns(new Model.ObjectStorage { Bucket = _testBucketPath, Type = "Filesystem" });
            _filesystemStorage = new FilesystemStorage(_mockLogger.Object, _mockOptions.Object);
#endif
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up the test bucket directory
            if (Directory.Exists(_testBucketPath))
            {
                Directory.Delete(_testBucketPath, true);
            }
        }

        [Test]
        public async Task Upload_File_Successfully()
        {
            // Arrange
            string objectKey = "testfile.txt";
            byte[] fileBytes = Encoding.UTF8.GetBytes("Test content");

            // Act
            bool result = await _filesystemStorage.Upload(objectKey, fileBytes);

            // Assert
            Assert.That(result, Is.True);

            var loaded = await _filesystemStorage.Load(objectKey);
            Assert.That(loaded, Is.EqualTo(fileBytes));
        }

        [Test]
        public async Task Upload_File_Creates_Directory_If_Not_Exists()
        {
            // Arrange
            string objectKey = "subfolder/testfile.txt";
            byte[] fileBytes = Encoding.UTF8.GetBytes("Test content");

            // Act
            bool result = await _filesystemStorage.Upload(objectKey, fileBytes);

            // Assert
            Assert.That(result, Is.True);
            var loaded = await _filesystemStorage.Load(objectKey);
            Assert.That(loaded, Is.EqualTo(fileBytes));
        }

        [Test]
        public async Task Upload_File_Fails_When_Exception_Occurs()
        {
            // Arrange
            string objectKey = "testfile.txt";
            byte[] fileBytes = Encoding.UTF8.GetBytes("Test content");

            // Simulate an error by setting a null bucket path
            _mockOptions.Setup(o => o.Value).Returns(new Model.ObjectStorage { Bucket = null });

            // Act
            bool result = await _filesystemStorage.Upload(objectKey, fileBytes);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ListVersions()
        {
            // Arrange
            string objectKey = "v/testfileVersioned.txt";
            byte[] fileBytes = Encoding.UTF8.GetBytes("Test content");
            byte[] fileBytes2 = Encoding.UTF8.GetBytes("Test content2");

            // Act
            bool result = await _filesystemStorage.Upload(objectKey, fileBytes);
            // Assert
            Assert.That(result, Is.True);
            // Act
            bool result2 = await _filesystemStorage.Upload(objectKey, fileBytes2);
            // Assert
            Assert.That(result, Is.True);

            var versions = await _filesystemStorage.ListVersions(objectKey);
            Assert.That(versions.Contains(objectKey));
            Assert.That(versions.Length > 1);

        }
    }
}
