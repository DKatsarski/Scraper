using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dnevnik.Persistence
{
    public class DnevnikContext : DbContext
    {
        public DnevnikContext()
        {
        }

        public DbSet<Article> Articles { get; set; }
    }
}
