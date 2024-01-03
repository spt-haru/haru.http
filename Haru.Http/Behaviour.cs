using System;
using Haru.Pools;
using Haru.Converters;

namespace Haru.ServerData.Http
{
    public abstract class Behaviour
    {
        private readonly Helper _helper;

        public Behaviour()
        {
            _helper = new Helper();
        }

        public abstract void Run(Context context);

        protected void SendBytes(Context context, ReadOnlySpan<byte> data, bool compress = true, bool encrypt = true)
        {
            var response = context.Response;
            var bytes = _helper.GetPayload(data, compress, encrypt);

            if (encrypt)
            {
                response.Headers.Add(Helper.EncryptHeader, "aes");
            }

            response.ContentType = "application/octet-stream";
            response.ContentLength64 = bytes.Length;

            response.OutputStream.Write(bytes);
            response.Close();
        }

        protected void SendText(Context context, string text, bool compress = true, bool encrypt = true)
        {
            var data = Utf8.ToBytes(text);
            SendBytes(context, data, compress, encrypt);
        }

        protected static void SendCached(Context context)
        {
            using (var response = context.Response)
            {
                response.StatusCode = 304;
                response.Close();
            }
        }
    }
}