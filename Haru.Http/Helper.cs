using System;
using System.Linq;
using System.Net.Http;
using WebSocketSharp.Net;
using Zlib.Managed;
using Haru.Cryptography;

namespace Haru.ServerData.Http
{
    public class Helper
    {
        public const string EncryptHeader = "X-Encryption";
        private readonly EftAes _cipher;

        public Helper()
        {
            _cipher = new EftAes();
        }

        public ReadOnlySpan<byte> GetPayload(ReadOnlySpan<byte> data, bool compress, bool encrypt)
        {
            if (compress)
            {
                data = MemoryZlib.Compress(data, 9);
            }

            if (encrypt)
            {
                data = _cipher.Encrypt(data);
            }

            return data;
        }

        private static string[] GetEncryptions(object message)
        {
            if (message.GetType() == typeof(HttpListenerRequest))
            {
                // server
                var req = (HttpListenerRequest)message;

                try
                {
                    var values = req.Headers.GetValues(EncryptHeader);
                    return values.ToArray();
                }
                catch (InvalidOperationException)
                {
                    return Array.Empty<string>();
                }
            }
            
            if (message.GetType() == typeof(HttpResponseMessage))
            {
                // client
                var req = (HttpResponseMessage)message;
                
                try
                {
                    var values = req.Headers.GetValues(EncryptHeader);
                    return values.ToArray();
                }
                catch (InvalidOperationException)
                {
                    return Array.Empty<string>();
                }
            }

            throw new ArgumentException("Unsupported message object");
        }

        public ReadOnlySpan<byte> GetBody(object message, ReadOnlySpan<byte> data)
        {
            var encryptions = GetEncryptions(message);

            if (encryptions.Length == 1 && encryptions[0] == "aes")
            {
                data = _cipher.Decrypt(data);
            }

            if (MemoryZlib.IsCompressed(data))
            {
                data = MemoryZlib.Decompress(data);
            }

            return data;
        }
    }
}