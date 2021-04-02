using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NamedPipeLib
{
    public class NamedPipeClient
    {
        private string pipename;
        private NamedPipeClientStream rxPipeClient, txPipeClient;
        private int msgCnt = 0;
        private Channel<(int id, byte[] data)> ch;
        private int bufSize = 10;
        private int clientId;
        private Task listeningTask;
        public Action OnDisconnect = ()=> { };
        public NamedPipeClient(string pipeName) 
        {
            this.pipename = pipeName;
            rxPipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.In);
            ch = Channel.CreateBounded<(int id, byte[] data)>(bufSize);
        }

        public async Task<int> ConnectAsync() 
        {
            await rxPipeClient.ConnectAsync();
            var intBuf = new byte[4];
            await rxPipeClient.ReadAsync(intBuf, 0, 4);
            clientId = BitConverter.ToInt32(intBuf);
            txPipeClient = new NamedPipeClientStream(".", pipename+"Rx", PipeDirection.Out);
            await txPipeClient.ConnectAsync();
            listeningTask = Task.Run(async () =>
            {
                //Console.WriteLine($"{clientId}: Connected to server");
                var headerBuf = new byte[8];
                while (true)
                {
                    int bytesRead = await this.rxPipeClient.ReadAsync(headerBuf, 0, 8);
                    int len = 0, id = 0;
                    if (bytesRead == 0 || (id = BitConverter.ToInt32(headerBuf, 0)) == -1)
                    {
                        OnDisconnect();
                        Close();
                        break;
                    }
                    len = BitConverter.ToInt32(headerBuf, 4);
                    var buf = new byte[len];
                    await this.rxPipeClient.ReadAsync(buf, 0, len);
                    ch.Writer.WriteAsync((id, buf));
                    // await pipeServer.ReadAsync(buf, 0, len);
                    //Console.WriteLine($"recieved {len} bytes");
                }
            });

            return clientId;
        }

        public async Task SendAsync<T>(int id, T item)
        {
            var buf = new MemoryStream();
            using (var writer = new BsonDataWriter(buf))
            {
                var serializer = JsonSerializer.Create(new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });
                buf.Write(BitConverter.GetBytes(id));
                buf.Write(BitConverter.GetBytes(0));
                serializer.Serialize(writer, item, typeof(T));
                int len = (int)buf.Length - 8;
                var bytes = buf.ToArray();
                BitConverter.GetBytes(len).CopyTo(bytes, 4);
                await txPipeClient.WriteAsync(bytes);
            }
        }

        public async Task<(int id, T data)> ReadAsync<T>() 
        {
            (int id, byte[] bytes) = await ch.Reader.ReadAsync();
            var serializer = new JsonSerializer();
            using (var bsonReader = new BsonDataReader(new MemoryStream(bytes, false)))
            {
                var result = serializer.Deserialize<T>(bsonReader);
                return (id, result);
            }
        }

        public async Task Process<T, U>(Func<T, Task<U>> processor) 
        {
            (int id, T data) = await ReadAsync<T>();
            processor
                .Invoke(data)
                .ContinueWith(t =>
                {
                    SendAsync(id, t.Result);
                });
        }

        public void Close() 
        {
            rxPipeClient.Close();
            txPipeClient.Close();
        }
    }
}
