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
        private Mock<ILogger<FilesystemStorage>> _mockLogger;
        private Mock<IOptions<Model.ObjectStorage>> _mockOptions;
        private FilesystemStorage _filesystemStorage;
        private string _testBucketPath;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<FilesystemStorage>>();
            _mockOptions = new Mock<IOptions<Model.ObjectStorage>>();

            // Setup a test bucket path
            _testBucketPath = Path.Combine(Path.GetTempPath(), "TestBucket");
            _mockOptions.Setup(o => o.Value).Returns(new Model.ObjectStorage { Bucket = _testBucketPath });

            _filesystemStorage = new FilesystemStorage(_mockLogger.Object, _mockOptions.Object);
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
            string filePath = Path.Combine(_testBucketPath, objectKey);
            Assert.That(File.Exists(filePath), Is.True);
            string content = await File.ReadAllTextAsync(filePath);
            Assert.That(content, Is.EqualTo("Test content"));
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
            string filePath = Path.Combine(_testBucketPath, objectKey);
            Assert.That(File.Exists(filePath), Is.True);
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
    }
}
