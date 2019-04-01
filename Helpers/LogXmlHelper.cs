using System;
using System.Data;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace Lora
{
    public static class LogXmlHelper
    {
        private const int CommandParts = 7;
        private const int EventParts = 8;
        private const int TransactionParts = 5;
        private const int QuoteParts = 6;

        private const string XmlFilename = "log.xml";

        private static long transactionNum = 1;

        private static XmlWriter xmlWriter;
        static LogXmlHelper() 
        {            
            // If the log exists, rename it to something else
            if (File.Exists(XmlFilename))
            {
                var num = 1;
                while (File.Exists($"{XmlFilename}.old{num}"))
                    num++;
                
                File.Move(XmlFilename, $"{XmlFilename}.old{num}");
            }

            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };            
            xmlWriter = XmlWriter.Create(XmlFilename, settings);
            xmlWriter.WriteStartDocument();
        }

        public static void CloseLog() 
        {
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
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
                string command, username, amount, stockSymbol, filename, timestamp, message;

                switch (type)
                {
                    case 'c': // Command
                        parts = line.Split(',', CommandParts);
                        if (parts.Length != CommandParts)
                            Console.WriteLine($"Command entry does not have {CommandParts} parts");

                        command = parts[1];
                        username = parts[2];
                        amount = parts[3];
                        stockSymbol = parts[4];
                        timestamp = parts[5];
                        filename = parts[6];

                        xmlWriter.WriteStartElement("userCommand");
                        WriteRequiredValues(xmlWriter, timestamp, server);
                        WriteCommonValues(xmlWriter, username, stockSymbol, amount, filename);
                        xmlWriter.WriteEndElement();

                        // TODO: Change this behaviour. But this is fine for the workload files
                        if (command == "DUMPLOG")
                        {
                            Console.WriteLine("DUMPLOG Encountered!");
                            Program.Quit();
                        }

                        break;
                    case 'e': // Event
                        parts = line.Split(',', EventParts);
                        if (parts.Length != EventParts)
                            Console.WriteLine($"Event entry does not have {EventParts} parts");

                        var eventType = parts[1].ToLower() + "Event";

                        username = parts[2];
                        amount = parts[3];
                        stockSymbol = parts[4];
                        filename = parts[5];
                        timestamp = parts[6];
                        message = parts[7];

                        xmlWriter.WriteStartElement(eventType);
                        WriteRequiredValues(xmlWriter, timestamp, server);
                        WriteCommonValues(xmlWriter, username, stockSymbol, amount, filename);

                        if (eventType == "errorEvent") 
                            xmlWriter.WriteElementString("errorMessage", message);
                        else if (eventType == "debugEvent")
                            xmlWriter.WriteElementString("debugMessage", message);

                        xmlWriter.WriteEndElement();

                        break;
                    case 't': // Transaction
                        parts = line.Split(',', TransactionParts);
                        if (parts.Length != TransactionParts)
                            Console.WriteLine($"Event entry does not have {TransactionParts} parts");
                        
                        username = parts[1];
                        amount = parts[2];
                        timestamp = parts[3];
                        message = parts[4];

                        xmlWriter.WriteStartElement("accountTransaction");
                        WriteRequiredValues(xmlWriter, timestamp, server);
                        
                        xmlWriter.WriteElementString("action", message);
                        xmlWriter.WriteElementString("username", username);
                        xmlWriter.WriteElementString("funds", amount);

                        xmlWriter.WriteEndElement();
                        break;
                    case 'q': // Quote
                        parts = line.Split(',', QuoteParts);
                        if (parts.Length != QuoteParts)
                            Console.WriteLine($"Event entry does not have {QuoteParts} parts");

                        amount = parts[1];
                        stockSymbol = parts[2];
                        username = parts[3];
                        timestamp = parts[4];

                        var cryptokey = parts[5];

                        xmlWriter.WriteStartElement("quoteServer");
                        WriteRequiredValues(xmlWriter, timestamp, server);

                        xmlWriter.WriteElementString("quoteServerTime", timestamp);
                        xmlWriter.WriteElementString("username", username);
                        xmlWriter.WriteElementString("stockSymbol", stockSymbol);
                        xmlWriter.WriteElementString("price", amount);
                        xmlWriter.WriteElementString("cryptokey", cryptokey);

                        xmlWriter.WriteEndElement();
                        break;
                    default:
                        Console.WriteLine($"Unknown log entry type {type}");
                        break;
                }
            }

            transactionNum++;
        }
    
        private static void WriteRequiredValues(XmlWriter xmlWriter, string timestamp, string server)
        {
            xmlWriter.WriteElementString("timestamp", timestamp);
            xmlWriter.WriteElementString("server", server);
            xmlWriter.WriteElementString("transactionNum", transactionNum.ToString());
        }

        private static void WriteCommonValues(XmlWriter xmlWriter, string username, string stockSymbol, string amount, string filename)
        {
            if (!string.IsNullOrEmpty(username)) xmlWriter.WriteElementString("username", username);
            if (!string.IsNullOrEmpty(stockSymbol)) xmlWriter.WriteElementString("stockSymbol", stockSymbol);
            if (!string.IsNullOrEmpty(filename)) xmlWriter.WriteElementString("filename", filename);
            if (!string.IsNullOrEmpty(amount)) xmlWriter.WriteElementString("funds", amount);
        }
    }
}
