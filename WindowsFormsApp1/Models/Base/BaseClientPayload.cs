﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models
{
    public class BaseClientPayload
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
