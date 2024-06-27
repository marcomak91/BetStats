using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BetStats.Models
{
    public class Stats
    {
        public string Id { get; set; }
        public int CornerKicks { get; set; }
        public int ShotsOnGoal { get; set; }
        public int ShotsOffGoal { get; set; }
        public int GoalkeeperSaves { get; set; }
        public int Fouls { get; set; }
        public int YellowCards { get; set; }
        public int RedCards { get; set; }
        public int Offsides { get; set; }
        public int TotalShots => ShotsOnGoal + ShotsOffGoal;
        public int TotalCards => YellowCards + RedCards;
    }
}