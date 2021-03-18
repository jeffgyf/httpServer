using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.RequestProcessorLib
{
    public delegate Task<IHttpResponse> RequestProcessor(HttpRequest request);
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

        public async static Task<IHttpResponse> Echo(HttpRequest request) 
        {
            var body = $"You are accessing {request.Uri}";
            var response = new HttpStringResponse { Body = body, Status = 200 };
            return response;
        }

        public async static Task<IHttpResponse> Random(HttpRequest request)
        {
            var response = new HttpStringResponse { Body = $"{rand.Next()}", Status = 200 };
            return response;
        }

        public async static Task<IHttpResponse> CGI(HttpRequest request) 
        {
            var qMarkLoc = request.Uri.IndexOf('?');
            var args = (qMarkLoc == -1 ? "" : request.Uri.Split('?')[1]).Replace('&', ' ');
            qMarkLoc = (qMarkLoc == -1 ? request.Uri.Length : qMarkLoc);
            var cmd = request.Uri.Substring(1, qMarkLoc - 1);

            Logger.Log($"CGI: {cmd} {args}");
            IHttpResponse resp = null;
            try
            {
                var cliHelper = new CLIHelper($"K:/workshop/httpServer/Server/CGI/{cmd}.exe");
                var result = cliHelper.Run(args);
                if (result.output == null) 
                {
                    throw new Exception(result.error);
                }
                resp = new HttpStringResponse { Body = result.output, Status = 200 };
            }
            catch (Exception e) 
            {
                resp = new HttpStringResponse { Body = JsonConvert.SerializeObject(e), Status = 500 };
            }

            return resp;
        }

        public async static Task<IHttpResponse> Static(HttpRequest request) 
        {
            string uri = request.Uri;
            IHttpResponse resp = null;
            try
            {
                string path = "K:/workshop/httpServer/Server/static" + uri;
                var fileAttr = File.GetAttributes(path);
                if ((fileAttr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    var dir = new DirectoryInfo(path);
                    resp = new HttpStringResponse { Body = string.Join("\r\n", dir.GetFiles().Select(f=>f.Name)
                        .Concat(dir.GetDirectories().Select(d=>d.Name))), Status = 20, ContentType = "text/plain" };
                }
                else
                {
                    var bytes = await File.ReadAllBytesAsync(path);
                    var bResp = new HttpBytesResponse { Body = bytes, Status = 200 };
                    if (new[] { ".html", ".html", ".txt", ".md" }.Any(ext => uri.EndsWith(ext)))
                    {
                        bResp.ContentType = "text/html";
                    }
                    else if (new[] { ".png", ".jpg", ".bmp" }.Any(ext => uri.EndsWith(ext)))
                    {
                        bResp.ContentType = "image";
                    }
                    else
                    {
                        bResp.ContentType = "application/octet-stream";
                    }
                    resp = bResp;
                }
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                resp = new HttpStringResponse { Body = JsonConvert.SerializeObject(e, Formatting.Indented), Status = 404, ContentType="application/json" };
            }
            catch (Exception e)
            {
                resp = new HttpStringResponse { Body = JsonConvert.SerializeObject(e, Formatting.Indented), Status = 500, ContentType = "application/json" };
            }
            return resp;
        }

        public static byte[] GenerateHeader(int statusCode, int bodyLength, string header, string contentType) 
        {
            var responseHeader =
                   $"HTTP/1.1 {statusCode} {GetCodeName(statusCode)}\r\n" +
                   header +
                   "MIME-Version: 1.0\r\n" +
                   $"Date: {DateTime.Now}\r\n" +
                   "Server: Simple-Server/1.0\r\n" +
                   $"Content-Type: {contentType}; charset=utf-8\r\n" +
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


        public async static Task<(byte[] Header, byte[] Body)> GenerateResponse(this RequestProcessor processor, HttpRequest request)
        {
            var resp = await processor.Invoke(request);
            var body = resp.Body;
            var header = GenerateHeader(resp.Status, body.Length, resp.Header, resp.ContentType);
            return (header, body);
        }
    }
}
