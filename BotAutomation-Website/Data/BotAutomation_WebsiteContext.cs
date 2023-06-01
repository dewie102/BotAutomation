using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutomationUtilities.Models;

namespace BotAutomation_Website.Data
{
    public class BotAutomation_WebsiteContext : DbContext
    {
        public BotAutomation_WebsiteContext (DbContextOptions<BotAutomation_WebsiteContext> options)
            : base(options)
        {
        }

        public DbSet<Notice> Notice { get; set; } = default!;
    }
}
