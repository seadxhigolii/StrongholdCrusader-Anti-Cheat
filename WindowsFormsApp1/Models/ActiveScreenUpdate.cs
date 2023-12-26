using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models
{
    public class ActiveScreenUpdate : BaseClientPayload
    {
        [JsonProperty("value")]
        public int Value { get; set; }
    }
}
