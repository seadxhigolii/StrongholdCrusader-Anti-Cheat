using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models
{
    public class Leaderboard
    {
        public Player red;
        public Player orange;

        public class Player
        {
            public string nickname;
            public int gold;
            public int troopsCount;
        }
    }
}
