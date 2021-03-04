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
               AsyncServe(socket).Wait();
            }
        }

        static void SimpleServe(Socket socket)
        {
            var conn = socket.Accept();
            var response = Process(conn, RequestProcessors.Echo);
            conn.Send(response.Header);
            conn.Send(response.Body);
        }

        static async Task AsyncServe(Socket socket) 
        {
            var taskCompletion = new TaskCompletionSource<object>();
            // var conn = socket.Accept();
            socket.BeginAccept(async ar => {
                taskCompletion.SetResult(null);
                var conn = socket.EndAccept(ar);
                try
                {
                    Console.WriteLine("start processing incoming request---------------------------------");
                    var response = Process(conn, RequestProcessors.Echo);
                    await conn.SendAsync(response.Header);
                    await conn.SendAsync(response.Body);
                }
                finally
                {
                    conn.Close();
                    Console.WriteLine("done--------------------------------------------------------------\r\n\r\n");
                }
            }, null);
            await taskCompletion.Task;
        }

        static async Task SendAsync(this Socket socket, byte[] buf) 
        {
            var taskCompletion = new TaskCompletionSource<object>();
            socket.BeginSend(buf, 0, buf.Length, 0,
              ar => 
              {
                  try
                  {
                      int bytesSent = socket.EndSend(ar);
                      Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                      taskCompletion.SetResult(null);
                  }
                  catch (Exception e)
                  {
                      Console.WriteLine(e.ToString());
                  }
              }, null);

            await taskCompletion.Task;
        }


        static HttpResponse Process(Socket conn, RequestProcessor processor)
        {
            var buf = new byte[1024];
            conn.Receive(buf, 0, 1024, SocketFlags.None);
            string header = Encoding.Default.GetString(buf);
            string requestLine = header.Substring(0, header.IndexOf("\r\n"));
            Console.WriteLine(requestLine);
            string[] splited = requestLine.Split(" ");
            if (splited.Length != 3)
            {
                throw new Exception("Unexpected request line: " + string.Join(" ", requestLine));
            }
            var request = new HttpRequest { Method = splited[0], Uri = splited[1], Version = splited[2] };
            
            return processor.Invoke(request);
        }

    }
}
