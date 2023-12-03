using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models
{
    public class ClientAuthPayload : BaseClientPayload
    {
        public bool InitialSetupRequired { get; set; }
        public bool IsAuthSuccessful { get; set; }
        public int ErrorCode { get; set; }
        public int RequiredAttempts { get; set; }
        public int TimeLimitPerAttempt { get; set; }
    }
}
