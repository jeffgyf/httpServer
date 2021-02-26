using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            const int port = 8080;
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            socket.Bind(ep);
            socket.Listen(100);
            
            while (true)
            {
                var conn = socket.Accept();
                Process(conn);
            }
        }

        static void Process(Socket conn)
        {
            try
            {
                var buf = new byte[1024];
                conn.Receive(buf, 0, 1024, SocketFlags.None);
                string header = Encoding.Default.GetString(buf);
                string[] requestLine = header.Substring(0, header.IndexOf("\r\n")).Split(" ");
                if (requestLine.Length != 3) 
                {
                    throw new Exception("Unexpected request line: " + string.Join(" ", requestLine));
                }
                (string method, string uri, string version) = (requestLine[0], requestLine[1], requestLine[2]);
                var body = Encoding.Default.GetBytes($"You are accessing {uri}");
                string responseHeader = 
                    "HTTP/1.0 200 OK\r\n" +
                    "MIME-Version: 1.0\r\n" +
                    $"Date: {DateTime.Now}\r\n" +
                    "Server: Simple-Server/1.0\r\n" +
                    "Content-Type: text/html\r\n" +
                    $"Content-Length: {body.Length}\r\n\r\n";
                conn.Send(Encoding.Default.GetBytes(responseHeader));
                conn.Send(body);
            }
            finally 
            {
                conn.Close();
            }
        }
    }
}
