using System;
using System.IO;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Lora.Models;

namespace Lora
{
    class Program
    {
        private const int CommandParts = 7;
        private const int EventParts = 8;
        private const int TransactionParts = 5;
        private const int QuoteParts = 6;

        private static LogContext db = new LogContext();

        static void Main(string[] args)
        {  
            Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, eventArgs) => Quit());

            RabbitHelper.CreateConsumer(AddLogEntry);
            
            Console.WriteLine("Logger running...");
            Console.WriteLine("Press Ctrl-C to exit.");

            db.Database.EnsureCreated();

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

            Console.WriteLine("Closing Database...");
            db.Dispose();

            Console.WriteLine("Done.");
            Environment.Exit(0);
        }

        public static void AddLogEntry(string logEntry)
        {
            var lines = logEntry.Split(Environment.NewLine);
            var server = lines[0];
            
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var type = line[0];
                string[] parts;

                var log = new Log();

                try
                {
                    switch (type)
                    {
                        case 'c': // Command
                            parts = line.Split(',', CommandParts);
                            if (parts.Length != CommandParts)
                                Console.WriteLine($"Command entry does not have {CommandParts} parts");

                            log.LogType = "command";
                            log.Command = parts[1];
                            log.Username = parts[2];
                            log.Amount = decimal.Parse(parts[3]);
                            log.StockSymbol = parts[4];
                            log.Timestamp = ulong.Parse(parts[5]);
                            log.Filename = parts[6];

                            break;
                        case 'e': // Event
                            parts = line.Split(',', EventParts);
                            if (parts.Length != EventParts)
                                Console.WriteLine($"Event entry does not have {EventParts} parts");

                            log.LogType = parts[1].ToLower();
                            log.Username = parts[2];
                            log.Amount = decimal.Parse(parts[3]);
                            log.StockSymbol = parts[4];
                            log.Filename = parts[5];
                            log.Timestamp = ulong.Parse(parts[6]);
                            log.Message = parts[7];

                            break;
                        case 't': // Transaction
                            parts = line.Split(',', TransactionParts);
                            if (parts.Length != TransactionParts)
                                Console.WriteLine($"Event entry does not have {TransactionParts} parts");
                            
                            log.Username = parts[1];
                            log.Amount = decimal.Parse(parts[2]);
                            log.Timestamp = ulong.Parse(parts[3]);
                            log.Message = parts[4];

                            break;
                        case 'q': // Quote
                            parts = line.Split(',', QuoteParts);
                            if (parts.Length != QuoteParts)
                                Console.WriteLine($"Event entry does not have {QuoteParts} parts");

                            log.Amount = decimal.Parse(parts[1]);
                            log.StockSymbol = parts[2];
                            log.Username = parts[3];
                            log.Timestamp = ulong.Parse(parts[4]);
                            log.Cryptokey = parts[5];

                            break;
                        default:
                            Console.WriteLine($"Unknown log entry type {type}");
                            break;
                    }

                    db.Add(log);

                    // TODO: Change this behaviour. But this is fine for the workload files
                    if (log.Command == "DUMPLOG")
                    {
                        Console.WriteLine("DUMPLOG Encountered!");
                        LogXmlHelper.CreateLog(log.Filename, db);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Invalid entry: {line}. Exception: {e.Message}");
                    continue;
                }
            }
        }
    }
}
