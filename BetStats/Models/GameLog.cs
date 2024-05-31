using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BetStats.Models
{
    public class GameLog
    {
        public string GameID { get; set; }
        public string Matchup { get; set; }
        public DateTime GameDate { get; set; }
        public int Minutes { get; set; }
        public int Points { get; set; }
        public int Assists { get; set; }
        public int Rebounds { get; set; }
        public int Steals { get; set; }
        public int Blocks { get; set; }
        // Aggiungi altre proprietà come necessario
    }
}