using System;
using System.IO;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using Lora.Models;
using Newtonsoft.Json.Linq;

namespace Lora
{
    class Program
    {
        private const int CommandParts = 7;
        private const int EventParts = 9;
        private const int TransactionParts = 5;
        private const int QuoteParts = 6;
        private const int RequestParts = 3;

        private static int addedLogs = 0;

        private static Timer commitTimer = new Timer(5000) {
            AutoReset = false,
            Enabled = false
        };

        private static LogContext db;

        static void Main(string[] args)
        {  
            Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, eventArgs) => Quit());

            db = new LogContext();

            db.Database.EnsureCreated();

            RabbitHelper.CreateConsumer(AddLogEntry);

            commitTimer.Elapsed += (source, eventArgs) => {
                Save();
            };

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
            // This is just so we can manually trigger a dump to file
            if (logEntry == "SPECIALDUMP")
            {
                var logXml = LogXmlHelper.CreateLog(db);
                WriteLogFile("testLog.xml", logXml);
                return;
            }

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
                            log.Amount = !string.IsNullOrWhiteSpace(parts[3]) ? (decimal?)decimal.Parse(parts[3]) : null;
                            log.StockSymbol = parts[4];
                            log.Filename = parts[5];
                            log.Timestamp = ulong.Parse(parts[6]);

                            break;
                        case 'e': // Event
                            parts = line.Split(',', EventParts);
                            if (parts.Length != EventParts)
                                throw new ArgumentException($"Event entry does not have {EventParts} parts");

                            log.LogType = parts[1].ToLower();
                            log.Command = parts[2];
                            log.Username = parts[3];
                            log.Amount = !string.IsNullOrWhiteSpace(parts[4]) ? (decimal?)decimal.Parse(parts[4]) : null;
                            log.StockSymbol = parts[5];
                            log.Filename = parts[6];
                            log.Timestamp = ulong.Parse(parts[7]);
                            log.Message = parts[8];

                            break;
                        case 't': // Transaction
                            parts = line.Split(',', TransactionParts);
                            if (parts.Length != TransactionParts)
                                throw new ArgumentException($"Event entry does not have {TransactionParts} parts");
                            
                            log.LogType = "transaction";
                            log.Username = parts[1];
                            log.Amount = !string.IsNullOrWhiteSpace(parts[2]) ? (decimal?)decimal.Parse(parts[2]) : null;
                            log.Timestamp = ulong.Parse(parts[3]);
                            log.Message = parts[4];

                            break;
                        case 'q': // Quote
                            parts = line.Split(',', QuoteParts);
                            if (parts.Length != QuoteParts)
                                throw new ArgumentException($"Event entry does not have {QuoteParts} parts");

                            log.LogType = "quote";
                            log.Amount = !string.IsNullOrWhiteSpace(parts[1]) ? (decimal?)decimal.Parse(parts[1]) : null;
                            log.StockSymbol = parts[2];
                            log.Username = parts[3];
                            log.Timestamp = ulong.Parse(parts[4]);
                            log.Cryptokey = parts[5];

                            break;
                        case 'r': // Request for logs to frontent
                            parts = line.Split(',', RequestParts);
                            if (parts.Length != RequestParts)
                                throw new ArgumentException($"Event entry does not have {RequestParts} parts");

                            var username = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1];
                            var reference = parts[2];

                            // Stop the timer in case this takes a while
                            commitTimer.Stop();

                            // Commit now before creating log for user
                            Save();
                            addedLogs = 0;

                            var logXml = LogXmlHelper.CreateLog(db, username);

                            JObject response = new JObject();
                            response.Add("ref", reference);
                            response.Add("status", "ok");
                            response.Add("data", logXml);

                            RabbitHelper.PushResponse(response);

                            // Restart the loop so we don't try to log this special entry
                            continue;
                        default:
                            throw new ArgumentException($"Unknown log entry type {type}");
                    }

                    db.Logs.Add(log);

                    // Restart the commit timer
                    commitTimer.Stop();
                    commitTimer.Start();

                    if (++addedLogs > 1000) {
                        Save();
                        addedLogs = 0;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Invalid entry: {line}. Exception: {e.Message}");
                    continue;
                }
            }
        }

        public static void WriteLogFile(string filename, string log)
        {
            // If the log exists, rename it to something else
            if (File.Exists(filename))
            {
                var num = 1;
                while (File.Exists($"{filename}.old{num}"))
                    num++;
                
                File.Move(filename, $"{filename}.old{num}");
            }

            File.WriteAllText(filename, log);
        }

        public static void Save()
        {
            db.SaveChanges();
            db.Dispose();
            db = new LogContext();
        }
    }
}
