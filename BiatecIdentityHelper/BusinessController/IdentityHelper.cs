using BiatecIdentity;
using BiatecIdentityHelper.Repository.Files;
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using System;
using System.Text;

namespace BiatecIdentityHelper.BusinessController
{
    public class IdentityHelper
    {
        private readonly ILogger<IdentityHelper> _logger;
        private readonly IOptions<Model.Config> _options;
        private readonly IFileStorage _fileStorage;

        public IdentityHelper(
            ILogger<IdentityHelper> logger,
            IOptions<Model.Config> options,
            IFileStorage fileStorage
            )
        {
            _logger = logger;
            _options = options;
            _fileStorage = fileStorage;
        }
        /// <summary>
        /// Parse the encrypted request to fetch the document history
        /// </summary>
        /// <param name="encryptedRequest"></param>
        /// <returns></returns>
        public async Task<byte[]> GetDocumentVersionsAsync(byte[] encryptedRequest)
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:50051");
            var client = new DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient(channel);

            var gatewaySigPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewaySignaturePublicKeyB64);
            var gatewayEncPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewayEncryptionPublicKeyB64);
            var helperEncryptionPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperEncryptionPrivateKeyB64);
            var helperSignPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperSignaturePrivateKeyB64);

            var decryptedEncDocumentData = await client.EncryptDecryptAsync(new DerecCrypto.EncryptDecryptRequest()
            {
                Ciphertext = Google.Protobuf.ByteString.CopyFrom(encryptedRequest),
                SecretKey = helperEncryptionPrivateKey
            });

            var doc = BiatecIdentity.GetDocumentVersionsSignedRequest.Parser.ParseFrom(decryptedEncDocumentData.Message.Span);
            var verifyEncDocumentResult = await client.SignVerifyAsync(new DerecCrypto.SignVerifyRequest()
            {
                Message = doc.Document,
                PublicKey = gatewaySigPublicKey,
                Signature = doc.Signature
            });

            if (!verifyEncDocumentResult.Valid)
            {
                _logger.LogError("Invalid message received. The signature is not valid.");
                return await MakeGetDocumentVersionsResponse(client, "Invalid message received. The signature is not valid.", []);
            }
            var shareWithAuth = BiatecIdentity.GetDocumentVersionsUnsigned.Parser.ParseFrom(doc.Document);
            var identity = shareWithAuth.Identity.ToStringUtf8();
            var objectKey = MakeObjectKey(identity, shareWithAuth.Docid.ToStringUtf8());
            var result = await _fileStorage.ListVersions(objectKey);
            return await MakeGetDocumentVersionsResponse(client, "", result.Select(r => FullObjectKeyToDocId(r, identity)));
        }
        /// <summary>
        /// Create the encrypted response for the document history
        /// </summary>
        /// <param name="client">derec grpc security client</param>
        /// <param name="error">error message</param>
        /// <param name="result">the result.. the list of the documents</param>
        /// <returns></returns>
        private async Task<byte[]> MakeGetDocumentVersionsResponse(
            DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient client,
            string error,
            IEnumerable<string> result
            )
        {
            var gatewaySigPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewaySignaturePublicKeyB64);
            var gatewayEncPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewayEncryptionPublicKeyB64);
            var helperEncryptionPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperEncryptionPrivateKeyB64);
            var helperSignPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperSignaturePrivateKeyB64);

            var resMessage = new BiatecIdentity.GetDocumentVersionsSignedResponse() { };
            resMessage.Versions.AddRange(result.Select(v => ByteString.CopyFrom(Encoding.UTF8.GetBytes(v))));

            var resMessageSig = await client.SignSignAsync(new DerecCrypto.SignSignRequest()
            {
                Message = resMessage.ToByteString(),
                SecretKey = helperSignPrivateKey,
            });
            var versions = new Google.Protobuf.Collections.RepeatedField<ByteString>();
            versions.AddRange(result.Select(v => ByteString.CopyFrom(Encoding.UTF8.GetBytes(v))));

            resMessage.Result = new Result()
            {
                Memo = string.IsNullOrEmpty(error) ? "OK" : error,
                Status = string.IsNullOrEmpty(error) ? StatusEnum.Ok : StatusEnum.Fail,
            };
            resMessage.Signature = resMessageSig.Signature;

            var responseEncryptedForGateway = await client.EncryptEncryptAsync(new DerecCrypto.EncryptEncryptRequest()
            {
                Message = resMessage.ToByteString(),
                PublicKey = gatewayEncPublicKey,
            });

            var ret = responseEncryptedForGateway.Ciphertext.ToByteArray();
            return ret;
        }

        public async Task<byte[]> StoreDocumentAsync(byte[] encryptedDocument)
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:50051");
            var client = new DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient(channel);

            var gatewaySigPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewaySignaturePublicKeyB64);
            var gatewayEncPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewayEncryptionPublicKeyB64);
            var helperEncryptionPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperEncryptionPrivateKeyB64);
            var helperSignPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperSignaturePrivateKeyB64);

            var decryptedEncDocumentData = await client.EncryptDecryptAsync(new DerecCrypto.EncryptDecryptRequest()
            {
                Ciphertext = Google.Protobuf.ByteString.CopyFrom(encryptedDocument),
                SecretKey = helperEncryptionPrivateKey
            });

            var doc = BiatecIdentity.PostDocumentSignedRequest.Parser.ParseFrom(decryptedEncDocumentData.Message.Span);
            var verifyEncDocumentResult = await client.SignVerifyAsync(new DerecCrypto.SignVerifyRequest()
            {
                Message = doc.Document,
                PublicKey = gatewaySigPublicKey,
                Signature = doc.Signature
            });

            if (!verifyEncDocumentResult.Valid)
            {
                _logger.LogError("Invalid message received. The signature is not valid.");
                return await MakePostDocumentResponse(client, "Invalid message received. The signature is not valid.", false);
            }

            var shareWithAuth = BiatecIdentity.PostDocumentUnsigned.Parser.ParseFrom(doc.Document);
            var objectKey = MakeObjectKey(shareWithAuth.Identity.ToStringUtf8(), shareWithAuth.Docid.ToStringUtf8());
            var result = await _fileStorage.Upload(objectKey, encryptedDocument);
            return await MakePostDocumentResponse(client, "", result);
        }
        private async Task<byte[]> MakePostDocumentResponse(
            DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient client,
            string error,
            bool result)
        {
            var gatewaySigPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewaySignaturePublicKeyB64);
            var gatewayEncPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewayEncryptionPublicKeyB64);
            var helperEncryptionPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperEncryptionPrivateKeyB64);
            var helperSignPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperSignaturePrivateKeyB64);

            byte[] resultByteArray = BitConverter.GetBytes(result);

            var resMessageSig = await client.SignSignAsync(new DerecCrypto.SignSignRequest()
            {
                Message = ByteString.CopyFrom(resultByteArray),
                SecretKey = helperSignPrivateKey,
            });

            var resMessage = new BiatecIdentity.PostDocumentSignedResponse()
            {
                Result = new Result()
                {
                    Memo = string.IsNullOrEmpty(error) ? "OK" : error,
                    Status = string.IsNullOrEmpty(error) ? StatusEnum.Ok : StatusEnum.Fail,
                },
                IsSuccess = result,
                Signature = resMessageSig.Signature
            };
            var responseEncryptedForGateway = await client.EncryptEncryptAsync(new DerecCrypto.EncryptEncryptRequest()
            {
                Message = resMessage.ToByteString(),
                PublicKey = gatewayEncPublicKey,
            });

            var ret = responseEncryptedForGateway.Ciphertext.ToByteArray();
            return ret;
        }
        private string MakeObjectKey(string identity, string docId)
        {
            if (docId.EndsWith(".share") || (docId.Contains(".share.") && docId.EndsWith(".archive")))
            {
                return $"{_options.Value.RootDataFolder}/{identity}/{docId}";
            }
            else
            {
                return $"{_options.Value.RootDataFolder}/{identity}/{docId}.share";
            }
        }
        /// <summary>
        /// this method is used for version list backward output
        /// </summary>
        /// <param name="objectkey"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        private string FullObjectKeyToDocId(string objectkey, string identity)
        {
            var ret = objectkey.Replace($"{_options.Value.RootDataFolder}/{identity}/", "");
            if (objectkey.EndsWith(".share"))
            {
                return ret.Replace(".share", "");
            }
            else
            {
                return ret;
            }
        }

        public async Task<byte[]> RequestDocumentAsync(byte[] encryptedRequest)
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:50051");
            var client = new DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient(channel);

            var gatewaySigPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewaySignaturePublicKeyB64);
            var gatewayEncPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewayEncryptionPublicKeyB64);
            var helperEncryptionPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperEncryptionPrivateKeyB64);
            var helperSignPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperSignaturePrivateKeyB64);

            var decryptedData = await client.EncryptDecryptAsync(new DerecCrypto.EncryptDecryptRequest()
            {
                Ciphertext = Google.Protobuf.ByteString.CopyFrom(encryptedRequest),
                SecretKey = helperEncryptionPrivateKey
            });
            var doc = BiatecIdentity.GetDocumentSignedRequest.Parser.ParseFrom(decryptedData.Message.Span);
            var verifySignature = await client.SignVerifyAsync(new DerecCrypto.SignVerifyRequest()
            {
                Message = doc.DocumentRequestUnsignedBytes,
                PublicKey = gatewaySigPublicKey,
                Signature = doc.Signature
            });

            if (!verifySignature.Valid)
            {
                _logger.LogError("Invalid message received. The signature is not valid.");
                throw new Exception("The signature is not valid");
            }
            var docRequest = BiatecIdentity.GetDocumentUnsigned.Parser.ParseFrom(doc.DocumentRequestUnsignedBytes);
            var objectKey = MakeObjectKey(docRequest.Identity.ToStringUtf8(), docRequest.Docid.ToStringUtf8());

            var result = await _fileStorage.Load(objectKey);

            var decryptedEncDocumentData = await client.EncryptDecryptAsync(new DerecCrypto.EncryptDecryptRequest()
            {
                Ciphertext = Google.Protobuf.ByteString.CopyFrom(result),
                SecretKey = helperEncryptionPrivateKey
            });

            var docEncryptedDocumentSigned = BiatecIdentity.PostDocumentSignedRequest.Parser.ParseFrom(decryptedEncDocumentData.Message.Span);
            var verifyEncDocumentResult = await client.SignVerifyAsync(new DerecCrypto.SignVerifyRequest()
            {
                Message = docEncryptedDocumentSigned.Document,
                PublicKey = gatewaySigPublicKey,
                Signature = docEncryptedDocumentSigned.Signature
            });

            if (!verifyEncDocumentResult.Valid)
            {
                _logger.LogError("Invalid message received. The signature is not valid.");
                throw new Exception("The signature is not valid after the data has been loaded from the storage");
            }

            var shareWithAuth = BiatecIdentity.PostDocumentUnsigned.Parser.ParseFrom(docEncryptedDocumentSigned.Document);
            if (!shareWithAuth.Identity.Equals(docRequest.Identity))
            {
                throw new Exception("Identity mismatch after the data has been loaded from the storage");
            }
            // from the storage can be loaded document which has docId the original docId but in the request is the history request. In case loading from history, the docId in the request must start with the docId loaded from the storage. In case it is not history request, the both will match thus the requested file will start on the fetched file as well
            if (!docRequest.Docid.ToStringUtf8().StartsWith(shareWithAuth.Docid.ToStringUtf8()))
            {
                throw new Exception("Docid mismatch after the data has been loaded from the storage");
            }

            // encrypt the output with the gateway enc public key
            return await MakeGetDocumentResponse(client, "", shareWithAuth.Share.ToByteArray());
        }
        private async Task<byte[]> MakeGetDocumentResponse(
            DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient client,
            string error,
            byte[] data
            )
        {
            var gatewaySigPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewaySignaturePublicKeyB64);
            var gatewayEncPublicKey = Google.Protobuf.ByteString.FromBase64(_options.Value.GatewayEncryptionPublicKeyB64);
            var helperEncryptionPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperEncryptionPrivateKeyB64);
            var helperSignPrivateKey = Google.Protobuf.ByteString.FromBase64(_options.Value.HelperSignaturePrivateKeyB64);

            var resMessageSig = await client.SignSignAsync(new DerecCrypto.SignSignRequest()
            {
                Message = ByteString.CopyFrom(data),
                SecretKey = helperSignPrivateKey,
            });

            var resMessage = new BiatecIdentity.GetDocumentSignedResponse()
            {
                Result = new Result()
                {
                    Memo = string.IsNullOrEmpty(error) ? "OK" : error,
                    Status = string.IsNullOrEmpty(error) ? StatusEnum.Ok : StatusEnum.Fail,
                },
                Document = ByteString.CopyFrom(data),
                Signature = resMessageSig.Signature
            };
            var responseEncryptedForGateway = await client.EncryptEncryptAsync(new DerecCrypto.EncryptEncryptRequest()
            {
                Message = resMessage.ToByteString(),
                PublicKey = gatewayEncPublicKey,
            });
            var ret = responseEncryptedForGateway.Ciphertext.ToByteArray();
            return ret;
        }
    }
}
