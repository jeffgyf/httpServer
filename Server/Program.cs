using Server.RequestProcessor;
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
                var request = new HttpRequest { Method = requestLine[0], Uri = requestLine[1], Version = requestLine[2] };

                var response = RequestProcessors.Echo(request);
                conn.Send(response.header);
                conn.Send(response.body);
            }
            finally 
            {
                conn.Close();
            }
        }
    }
}
