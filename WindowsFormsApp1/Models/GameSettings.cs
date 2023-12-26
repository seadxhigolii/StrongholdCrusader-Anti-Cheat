using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models
{
    public class GameSettings : BaseClientPayload
    {
        [JsonProperty("gold")]
        public int Gold { get; set; }
        [JsonProperty("pt")]
        public int Pt { get; set; }
        [JsonProperty("gameSpeed")]
        public int GameSpeed { get; set; }
        [JsonProperty("gameType")]
        public int GameType { get; set; }
        [JsonProperty("mapName")]
        public string MapName { get; set; }
    }

}
