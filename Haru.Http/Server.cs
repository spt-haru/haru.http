using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using WebSocketSharp.Server;

namespace Haru.ServerData.Http
{
    public class Server
    {
        private readonly HttpServer _httpv;
        private readonly Dictionary<string, Behaviour> _services;
        public readonly string Address;
        public readonly string Name;

        public Server(string name, string address, string cert = null, string password = null)
        {
            _httpv = CreateInstance(address, cert, password);
            _services = new Dictionary<string, Behaviour>();
            Name = name;
            Address = address;
        }

        private HttpServer CreateInstance(string address, string cert = null, string password = null)
        {
            HttpServer server = null;
            var uri = new Uri(address);
            var protocol = uri.Scheme;

            switch (protocol)
            {
                case "http":
                    server = new HttpServer(uri.Port);
                    break;

                case "https":
                    server = new HttpServer(uri.Port, true);
                    server.SslConfiguration.ClientCertificateValidationCallback += IsCertificateValid;
                    server.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
                    server.SslConfiguration.ServerCertificate = new X509Certificate2(cert, password);

                    // todo: proper certificate validation
                    server.SslConfiguration.ClientCertificateRequired = false;
                    break;

                default:
                    throw new ArgumentException($"protocol {protocol} not supported.");
            }

            server.OnGet += OnRequest;
            server.OnPost += OnRequest;
            return server;
        }

        private bool IsCertificateValid(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // todo: proper certificate validation
            return true;
        }

        private void OnRequest(object sender, HttpRequestEventArgs e)
        {
            var context = new Context(e.Request, e.Response);
            var path = context.GetPath();

            if (_services.ContainsKey(path))
            {
                var service = _services[path];
                Console.WriteLine($"[{Name}] {path}");
                service.Run(context);
            }
            else
            {
                Console.WriteLine($"[{Name}] No service found for {path}");
                e.Response.Close();
            }
        }

        public void Start()
        {
            _httpv.Start();
            Console.WriteLine($"[{Name}] Started on {Address}");
        }

        public void Stop()
        {
            _httpv.Stop();
            Console.WriteLine($"[{Name}] Stopped.");
        }

        public void AddHttpService<T>(string path) where T : Behaviour, new()
        {
            _services.Add(path, new T());
        }

        public void AddWsService<T>(string path) where T : WebSocketBehavior, new()
        {
            _httpv.AddWebSocketService<T>(path);
        }
    }
}