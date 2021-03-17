using System;

namespace CgiAdder
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                (int a, int b) = (int.Parse(args[0]), int.Parse(args[1]));
                Console.WriteLine(a + b);
            }
            catch (Exception) 
            {
                Environment.Exit(-1);
            }
        }
    }
}
