using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models.Client
{
    public class ClientGamerangerStatusPayload : BaseClientPayload
    {
        public bool IsFactual { get; set; }
        public int ErrorCode { get; set; }
    }
}
