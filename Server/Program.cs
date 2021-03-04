using Server.RequestProcessorLib;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    static class Program
    {
        static void Main(string[] args)
        {
            const int port = 8080;
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            socket.Bind(ep);
            socket.Listen(1);

            while (true)
            {
                AsyncServe(socket);
            }
        }

        static void SimpleServe(Socket socket)
        {
            var conn = socket.Accept();
            var response = Process(conn, RequestProcessors.Echo);
            conn.Send(response.Header);
            conn.Send(response.Body);
        }

        static async void AsyncServe(Socket socket) 
        {
            var conn = socket.Accept();
            try
            {
                var response = Process(conn, RequestProcessors.Echo);
                await conn.SendAsync(response.Header);
                await conn.SendAsync(response.Body);
            }
            finally 
            {
                conn.Close();
            }
        }

        static async Task SendAsync(this Socket socket, byte[] buf) 
        {
            var taskCompletion = new TaskCompletionSource<object>();
            socket.BeginSend(buf, 0, buf.Length, 0,
              new AsyncCallback(sendCallback), socket);

            void sendCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.  
                    Socket handler = (Socket)ar.AsyncState;
                    // Complete sending the data to the remote device.  
                    int bytesSent = handler.EndSend(ar);
                    Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                    taskCompletion.SetResult(null);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            await taskCompletion.Task;
        }


        static HttpResponse Process(Socket conn, RequestProcessor processor)
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

            return processor.Invoke(request);
        }

    }
}
