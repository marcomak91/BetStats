using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BetStats.Models
{
    public class MatchDetail
    {
        public string MatchID { get; set; }
        public DateTime MatchDate { get; set; }
        public string Referee { get; set; }
        public string Status { get; set; }
        public string League { get; set; }
        public string Matchup { get; set; }
        public string ResultRegularTime { get; set; }
        public string EvenOdd { get; set; }
    }
}