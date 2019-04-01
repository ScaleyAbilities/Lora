using System;
using System.IO;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Lora
{
    class Program
    {
        static void Main(string[] args)
        {  
            Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, eventArgs) => Quit());

            RabbitHelper.CreateConsumer(LogXmlHelper.AddLogEntry);
            
            Console.WriteLine("Logger running...");
            Console.WriteLine("Press Ctrl-C to exit.");

            while(true)
            {
                Console.ReadLine();
            }
        }

        public static void Quit()
        {
            Console.WriteLine("Quitting...");

            Console.WriteLine("Ending Rabbit connection...");
            RabbitHelper.CloseRabbit();

            Console.WriteLine("Finishing log file...");
            LogXmlHelper.CloseLog();

            Console.WriteLine("Done.");
            Environment.Exit(0);
        }
    }
}
