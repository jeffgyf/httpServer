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
            var body = $"You are accessing {request.Uri}";
            var response = new HttpResponse { Body = body, Status = 200 };
            return response;
        }

        public static HttpResponse Random(HttpRequest request)
        {
            var response = new HttpResponse { Body = $"{rand.Next()}", Status = 200 };
            return response;
        }

        public static HttpResponse CGI(HttpRequest request) 
        {
            var qMarkLoc = request.Uri.IndexOf('?');
            var args = (qMarkLoc == -1 ? "" : request.Uri.Split('?')[1]).Replace('&', ' ');
            qMarkLoc = (qMarkLoc == -1 ? request.Uri.Length : qMarkLoc);
            var cmd = request.Uri.Substring(1, qMarkLoc - 1);

            Logger.Log($"CGI: {cmd} {args}");
            HttpResponse resp = null;
            try
            {
                var cliHelper = new CLIHelper($"K:/workshop/httpServer/Server/CGI/{cmd}.exe");
                var result = cliHelper.Run(args);
                if (result.output == null) 
                {
                    throw new Exception(result.error);
                }
                resp = new HttpResponse { Body = result.output, Status = 200 };
            }
            catch (Exception e) 
            {
                resp = new HttpResponse { Body = e.Message, Status = 500 };
            }

            return resp;
        }

        public static byte[] GenerateHeader(int statusCode, int bodyLength, string header) 
        {
            var responseHeader =
                   $"HTTP/1.1 {statusCode} {GetCodeName(statusCode)}\r\n" +
                   header +
                   "MIME-Version: 1.0\r\n" +
                   $"Date: {DateTime.Now}\r\n" +
                   "Server: Simple-Server/1.0\r\n" +
                   "Content-Type: text/html; charset=utf-8\r\n" +
                   $"Content-Length: {bodyLength}\r\n" +
                   $"\r\n";

            return Encoding.UTF8.GetBytes(responseHeader);
        }

        public static string GetCodeName(int statusCode) 
        {
            switch (statusCode) 
            {
                case 200:
                    return "OK";
                case 404:
                    return "Not Found";
                case 500:
                    return "Internal Server Error";
            }
            return "Unknown";
        }

        public static (byte[] Header, byte[] Body) GenerateResponse(this RequestProcessor processor, HttpRequest request) 
        {
            var resp = processor.Invoke(request);
            var body = Encoding.UTF8.GetBytes(resp.Body);
            var header = GenerateHeader(resp.Status, body.Length, resp.Header);
            return (header, body);
        }
    }
}
