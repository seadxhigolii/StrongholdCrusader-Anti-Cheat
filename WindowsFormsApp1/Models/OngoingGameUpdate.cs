using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models
{
    public class OngoingGameUpdate : BaseClientPayload
    {
        public GranaryInfo granary { get; set; }
        public int fearFactor { get; set; }
        public int activeTaxes { get; set; }
        public CurrentDateInfo currentDate { get; set; }
        public PopulationInfo population { get; set; }
        public LeaderboardInfo leaderboard { get; set; }
    }

    public class GranaryInfo
    {
        public InventoryInfo inventory { get; set; }
        public int currentRations { get; set; }

      
    }
    public class InventoryInfo
    {
        public int apples { get; set; }
        public int meat { get; set; }
        public int cheese { get; set; }
        public int bread { get; set; }
    }
    public class CurrentDateInfo
    {
        public int month { get; set; }
        public int year { get; set; }
    }

    public class PopulationInfo
    {
        public int count { get; set; }
        public int max { get; set; }
        public int popularity { get; set; }
    }

    public class LeaderboardInfo
    {
        public PlayerInfo red { get; set; }
        public PlayerInfo orange { get; set; }
    }

    public class PlayerInfo
    {
        public string nickname { get; set; }
        public int gold { get; set; }
        public int troopsCount { get; set; }
        public int lordHp { get; set; }
    }
}
