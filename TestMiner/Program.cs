using System;
using System.Threading;

namespace TestMiner
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = 1;
            Console.WriteLine("I'm pretending to mine.");
            if (args.Length > 0)
            {
                Console.WriteLine(string.Format("Arguments: {0}", string.Join(" ", args)));
            }

            while (!Console.KeyAvailable)
            {
                Thread.Sleep(1000);
                Console.WriteLine(string.Format("Found block {0}!", x));
                x++;
            }
            Console.ReadKey(true);
        }
    }
}
