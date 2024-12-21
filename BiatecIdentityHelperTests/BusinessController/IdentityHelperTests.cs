using BiatecIdentity;
using BiatecIdentityHelper.BusinessController;
using BiatecIdentityHelper.Controllers;
using BiatecIdentityHelper.Model;
using BiatecIdentityHelper.Repository.Files;
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BiatecIdentityHelperTests.BusinessController
{
    [TestFixture]
    public class IdentityHelperTests
    {

        private Mock<ILogger<IdentityHelper>> _mockLoggerHelper;
        private Mock<ILogger<BiatecIdentityHelper.Repository.Files.FilesystemStorage>> _mockLoggerFiles;
        private Mock<IOptions<BiatecIdentityHelper.Model.Config>> _mockOptionsHelper;
        private Mock<IOptions<BiatecIdentityHelper.Model.ObjectStorage>> _mockOptionsFiles;
        private IdentityHelper _identityHelper;
        private FilesystemStorage _filesystemStorage;
        private string _testBucketPath;
        private GrpcChannel channel;
        private DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient client;
        private BiatecIdentityHelper.Model.Config _config;
        private DerecCrypto.SignGenerateSigningKeyResponse signKeysHelper;
        private DerecCrypto.SignGenerateSigningKeyResponse signKeysGateway;
        private DerecCrypto.EncryptGenerateEncryptionKeyResponse encKeysHelper;
        private DerecCrypto.SignGenerateSigningKeyResponse encKeysGateway;
        [SetUp]
        public async Task Setup()
        {

            channel = GrpcChannel.ForAddress("http://localhost:50051");
            client = new DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient(channel);

            _mockLoggerFiles = new Mock<ILogger<FilesystemStorage>>();
            _mockLoggerHelper = new Mock<ILogger<IdentityHelper>>();
            _mockOptionsFiles = new Mock<IOptions<BiatecIdentityHelper.Model.ObjectStorage>>();
            _mockOptionsHelper = new Mock<IOptions<BiatecIdentityHelper.Model.Config>>();

            // Setup a test bucket path
            _testBucketPath = Path.Combine(Path.GetTempPath(), "TestBucket");
            _mockOptionsFiles.Setup(o => o.Value).Returns(new BiatecIdentityHelper.Model.ObjectStorage { Bucket = _testBucketPath });

            _filesystemStorage = new FilesystemStorage(_mockLoggerFiles.Object, _mockOptionsFiles.Object);

            signKeysHelper = await client.SignGenerateSigningKeyAsync(new DerecCrypto.SignGenerateSigningKeyRequest() { });
            signKeysGateway = await client.SignGenerateSigningKeyAsync(new DerecCrypto.SignGenerateSigningKeyRequest() { });
            encKeysHelper = await client.EncryptGenerateEncryptionKeyAsync(new DerecCrypto.EncryptGenerateEncryptionKeyRequest() { });
            encKeysGateway = await client.SignGenerateSigningKeyAsync(new DerecCrypto.SignGenerateSigningKeyRequest() { });

            _config = new BiatecIdentityHelper.Model.Config
            {
                GatewayEncryptionPublicKeyB64 = encKeysGateway.PublicKey.ToBase64(),
                GatewaySignaturePublicKeyB64 = signKeysGateway.PublicKey.ToBase64(),
                HelperEncryptionPrivateKeyB64 = encKeysHelper.PrivateKey.ToBase64(),
                HelperEncryptionPublicKeyB64 = encKeysHelper.PublicKey.ToBase64(),
                HelperSignaturePrivateKeyB64 = signKeysHelper.PrivateKey.ToBase64(),
                HelperSignaturePublicKeyB64 = signKeysHelper.PublicKey.ToBase64()
            };

            // Setup a options for main config
            _mockOptionsHelper.Setup(o => o.Value).Returns(_config);

            _identityHelper = new IdentityHelper(_mockLoggerHelper.Object, _mockOptionsHelper.Object, _filesystemStorage);

        }
        [TearDown]
        public void TearDown()
        {
            channel?.Dispose();
        }
        [Test]
        public async Task StoreDocumentTest()
        {
            var document = Encoding.UTF8.GetBytes("Test");
            var addr = "ADDR";
            var docId = "docId1";

            var doc = new BiatecIdentity.PostDocumentUnsigned()
            {
                Docid = Google.Protobuf.ByteString.CopyFromUtf8(docId),
                Identity = Google.Protobuf.ByteString.CopyFromUtf8(addr),
                Share = Google.Protobuf.ByteString.CopyFrom(document)
            };
            var docBytes = doc.ToByteString();
            var signed = await client.SignSignAsync(new DerecCrypto.SignSignRequest()
            {
                Message = docBytes,
                SecretKey = signKeysGateway.PrivateKey
            });
            var docSigned = new BiatecIdentity.PostDocumentSignedRequest()
            {
                Document = docBytes,
                Signature = signed.Signature
            };
            var encryptedSigned = await client.EncryptEncryptAsync(new DerecCrypto.EncryptEncryptRequest()
            {
                Message = docSigned.ToByteString(),
                PublicKey = encKeysHelper.PublicKey
            });
            var encryptedSignedBytes = encryptedSigned.Ciphertext.ToByteArray();

            var storedResponse = await _identityHelper.StoreDocumentAsync(encryptedSignedBytes);

            var decryptedStoredResponse = await client.EncryptDecryptAsync(new DerecCrypto.EncryptDecryptRequest() { Ciphertext = ByteString.CopyFrom(storedResponse), SecretKey = encKeysGateway.PrivateKey });
            var decryptedStoredResponseMsg = BiatecIdentity.PostDocumentSignedResponse.Parser.ParseFrom(decryptedStoredResponse.Message);
            var isSuccess = decryptedStoredResponseMsg.IsSuccess;
            Assert.That(isSuccess, Is.True);
            Assert.That(decryptedStoredResponseMsg.Result.Status, Is.EqualTo(StatusEnum.Ok));
            var decryptedStoredResponseCheckSign = await client.SignVerifyAsync(new DerecCrypto.SignVerifyRequest() { Message = ByteString.CopyFrom(BitConverter.GetBytes(isSuccess)), PublicKey = signKeysHelper.PublicKey, Signature = decryptedStoredResponseMsg.Signature });
            Assert.That(decryptedStoredResponseCheckSign.Valid, Is.True);

            var req = new BiatecIdentity.GetDocumentUnsigned()
            {
                Docid = Google.Protobuf.ByteString.CopyFromUtf8(docId),
                Identity = Google.Protobuf.ByteString.CopyFromUtf8(addr)
            };
            var reqBytes = req.ToByteString();
            var signedReq = await client.SignSignAsync(new DerecCrypto.SignSignRequest()
            {
                Message = reqBytes,
                SecretKey = signKeysGateway.PrivateKey
            });
            var reqSigned = new BiatecIdentity.PostDocumentSignedRequest()
            {
                Document = reqBytes,
                Signature = signedReq.Signature
            };
            var encryptedSignedReq = await client.EncryptEncryptAsync(new DerecCrypto.EncryptEncryptRequest()
            {
                Message = reqSigned.ToByteString(),
                PublicKey = encKeysHelper.PublicKey
            });
            var encryptedSignedReqBytes = encryptedSignedReq.Ciphertext.ToByteArray();
            var docRequestResponse = await _identityHelper.RequestDocumentAsync(encryptedSignedReqBytes);

            var encryptedResult = await client.EncryptDecryptAsync(new DerecCrypto.EncryptDecryptRequest()
            {
                Ciphertext = ByteString.CopyFrom(docRequestResponse),
                SecretKey = encKeysGateway.PrivateKey
            });

            var getDocumentSignedResponse = BiatecIdentity.GetDocumentSignedResponse.Parser.ParseFrom(encryptedResult.Message);
            Assert.That(getDocumentSignedResponse.Document.ToByteArray(), Is.EqualTo(document));
            Assert.That(decryptedStoredResponseMsg.Result.Status, Is.EqualTo(StatusEnum.Ok));
            var getDocumentSignedResponseCheckSign = await client.SignVerifyAsync(new DerecCrypto.SignVerifyRequest() { Message = getDocumentSignedResponse.Document, PublicKey = signKeysHelper.PublicKey, Signature = getDocumentSignedResponse.Signature });
            Assert.That(decryptedStoredResponseCheckSign.Valid, Is.True);
        }


    }
}
