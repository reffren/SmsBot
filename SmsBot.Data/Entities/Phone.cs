using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsBot.Data.Entities
{
    public class Phone
    {
        public int PhoneID { get; set; }
        public string Date { get; set; }
        public string NumPhone { get; set; }
        public string Message { get; set; }
        public int Status { get; set; }
    }
}
