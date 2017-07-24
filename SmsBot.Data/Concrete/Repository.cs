using SmsBot.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsBot.Data.Concrete
{
    public class Repository
    {
        private EFContext context = new EFContext();

        public IQueryable<Phone> Phones
        {
            get { return context.Phones; }
        }

        public void SavePhone(Phone phone)
        {
            if (phone.PhoneID == 0)
            {
                context.Phones.Add(phone);
            }
            else
            {
                context.Entry(phone).State = EntityState.Modified; // Indicating that the record is changed
            }
            context.SaveChanges();
        }
    }
}
