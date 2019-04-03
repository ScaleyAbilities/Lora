using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Lora.Models
{
    public class LogContext : DbContext
    {
        public DbSet<Log> Logs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=logs.db");
        }
    }

    public class Log
    {
        public int LogId { get; set; }
        public string Transaction { get; set; }
        public string LogType { get; set; }
        public string Server { get; set; }
        public ulong Timestamp { get; set; }
        public string Command { get; set; }
        public string Username { get; set; }
        public decimal? Amount { get; set; }
        public string StockSymbol { get; set; }
        public string Filename { get; set; }
        public string Message { get; set; }
        public string Cryptokey { get; set; }
    }
}