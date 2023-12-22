using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models.Client
{
    public class ClientAuthUpdatePayload : BaseClientPayload
    {
        public int RemainingClosings { get; set; }
        public int RemainingOpenings { get; set; }
        public int ErrorCode { get; set; }
    }
}
