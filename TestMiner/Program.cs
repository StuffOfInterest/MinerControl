using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestMiner
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = 1;
            Console.WriteLine("I'm pretending to mine.");
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
