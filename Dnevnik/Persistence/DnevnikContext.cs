using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Dnevnik.Models;

namespace Dnevnik.Persistence
{
    public class DnevnikContext : DbContext
    {
        public DnevnikContext() : base()
        {
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet <Comment>  Comments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=.;Database=DnevnikDb;Trusted_Connection=True;");
        }
    }
}