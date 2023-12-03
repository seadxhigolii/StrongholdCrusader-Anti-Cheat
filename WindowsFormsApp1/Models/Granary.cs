using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Models
{
    public class Granary
    {
        public Inventory inventory;
        public int currentRations;

        public class Inventory
        {
            public int apples;
            public int meat;
            public int cheese;
            public int bread;
        }
    }
}
