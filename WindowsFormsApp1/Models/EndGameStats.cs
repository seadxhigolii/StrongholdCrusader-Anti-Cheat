using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WindowsFormsApp1.Models.Leaderboard;

namespace WindowsFormsApp1.Models
{
    public class EndGameStats
    {
        public string Type { get; set; }
        public Player Red { get; set; }
        public Player Orange { get; set; }
    }

    public class Player
    {
        public int GoldAcquired { get; set; }
        public int FoodProduced { get; set; }
        public int WoodProduced { get; set; }
        public int StoneProduced { get; set; }
        public int IronProduced { get; set; }
        public int WeaponsProduced { get; set; }
        public int TroopsProduced { get; set; }
        public int HighestPopulation { get; set; }
        public int EnemyBuildingsRazed { get; set; }
        public int BuildingsLost { get; set; }
        // Additional fields for Orange TeamStats
        public int? TroopsLost { get; set; }
        public int? TroopsKilled { get; set; }
    }

}
