using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BetStats.Models
{
    public class Team
    {
        public string TeamID { get; set; }
        public string TeamName { get; set; }
        public string CurrentSeason { get; set; }
        public string LastEvenOddCount { get; set; }

    }
}