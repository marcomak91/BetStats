using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BetStats.Models
{
    public class MatchDetail
    {
        private Stats _team1;
        private Stats _team2;
        private Stats _matchStats;

        public string MatchID { get; set; }
        public DateTime MatchDate { get; set; }
        public string Referee { get; set; }
        public string Status { get; set; }
        public string League { get; set; }
        public string Matchup { get; set; }
        public string ResultRegularTime { get; set; }
        public string EvenOdd { get; set; }
        public Stats Team1Stats
        {
            get => _team1;
            set
            {
                _team1 = value;
                UpdateMatchStats();
            }
        }
        
        public Stats Team2Stats
        {
            get => _team2;
            set
            {
                _team2 = value;
                UpdateMatchStats();
            }
        }
        
        public Stats MatchStats => _matchStats;

        private void UpdateMatchStats()
        {
            _matchStats = new Stats
            {
                Id = MatchID,
                CornerKicks = (Team1Stats?.CornerKicks ?? 0) + (Team2Stats?.CornerKicks ?? 0),
                ShotsOnGoal = (Team1Stats?.ShotsOnGoal ?? 0) + (Team2Stats?.ShotsOnGoal ?? 0),
                ShotsOffGoal = (Team1Stats?.ShotsOffGoal ?? 0) + (Team2Stats?.ShotsOffGoal ?? 0),
                GoalkeeperSaves = (Team1Stats?.GoalkeeperSaves ?? 0) + (Team2Stats?.GoalkeeperSaves ?? 0),
                Fouls = (Team1Stats?.Fouls ?? 0) + (Team2Stats?.Fouls ?? 0),
                YellowCards = (Team1Stats?.YellowCards ?? 0) + (Team2Stats?.YellowCards ?? 0),
                RedCards = (Team1Stats?.RedCards ?? 0) + (Team2Stats?.RedCards ?? 0),
                Offsides = (Team1Stats?.Offsides ?? 0) + (Team2Stats?.Offsides ?? 0)
            };
        }
    }
}