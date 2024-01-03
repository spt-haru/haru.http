using System;
using WebSocketSharp.Net;
using Haru.Pools;

namespace Haru.ServerData.Http
{
    public class Context
    {
        private readonly Helper _helper;
        public readonly HttpListenerRequest Request;
        public readonly HttpListenerResponse Response;

        public Context(HttpListenerRequest request, HttpListenerResponse response)
        {
            _helper = new Helper();
            Request = request;
            Response = response;
        }

        public string GetPath()
        {
            var url = Request.Url;
            var path = url.PathAndQuery;

            if (path.Contains("?"))
            {
                path = path.Split('?')[0];
            }

            return path;
        }

        public bool HasBody()
        {
            return Request.HasEntityBody;
        }

        public ReadOnlySpan<byte> GetBody()
        {
            var ms = MemoryStreamPool.Rent();

            try
            {
                Request.InputStream.CopyTo(ms);
                var data = ms.ToArray();
                return _helper.GetBody(Request, data);
            }
            finally
            {
                MemoryStreamPool.Return(ms);
            }
        }

        public string GetIp()
        {
            return Request.RemoteEndPoint.ToString();
        }

        public string GetAccountId()
        {
            // Cookie: PHPSESSID=<aid>
            var cookie = Request.Headers["Cookie"];
            return cookie.Split('=')[1];
        }

        public uint GetCrc()
        {
            var value = Request.Headers["If-None-Match"];

            if (value != null)
            {
                var text = value.Replace("\"", string.Empty);
                return Convert.ToUInt32(text);
            }
            else
            {
                // header doesn't exist
                return 0;
            }
        }
    }
}