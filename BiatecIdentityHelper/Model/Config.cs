namespace BiatecIdentityHelper.Model
{
    public class Config
    {
        /// <summary>
        /// Gateway s public key in base64
        /// </summary>
        public string GatewaySignaturePublicKeyB64 { get; set; } = string.Empty;
        /// <summary>
        /// Gateway e public key in base64
        /// </summary>
        public string GatewayEncryptionPublicKeyB64 { get; set; } = string.Empty;

        /// <summary>
        /// Helper s public key in base64
        /// </summary>
        public string HelperSignaturePublicKeyB64 { get; set; } = string.Empty;

        /// <summary>
        /// Helper s private key in base64
        /// </summary>
        public string HelperSignaturePrivateKeyB64 { get; set; } = string.Empty;
        /// <summary>
        /// Helper e public key in base64
        /// </summary>
        public string HelperEncryptionPublicKeyB64 { get; set; } = string.Empty;

        /// <summary>
        /// Helper e private key in base64
        /// </summary>
        public string HelperEncryptionPrivateKeyB64 { get; set; } = string.Empty;
        /// <summary>
        /// Folder in which shares are stored
        /// </summary>
        public object RootDataFolder { get; internal set; } = "data";
    }
}
