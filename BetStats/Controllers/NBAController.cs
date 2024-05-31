using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using BetStats.Models;

public class NBAController : Controller
{
    private readonly HttpClient _httpClient;

    public NBAController()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://stats.nba.com/");
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://stats.nba.com");
        _httpClient.DefaultRequestHeaders.Add("Host", "stats.nba.com");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "it-IT,it;q=0.9,en-US;q=0.8,en;q=0.7");
    }

    public async Task<ActionResult> Teams()
    {
        var teams = await GetTeamsAsync();
        return View(teams);
    }

    public async Task<ActionResult> Players(string teamId, string teamName)
    {
        var players = await GetPlayersAsync(teamId);
        ViewBag.TeamName = teamName;
        return View(players);
    }

    public async Task<ActionResult> PlayerDetails(string playerId, string playerName)
    {
        var gameLogs = await GetPlayerGameLogsAsync(playerId, "2023-24");
        ViewBag.PlayerName = playerName;
        return View(gameLogs);
    }

    private async Task<List<Team>> GetTeamsAsync()
    {
        string url = "https://stats.nba.com/stats/leaguestandingsv3?LeagueID=00&Season=2023-24&SeasonType=Regular+Season";
        var response = await _httpClient.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        var teams = new List<Team>();
        foreach (var team in json["resultSets"][0]["rowSet"])
        {
            teams.Add(new Team
            {
                TeamID = team[2].ToString(),
                TeamName = team[3].ToString() + " " + team[4].ToString()
            });
        }
        return teams;
    }

    private async Task<List<Player>> GetPlayersAsync(string teamId)
    {
        string url = $"https://stats.nba.com/stats/commonteamroster?TeamID={teamId}&Season=2023-24";
        var response = await _httpClient.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        var players = new List<Player>();
        foreach (var player in json["resultSets"][0]["rowSet"])
        {
            players.Add(new Player
            {
                PlayerID = player[14].ToString(),
                PlayerName = player[3].ToString(),
            });
        }
        return players;
    }

    private async Task<List<GameLog>> GetPlayerGameLogsAsync(string playerId, string season)
    {
        string url = $"https://stats.nba.com/stats/playergamelog?PlayerID={playerId}&Season={season}&SeasonType=Regular+Season";
        var response = await _httpClient.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        var gameLogs = new List<GameLog>();
        foreach (var game in json["resultSets"][0]["rowSet"])
        {
            gameLogs.Add(new GameLog
            {
                GameID = game[2].ToString(),
                Matchup = game[4].ToString(),
                GameDate = DateTime.Parse(game[3].ToString()),
                Minutes = int.Parse(game[6].ToString()),
                Points = int.Parse(game[24].ToString()),
                Assists = int.Parse(game[19].ToString()),
                Rebounds = int.Parse(game[18].ToString()),
                Steals = int.Parse(game[20].ToString()),
                Blocks = int.Parse(game[21].ToString())
            });
        }
        return gameLogs;
    }
}
