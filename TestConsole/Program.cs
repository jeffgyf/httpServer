﻿using Server.Commons;
using Server.JSP;
using System;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = new TestController();
            var result = Pipeline.Impl(new HttpRequest { Uri = "/ok", Method="GET" }).Result;
        }
    }
}
