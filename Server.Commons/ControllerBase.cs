using Server.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Commons
{
    public class ControllerBase
    {
        private List<RequestProcessor> processors;
        public ControllerBase() 
        {
            var methods = this.GetType().GetMethods();
            IEnumerable<System.Reflection.MethodInfo> processorMethods = methods.Where(m => m.GetCustomAttributes(false).Any(a => a is HttpMethodAttribute));
            processors = processorMethods.Select(p =>
            {
                
                var attrs = p.GetCustomAttributes(false);
                var httpMethodAttr = (HttpMethodAttribute)attrs.Where(a => a is HttpMethodAttribute).First();
                var routeAttr = (RouteAttribute)attrs.Where(a => a is RouteAttribute).First();
                return new RequestProcessor
                {
                    Method = httpMethodAttr.Method,
                    Path = routeAttr.Path,
                    ProcessFunc = (Func<HttpRequest, Task<IHttpResponse>>)Delegate.CreateDelegate(typeof(Func<HttpRequest, Task<IHttpResponse>>), this, p)
                };
            }).ToList();
        }

        public void Register(Dictionary<string, RequestProcessor[]> processorMap) 
        {
            processors.ForEach(p => 
            {
                if (!processorMap.ContainsKey(p.Path)) 
                {
                    processorMap[p.Path] = new RequestProcessor[2];
                }
                if (processorMap[p.Path][(int)p.Method] != null) 
                {
                    throw new Exception($"{p.Method} {p.Path} is already registered!");
                }
                processorMap[p.Path][(int)p.Method] = p;
            });
        }

        public class RequestProcessor
        {
            public HttpMethod Method;
            public string Path;
            public Func<HttpRequest, Task<IHttpResponse>> ProcessFunc;
        }
    }

    
}
