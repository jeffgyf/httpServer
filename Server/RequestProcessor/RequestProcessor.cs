using System;
using System.Linq;
using System.Text;

namespace Server.RequestProcessorLib
{
    public delegate HttpResponse RequestProcessor(HttpRequest request);
    public static class RequestProcessors
    {
        [ThreadStatic]
        static Random randInternal = new Random();

        static Random rand 
        {
            get 
            {
                if (randInternal == null)
                {
                    randInternal = new Random();
                }
                return randInternal;
            }
        }

        public static HttpResponse Echo(HttpRequest request) 
        {
            return Generate(() => $"You are accessing {request.Uri}").Invoke(request);
        }

        public static HttpResponse Random(HttpRequest request)
        {
            return Generate(() => $"{rand.Next()}").Invoke(request);
        }

        public static RequestProcessor Generate(Func<string> func)
        {
            HttpResponse fun(HttpRequest request)
            {
                var body = Encoding.Default.GetBytes(func.Invoke());
                var responseHeader = Encoding.Default.GetBytes(
                    "HTTP/1.1 200 OK\r\n" +
                    "MIME-Version: 1.0\r\n" +
                    $"Date: {DateTime.Now}\r\n" +
                    "Server: Simple-Server/1.0\r\n" +
                    "Content-Type: text/html\r\n" +
                    $"Content-Length: {body.Length}\r\n" +
                    $"\r\n");
                return new HttpResponse { Header = responseHeader, Body = body };
            }

            return fun;
        }
    }
}
