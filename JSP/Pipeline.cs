using NamedPipeLib;
using Server.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Server.Commons.ControllerBase;

namespace Server.JSP
{
    public static class Pipeline
    {
        private static Dictionary<string, RequestProcessor[]> processorMap;

        static Pipeline() 
        {
            // AppDomain.CurrentDomain.Load("JMVG");
            var controllerImpls = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && typeof(ControllerBase)!=t && !t.IsInterface && !t.IsAbstract)
                .ToList();
            processorMap = new Dictionary<string, RequestProcessor[]>();
            var controllers = controllerImpls.Select(t => (ControllerBase) Activator.CreateInstance(t)).ToList();
            controllers.ForEach(c => Register(c));
        }

        private static void Register(ControllerBase controller) 
        {
            controller.Register(processorMap);
        }

        public static void Run() 
        {
            var completionResource = new TaskCompletionSource<int>();
            new Func<Task>(async () =>
            {
                var pipeClient = new NamedPipeClient("JspServer");
                pipeClient.OnDisconnect = (() => completionResource.SetResult(0));
                await pipeClient.ConnectAsync();
                Console.WriteLine("server connected");
                while (true)
                {
                    await pipeClient.Process((HttpRequest request) =>
                    {
                        Console.WriteLine($"JSP: Process {request.Method} {request.Uri}");
                        return Task.Run(()=> Impl(request));
                    });
                }
            }).Invoke();
            completionResource.Task.Wait();
        }

        public static async Task<IHttpResponse> Impl(HttpRequest request) 
        {
            var qMarkLoc = request.Uri.IndexOf('?');
            string path = qMarkLoc == -1 ? request.Uri : request.Uri.Substring(0, qMarkLoc);
            if (processorMap.TryGetValue(path, out RequestProcessor[] prossesors))
            {
                var method = Enum.Parse<HttpMethod>(request.Method);
                var processor = prossesors[(int)method];
                if (processor == null)
                {
                    return new HttpStringResponse { Body = $"{path} does not support {method} method", Status = 404 };
                }

                return await processor.ProcessFunc(request);
            }
            else 
            {
                return new HttpStringResponse { Body = $"route {path} not found", Status = 404 };
            }
        }

        public static HttpMethod Parse(this HttpMethod method, string s) 
        {
            var result = Enum.Parse<HttpMethod>(s);
            return result;
        }
    }
}
