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

            db.Database.EnsureCreated();

            RabbitHelper.CreateConsumer(AddLogEntry);
            
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

            Console.WriteLine("Closing Database...");
            db.Dispose();

            Console.WriteLine("Done.");
            Environment.Exit(0);
        }

        public static void AddLogEntry(string logEntry)
        {
            var lines = logEntry.Split(Environment.NewLine);
            var logParams = lines[0].Split(',', 2);

            if (logParams.Length < 2)
                throw new ArgumentException("Log params does not contain two entries");

            var server = logParams[0];
            var transaction = logParams[1];
            
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var type = line[0];
                string[] parts;

                var log = new Log();

                log.Server = server;
                log.Transaction = transaction;

                try
                {
                    switch (type)
                    {
                        case 'c': // Command
                            parts = line.Split(',', CommandParts);
                            if (parts.Length != CommandParts)
                                throw new ArgumentException($"Command entry does not have {CommandParts} parts");

                            log.LogType = "command";
                            log.Command = parts[1];
                            log.Username = parts[2];
                            log.Amount = parts[3] == null ? (decimal?)decimal.Parse(parts[3]) : null;
                            log.StockSymbol = parts[4];
                            log.Filename = parts[5];
                            log.Timestamp = ulong.Parse(parts[6]);

                            break;
                        case 'e': // Event
                            parts = line.Split(',', EventParts);
                            if (parts.Length != EventParts)
                                throw new ArgumentException($"Event entry does not have {EventParts} parts");

                            log.LogType = parts[1].ToLower();
                            log.Username = parts[2];
                            log.Amount = parts[3] == null ? (decimal?)decimal.Parse(parts[3]) : null;
                            log.StockSymbol = parts[4];
                            log.Filename = parts[5];
                            log.Timestamp = ulong.Parse(parts[6]);
                            log.Message = parts[7];

                            break;
                        case 't': // Transaction
                            parts = line.Split(',', TransactionParts);
                            if (parts.Length != TransactionParts)
                                throw new ArgumentException($"Event entry does not have {TransactionParts} parts");
                            
                            log.LogType = "transaction";
                            log.Username = parts[1];
                            log.Amount = parts[2] == null ? (decimal?)decimal.Parse(parts[2]) : null;
                            log.Timestamp = ulong.Parse(parts[3]);
                            log.Message = parts[4];

                            break;
                        case 'q': // Quote
                            parts = line.Split(',', QuoteParts);
                            if (parts.Length != QuoteParts)
                                throw new ArgumentException($"Event entry does not have {QuoteParts} parts");

                            log.LogType = "transaction";
                            log.Amount = parts[1] == null ? (decimal?)decimal.Parse(parts[1]) : null;
                            log.StockSymbol = parts[2];
                            log.Username = parts[3];
                            log.Timestamp = ulong.Parse(parts[4]);
                            log.Cryptokey = parts[5];

                            break;
                        default:
                            throw new ArgumentException($"Unknown log entry type {type}");
                    }

                    db.Logs.Add(log);
                    db.SaveChanges();

                    // TODO: Change this behaviour. But this is fine for the workload files
                    if (log.Command == "DUMPLOG")
                    {
                        Console.WriteLine("DUMPLOG Encountered!");
                        //LogXmlHelper.CreateLog(log.Filename, db);
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
