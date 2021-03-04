using Server.RequestProcessorLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class MySocket : Socket
    {
        public MySocket(SocketInformation socketInformation) : base(socketInformation)
        {
        }

        public MySocket(SocketType socketType, ProtocolType protocolType) : base(socketType, protocolType)
        {
        }

        public MySocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) : base(addressFamily, socketType, protocolType)
        {
        }

        public int Id;
    }
    static class Program
    {
        static void Main(string[] args)
        {
            const int port = 8080;
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            socket.Bind(ep);
            socket.Listen(10000);

            //SimpleServe(socket);
            AsyncServe(socket).Wait();
        }

        static void SimpleServe(Socket socket)
        {
            while (true)
            {
                var conn = socket.Accept();

                try
                {
                    var buf = new byte[1024];
                    conn.Receive(buf, 0, 1024, SocketFlags.None);
                    var response = Process(buf, 0, RequestProcessors.Echo);
                    conn.Send(response.Header);
                    conn.Send(response.Body);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        static int connCnt = 0;
        static async Task AsyncServe(Socket socket) 
        {
            // var taskCompletion = new TaskCompletionSource<object>();
            while (true)
            {
                var conn = socket.Accept();
                var clientIp = (IPEndPoint)conn.RemoteEndPoint;
                int connId = connCnt++;
                Logger.Log($"----------connection {connId} established: {clientIp.Address}");
                var func = new Func<Socket, Task>(async conn =>
                {
                    try
                    {
                        var st = DateTime.Now;
                        var et = st + TimeSpan.FromSeconds(300);
                        while (DateTime.Now < et)
                        {
                            var result = await conn.ReceiveAsync();
                            if (result.Count == 0)
                            {
                                break;
                            }
                            //var result = new byte[1024];
                            //conn.Receive(result, 0, 1024, SocketFlags.None);
                            var response = Process(result, connId, RequestProcessors.Echo);
                            await conn.SendAsync(response.Header.Concat(response.Body).ToArray());
                            /*socket.BeginAccept(async ar => {
                                taskCompletion.SetResult(null);
                                var conn = socket.EndAccept(ar);
                                try
                                {
                                    //Console.WriteLine("start processing incoming request---------------------------------");
                                    var response = Process(conn, RequestProcessors.Echo);
                                    await conn.SendAsync(response.Header);
                                    await conn.SendAsync(response.Body);
                                }
                                finally
                                {
                                    conn.Close();
                                    //Console.WriteLine("done--------------------------------------------------------------\r\n\r\n");
                                }
                            }, null);//*/
                        }
                    }
                    finally
                    {
                        Logger.Log($"----------connection {connId} closed!");
                        conn.Close();
                    }
                }).Invoke(conn);

            }
            //await taskCompletion.Task;
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
                      //Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                      taskCompletion.SetResult(null);
                  }
                  catch (Exception e)
                  {
                      Console.WriteLine(e.ToString());
                  }
              }, null);

            await taskCompletion.Task;
        }

        static async Task<List<byte>> ReceiveAsync(this Socket socket)
        {
            var taskCompletion = new TaskCompletionSource<object>();
            var buf = new byte[1024];
            
            var result = new List<byte>();
            AsyncCallback func = null;
            func = (ar =>
            {
                try
                {
                    int bytesRead = socket.EndReceive(ar);
                    /*if (bytesRead > 0)
                    {
                        // There might be more data, so store the data received so far.  
                        result.AddRange(buf.Take(bytesRead));
                        // Get the rest of the data.  
                        socket.BeginReceive(buf, 0, buf.Length, 0,
                            func, null);
                    }
                    else
                    {
                        // All the data has arrived; put it in response.  
                        // Signal that all bytes have been received.  
                        taskCompletion.SetResult(null);
                    }//*/

                    result.AddRange(buf.Take(bytesRead));
                    taskCompletion.SetResult(null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            });

            socket.BeginReceive(buf, 0, buf.Length, 0, func, null);

            await taskCompletion.Task;
            return result;
        }



        static HttpResponse Process(IEnumerable<byte> buf, int connId, RequestProcessor processor)
        {
            string header = Encoding.Default.GetString(buf.ToArray());
            string requestLine = header.Substring(0, header.IndexOf("\r\n"));
            Console.WriteLine($"connection {connId}:" + requestLine);
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
