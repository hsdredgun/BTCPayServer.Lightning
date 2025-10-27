using System;
using System.Linq;
using System.Net.Http;
using NBitcoin;

namespace BTCPayServer.Lightning.LNbits
{
    public class LNbitsConnectionStringHandler : ILightningConnectionStringHandler
    {
        private readonly HttpClient _httpClient;

        public LNbitsConnectionStringHandler(HttpClient httpClient = null)
        {
            _httpClient = httpClient;
        }

        public ILightningClient Create(string connectionString, Network network, out string error)
        {
            var kv = LightningConnectionStringHelper.ExtractValues(connectionString, out var type);
            
            if (type != "lnbits")
            {
                error = null;
                return null;
            }

            if (!kv.TryGetValue("server", out var server))
            {
                error = "The key 'server' is mandatory for lnbits connection strings";
                return null;
            }

            if (!Uri.TryCreate(server, UriKind.Absolute, out var uri))
            {
                error = "The key 'server' should be a valid URI";
                return null;
            }

            if (!kv.TryGetValue("wallet-id", out var walletId))
            {
                error = "The key 'wallet-id' is mandatory for lnbits connection strings";
                return null;
            }

            if (!kv.TryGetValue("api-key", out var apiKey))
            {
                error = "The key 'api-key' is mandatory for lnbits connection strings";
                return null;
            }

            bool allowInsecure = false;
            if (kv.TryGetValue("allowinsecure", out var allowinsecureStr))
            {
                allowInsecure = allowinsecureStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            if (!LightningConnectionStringHelper.VerifySecureEndpoint(uri, allowInsecure))
            {
                error = "The server must use HTTPS or set allowinsecure=true";
                return null;
            }

            error = null;
            return new LNbitsLightningClient(uri, walletId, apiKey, _httpClient);
        }
    }
}
