using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models.Client
{
    
    public class ClientAuthCompletePayload : BaseClientPayload
    {
        public bool AuthStatus { get; set; }
        public int ErrorCode { get; set; }
    }
}
