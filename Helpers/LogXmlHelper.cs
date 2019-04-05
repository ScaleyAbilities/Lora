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
        public static string CreateLog(LogContext db, string username = null)
        {
            var transactions = new Dictionary<string, int>();
            var transactionCount = 0;
            string logString;

            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("log");

                foreach (var log in username == null ? db.Logs : db.Logs.Where(log => log.Username == username))
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
                xmlWriter.Flush();

                logString = stringWriter.ToString();
            }

            return logString;
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
