using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server
{
    public static class Logger
    {
        public static void Log(string msg) 
        {
            if (Debugger.IsAttached) 
            {
                Console.WriteLine(msg);
            }
        }
    }
}
