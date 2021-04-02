using JMVG;
using Server.Commons;
using Server.JSP;
using System;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = new ApiController();
            Pipeline.Run();
            var result = Pipeline.Impl(new HttpRequest { Uri = "/ok", Method="GET" }).Result;
        }
    }
}
