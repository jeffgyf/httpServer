using NamedPipeLib;
using Server.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.RequestProcessorLib
{
    public class JspProcessor
    {
        private List<NamedPipeServer> workers;
        private bool disconnected = false;
        private int workerNum;
        private int idx = 0;

        public JspProcessor(int workerNum) 
        {
            this.workerNum = workerNum;
            var tasks = Enumerable.Range(0, workerNum).Select(async i => 
            {
                var pipeServer = new NamedPipeServer("JspServer", workerNum, i);
                pipeServer.OnDisconnect = (() =>
                {
                    disconnected = true;
                });
                Console.WriteLine($"worker{i} pipe initialized");
                await pipeServer.WaitForConnectionAsync();
                Console.WriteLine($"worker{i} connected");

                return pipeServer;
            }).ToArray();
            Task.WaitAll(tasks);
            workers = tasks.Select(t => t.Result).ToList();
        }

        public async Task<IHttpResponse> Process(HttpRequest request) 
        {
            idx = (idx + 1) % workerNum;
            var result = await workers[idx].CallAsync<IHttpResponse, HttpRequest>(request);
            return result;
        }
    }
}
