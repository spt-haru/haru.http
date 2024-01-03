using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Haru.Pools;
using Haru.Converters;

namespace Haru.ServerData.Http
{
    public sealed class Client : IDisposable
    {
        private readonly Helper _helper;
        private readonly HttpClient _httpv;
        private readonly int _requestId;
        private readonly string _accountId;
        private readonly string _gameVersion;
        private readonly string _address;

        public Client(string address, string accountId, string gameVersion)
        {
            _helper = new Helper();

            _requestId = 0;
            _accountId = accountId;
            _gameVersion = gameVersion;
            _address = address;

            // setup certificate handler
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback += IsCertificateValid;

            // setup http client
            _httpv = new HttpClient(handler);
        }

        private bool IsCertificateValid(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // todo: proper certificate validation
            return true;
        }

        private HttpRequestMessage GetNewRequest(HttpMethod method, string path)
        {
            var cookie = $"PHPSESSID={_accountId}";
            return new HttpRequestMessage()
            {
                Method = method,
                RequestUri = new Uri(_address + path),
                Headers = {
                    { "App-Version",        _gameVersion            },
                    { "GClient-RequestId",  _requestId.ToString()   },
                    { "Cookie",             cookie                  },
                }
            };
        }

        private ReadOnlySpan<byte> Send(HttpMethod method, string path, ReadOnlySpan<byte> data, uint crc = 0)
        {
            HttpResponseMessage response = null;

            using (var request = GetNewRequest(method, path))
            {
                if (data != null)
                {
                    var payload = _helper.GetPayload(data, true, true);
                    request.Content = new ByteArrayContent(payload.ToArray());
                    request.Headers.Add(Helper.EncryptHeader, "aes");
                }

                if (crc != 0)
                {
                    request.Headers.Add("If-None-Match", crc.ToString());
                }

                response = _httpv.SendAsync(request).Result;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Code {response.StatusCode}");
            }

            var ms = MemoryStreamPool.Rent();

            try
            {
                using (var stream = response.Content.ReadAsStreamAsync().Result)
                {
                    stream.CopyTo(ms);

                    var bytes = ms.ToArray();
                    if (bytes != null)
                    {
                        return _helper.GetBody(response, bytes);
                    }
                }
            }
            finally
            {
                MemoryStreamPool.Return(ms);
            }

            return null;
        }

        public ReadOnlySpan<byte> GetBytes(string path)
        {
            return Send(HttpMethod.Get, path, null);
        }

        public string GetText(string path)
        {
            var bytes = GetBytes(path);
            return Utf8.ToString(bytes);
        }

        public ReadOnlySpan<byte> PostBytes(string path, ReadOnlySpan<byte> data, uint crc = 0)
        {
            return Send(HttpMethod.Post, path, data, crc);
        }

        public string PostText(string path, ReadOnlySpan<byte> data, uint crc = 0)
        {
            var bytes = PostBytes(path, data, crc);
            return Utf8.ToString(bytes);
        }

        public void Dispose()
        {
            _httpv.Dispose();
        }
    }
}