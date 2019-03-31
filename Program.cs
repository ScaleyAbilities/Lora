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
        static void Log(String logEntry)
        {
            LogXmlHelper.AddLogEntry(logEntry);
        }

        static void Main(string[] args)
        {         
            RabbitHelper.CreateConsumer(Log, RabbitHelper.rabbitLogQueue);
            
            Console.WriteLine("Logger running...");
            Console.WriteLine("Press Ctrl-C to exit.");

            Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, eventArgs) => {
                Console.WriteLine("Quitting...");

                Console.WriteLine("Ending Rabbit connection...");
                RabbitHelper.CloseRabbit();

                Console.WriteLine("Finishing log file...");
                LogXmlHelper.CloseLog();

                Console.WriteLine("Done.");
                Environment.Exit(0);
            });

            while(true)
            {
                Console.ReadLine();
            }    
        }
    }
}
