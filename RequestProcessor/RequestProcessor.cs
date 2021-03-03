using System;
using System.Linq;
using System.Text;

namespace Server.RequestProcessor
{
    public static class RequestProcessors
    {
        public static (byte[] header, byte[] body) Echo(HttpRequest request) 
        {
            var body = Encoding.Default.GetBytes($"You are accessing {request.Uri}");
            var responseHeader = Encoding.Default.GetBytes(
                "HTTP/1.0 200 OK\r\n" +
                "MIME-Version: 1.0\r\n" +
                $"Date: {DateTime.Now}\r\n" +
                "Server: Simple-Server/1.0\r\n" +
                "Content-Type: text/html\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                $"\r\n");
            return (responseHeader, body);
        }
    }
}
