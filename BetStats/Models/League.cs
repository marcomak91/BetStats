using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BetStats.Models
{
    public class League
    {
        public string LeagueID { get; set; }
        public string LeagueName { get; set; }
        public string LeagueNation { get; set; }
        public string LeagueType { get; set; }
        public string CurrentSeason { get; set; }
    }
}