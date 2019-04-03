using System;
using System.Data;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Data.Common;
using System.IO;
using System.Linq;
using Lora.Models;
using System.Collections.Generic;

namespace Lora
{
    public static class LogXmlHelper
    {
        public static void CreateLog(string filename, LogContext db, string username = null)
        {            
            // If the log exists, rename it to something else
            if (File.Exists(filename))
            {
                var num = 1;
                while (File.Exists($"{filename}.old{num}"))
                    num++;
                
                File.Move(filename, $"{filename}.old{num}");
            }

            var transactions = new Dictionary<string, int>();
            var transactionCount = 0;

            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
            using (var xmlWriter = XmlWriter.Create(filename, settings))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("log");

                foreach (var log in db.Logs)
                {
                    int transactionNum;
                    if (!transactions.TryGetValue(log.Transaction, out transactionNum))
                    {
                        transactionNum = ++transactionCount;
                        transactions.Add(log.Transaction, transactionNum);
                    }

                    switch (log.LogType)
                    {
                        case "command":
                            xmlWriter.WriteStartElement("userCommand");
                            WriteRequiredValues(xmlWriter, log, transactionNum);
                            WriteCommonValues(xmlWriter, log);
                            xmlWriter.WriteEndElement();
                            break;

                        case "quote":
                            xmlWriter.WriteStartElement("quoteServer");
                            WriteRequiredValues(xmlWriter, log, transactionNum);
                            xmlWriter.WriteElementString("quoteServerTime", log.Timestamp.ToString());
                            xmlWriter.WriteElementString("username", log.Username);
                            xmlWriter.WriteElementString("stockSymbol", log.StockSymbol);
                            xmlWriter.WriteElementString("price", log.Amount.ToString());
                            xmlWriter.WriteElementString("cryptokey", log.Cryptokey);
                            xmlWriter.WriteEndElement();
                            break;

                        case "transaction":
                            xmlWriter.WriteStartElement("accountTransaction");
                            WriteRequiredValues(xmlWriter, log, transactionNum);
                            xmlWriter.WriteElementString("action", log.Message);
                            xmlWriter.WriteElementString("username", log.Username);
                            xmlWriter.WriteElementString("funds", log.Amount.ToString());
                            xmlWriter.WriteEndElement();
                            break;

                        case "system":
                            xmlWriter.WriteStartElement("systemEvent");
                            WriteRequiredValues(xmlWriter, log, transactionNum);
                            WriteCommonValues(xmlWriter, log);
                            xmlWriter.WriteEndElement();
                            break;

                        case "error":
                            xmlWriter.WriteStartElement("errorEvent");
                            WriteRequiredValues(xmlWriter, log, transactionNum);
                            WriteCommonValues(xmlWriter, log);
                            xmlWriter.WriteElementString("errorMessage", log.Message);
                            xmlWriter.WriteEndElement();
                            break;

                        case "debug":
                            xmlWriter.WriteStartElement("debugEvent");
                            WriteRequiredValues(xmlWriter, log, transactionNum);
                            WriteCommonValues(xmlWriter, log);
                            xmlWriter.WriteElementString("debugMessage", log.Message);
                            xmlWriter.WriteEndElement();
                            break;
                    }
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
        }

        private static void WriteRequiredValues(XmlWriter xmlWriter, Log log, int transactionNum)
        {
            xmlWriter.WriteElementString("timestamp", log.Timestamp.ToString());
            xmlWriter.WriteElementString("server", log.Server);
            xmlWriter.WriteElementString("transactionNum", transactionNum.ToString());
        }

        private static void WriteCommonValues(XmlWriter xmlWriter, Log log)
        {
            xmlWriter.WriteElementString("command", log.Command);

            if (!string.IsNullOrEmpty(log.Username)) xmlWriter.WriteElementString("username", log.Username);
            if (!string.IsNullOrEmpty(log.StockSymbol)) xmlWriter.WriteElementString("stockSymbol", log.StockSymbol);
            if (!string.IsNullOrEmpty(log.Filename)) xmlWriter.WriteElementString("filename", log.Filename);
            if (log.Amount != null) xmlWriter.WriteElementString("funds", log.Amount.ToString());
        }
    }
}
