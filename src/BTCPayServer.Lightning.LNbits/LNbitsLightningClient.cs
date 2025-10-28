using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Lightning;
using NBitcoin;

namespace BTCPayServer.Lightning.LNbits
{
    public class LNbitsLightningClient : ILightningClient
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _baseUri;
        private readonly string _walletId;
        private readonly string _apiKey;

        public LNbitsLightningClient(Uri baseUri, string walletId, string apiKey, HttpClient httpClient = null)
        {
            _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
            _walletId = walletId ?? throw new ArgumentNullException(nameof(walletId));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

            _httpClient = httpClient ?? new HttpClient();
            _httpClient.BaseAddress = baseUri;
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        public async Task<LightningInvoice> CreateInvoice(
            LightMoney amount,
            string description,
            TimeSpan expiry,
            CancellationToken cancellation = default)
        {
            var amountSats = (long)amount.ToUnit(LightMoneyUnit.Satoshi);

            var request = new
            {
                @out = false,
                amount = amountSats,
                memo = description ?? "BTCPay Server Invoice",
                unit = "sat",
                expiry = (int)expiry.TotalSeconds
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/payments", request, cancellation);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LNbitsInvoiceResponse>(cancellationToken: cancellation);

            return new LightningInvoice
            {
                Id = result.payment_hash,
                BOLT11 = result.payment_request,
                Status = LightningInvoiceStatus.Unpaid,
                Amount = amount,
                ExpiresAt = DateTimeOffset.UtcNow.Add(expiry)
            };
        }

        public async Task<LightningInvoice> GetInvoice(string invoiceId, CancellationToken cancellation = default)
        {
            var response = await _httpClient.GetAsync($"/api/v1/payments/{invoiceId}", cancellation);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LNbitsPaymentResponse>(cancellationToken: cancellation);

            return new LightningInvoice
            {
                Id = result.payment_hash,
                BOLT11 = result.bolt11,
                Status = result.paid ? LightningInvoiceStatus.Paid : LightningInvoiceStatus.Unpaid,
                Amount = LightMoney.MilliSatoshis(result.amount),
                PaidAt = result.paid ? DateTimeOffset.FromUnixTimeSeconds(result.time) : null
            };
        }

        public async Task<LightningInvoice> GetInvoice(uint256 paymentHash, CancellationToken cancellation = default)
        {
            return await GetInvoice(paymentHash.ToString(), cancellation);
        }

        public Task<LightningInvoice> CreateInvoice(CreateInvoiceParams createInvoiceRequest, CancellationToken cancellation = default)
        {
            return CreateInvoice(createInvoiceRequest.Amount, createInvoiceRequest.Description, createInvoiceRequest.Expiry, cancellation);
        }

        public Task<LightningInvoice[]> ListInvoices(CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<LightningInvoice[]> ListInvoices(ListInvoicesParams request, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<LightningPayment> GetPayment(string paymentHash, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<LightningPayment[]> ListPayments(CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<LightningPayment[]> ListPayments(ListPaymentsParams request, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<ILightningInvoiceListener> Listen(CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public async Task<LightningNodeInformation> GetInfo(CancellationToken cancellation = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/v1/wallet", cancellation);
                response.EnsureSuccessStatusCode();

                var walletInfo = await response.Content.ReadFromJsonAsync<LNbitsWalletResponse>(cancellationToken: cancellation);

                return new LightningNodeInformation
                {
                    Alias = walletInfo?.name ?? "LNbits Wallet",
                    BlockHeight = 0,
                    NodeInfoList = Array.Empty<NodeInfo>()
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get wallet info: {ex.Message}", ex);
            }
        }

        public async Task<LightningNodeBalance> GetBalance(CancellationToken cancellation = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/v1/wallet", cancellation);
                response.EnsureSuccessStatusCode();
                
                var walletInfo = await response.Content.ReadFromJsonAsync<LNbitsWalletResponse>(cancellationToken: cancellation);
                
                return new LightningNodeBalance
                {
                    OnchainBalance = null,
                    OffchainBalance = new OffchainBalance
                    {
                        Opening = LightMoney.Zero,
                        Local = LightMoney.MilliSatoshis(walletInfo?.balance ?? 0),
                        Remote = LightMoney.Zero,
                        Closing = LightMoney.Zero
                    }
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get balance: {ex.Message}", ex);
            }
        }

        public Task<PayResponse> Pay(PayInvoiceParams payParams, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<PayResponse> Pay(string bolt11, PayInvoiceParams payParams, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<PayResponse> Pay(string bolt11, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<OpenChannelResponse> OpenChannel(OpenChannelRequest openChannelRequest, CancellationToken cancellation = default)
        {
            throw new NotSupportedException("LNbits does not support channel management");
        }

        public Task<BitcoinAddress> GetDepositAddress(CancellationToken cancellation = default)
        {
            throw new NotSupportedException("LNbits does not support on-chain operations");
        }

        public Task<ConnectionResult> ConnectTo(NodeInfo nodeInfo, CancellationToken cancellation = default)
        {
            throw new NotSupportedException("LNbits does not support peer connections");
        }

        public Task CancelInvoice(string invoiceId, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<LightningChannel[]> ListChannels(CancellationToken cancellation = default)
        {
            return Task.FromResult(Array.Empty<LightningChannel>());
        }

        private class LNbitsInvoiceResponse
        {
            public string payment_hash { get; set; }
            public string payment_request { get; set; }
        }

        private class LNbitsPaymentResponse
        {
            public string payment_hash { get; set; }
            public bool paid { get; set; }
            public long amount { get; set; }
            public string bolt11 { get; set; }
            public long time { get; set; }
        }

        private class LNbitsWalletResponse
        {
            public string id { get; set; }
            public string name { get; set; }
            public long balance { get; set; }
        }
    }
}
