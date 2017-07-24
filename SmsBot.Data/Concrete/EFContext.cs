using SmsBot.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsBot.Data.Concrete
{
    public class EFContext : DbContext
    {
        public EFContext() : base("DefaultConnection") { }
        public DbSet<Phone> Phones { get; set; }
    }
}
