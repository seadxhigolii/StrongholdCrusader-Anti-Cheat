using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models
{   
    public class FirstAuthentication :BaseClientPayload
    {
        public string Email { get; set; }
        public int GameRangerId{ get; set; }
        public string Token { get; set; }
        public string[] KnownMacAddresses { get; set; }
    }
}
