using BiatecIdentityHelper.BusinessController;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;

namespace BiatecIdentityHelper.Controllers
{
    [ApiController]
    public class IdentityHelperController : ControllerBase
    {

        private readonly ILogger<IdentityHelperController> _logger;
        private readonly IdentityHelper _identityHelper;

        public IdentityHelperController(
            ILogger<IdentityHelperController> logger,
            IdentityHelper identityHelper)
        {
            _logger = logger;
            _identityHelper = identityHelper;
        }

        /// <summary>
        /// Stores the document
        /// </summary>
        /// <param name="data">Encrypted by helper public key, signed with Gateway private key</param>
        /// <returns>True if document has been stored</returns>
        [Route("/v1/store-document")]
        [HttpPost]
        public Task<byte[]> StoreDocument([FromBody] byte[] data)
        {
            _logger.LogInformation($"Document {data.Length}");
            return _identityHelper.StoreDocumentAsync(data);
        }

        /// <summary>
        /// Stores the document
        /// </summary>
        /// <param name="data">Encrypted by helper public key, signed with Gateway private key</param>
        /// <returns>True if document has been stored</returns>
        [Route("/v1/get-document")]
        [HttpPost]
        public Task<byte[]> GetDocument([FromBody] byte[] request)
        {
            _logger.LogInformation($"GetDocument request {request.Length}");
            return _identityHelper.RequestDocumentAsync(request);
        }
    }
}
