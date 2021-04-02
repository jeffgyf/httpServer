using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipeLib
{
    public class NamedPipeServer
    {
        private int maxNumberOfServerInstances;
        private string pipeName;

        // 不能同时读写
        private NamedPipeServerStream txPipeServer, rxPipeServer;
        private int msgCnt = 0;
        private Dictionary<int, TaskCompletionSource<byte[]>> dict;
        private int serverId;
        public Action OnDisconnect = () => { };
        private Task listeningTask;
        public NamedPipeServer(string pipeName, int maxNumberOfServerInstances, int serverId) 
        {
            this.maxNumberOfServerInstances = maxNumberOfServerInstances;
            this.pipeName = pipeName;
            txPipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out, maxNumberOfServerInstances);
            dict = new Dictionary<int, TaskCompletionSource<byte[]>>();
            this.serverId = serverId;
        }

        public async Task WaitForConnectionAsync() 
        {
            await txPipeServer.WaitForConnectionAsync();
            await txPipeServer.WriteAsync(BitConverter.GetBytes(serverId));
            rxPipeServer = new NamedPipeServerStream(pipeName + "Rx", PipeDirection.In, maxNumberOfServerInstances);
            await rxPipeServer.WaitForConnectionAsync();
            listeningTask = Task.Run(async () =>
            {
                //Console.WriteLine($"{serverId}: Client connected");
                var headerBuf = new byte[8];
                while (true)
                {
                    int bytesRead = await rxPipeServer.ReadAsync(headerBuf, 0, 8);
                    int len = 0, id = 0;
                    if (bytesRead == 0 || (id = BitConverter.ToInt32(headerBuf, 0)) == -1)
                    {
                        OnDisconnect();
                        Close();
                        break;
                    }
                    len = BitConverter.ToInt32(headerBuf, 4);
                    var buf = new byte[len];
                    await rxPipeServer.ReadAsync(buf, 0, len);
                    if (dict.TryGetValue(id, out var t))
                    {
                        t.SetResult(buf);
                    }
                    //await pipeServer.ReadAsync(buf, 0, len);
                    //Console.WriteLine($"recieved {len} bytes");
                }
            });
        }

        public async Task<T> CallAsync<T, U>(U item) 
        {
            int id = await SendAsync(item);
            var result = await ReadAsync<T>(id);
            return result;
        }

        public async Task<int> SendAsync<T>(T item) 
        {
            int id = Interlocked.Increment(ref msgCnt);
            if (!dict.TryAdd(id, new TaskCompletionSource<byte[]>())) 
            {
                throw new Exception($"id {id} existed!");
            }
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
                await txPipeServer.WriteAsync(bytes);
            }
            
            return id;
        }

        public async Task<T> ReadAsync<T>(int eventId) 
        {
            if (dict.TryGetValue(eventId, out var t))
            {
                var bytes = await t.Task;
                dict.Remove(eventId);
                var serializer = JsonSerializer.Create(new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });
                using (var bsonReader = new BsonDataReader(new MemoryStream(bytes, false)))
                {
                    var result = serializer.Deserialize<T>(bsonReader);

                    return result;
                }
            } 
            else 
            {
                throw new Exception($"No such eventId:{eventId}");
            }
        }

        public void Close() 
        {
            txPipeServer.Close();
            rxPipeServer.Close();
        }
    }
}
