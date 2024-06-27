using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using BetStats.Models;
using System.Threading;
using System.Linq;
using System.Runtime.Caching;

public class FootballController : Controller
{
    private readonly HttpClient _httpClient;
    private static readonly ObjectCache _cache = MemoryCache.Default;
    private static readonly ApiRateLimiter _rateLimiter = new ApiRateLimiter(9, TimeSpan.FromMinutes(1));
    private LocalCacheManager _localCacheManagerMatchStats;
    private LocalCacheManager _localCacheManagerMatchPlayerStats;
    private static List<MatchDetail> _matchDetails = new List<MatchDetail>();

    public FootballController()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", ApiKeyHelper.GetApiKey());
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "v3.football.api-sports.io");
    }

    public async Task<ActionResult> Leagues()
    {
        InitializeCacheManager();

        var leagues = await GetLeaguesAsync();
        return View(leagues);
    }

    public async Task<ActionResult> Teams(string leagueId, string leagueName, string currentSeason)
    {
        InitializeCacheManager();

        var teams = await GetTeamsAsync(leagueId, currentSeason);

        var tasks = teams.Select(async team =>
        {
            var matchDetails = await GetMatchDetailsLogsAsync(team.TeamID, currentSeason);

            if (matchDetails.Any())
            {
                // Ordinare i dettagli della partita per data decrescente
                var orderedMatchDetails = matchDetails.OrderByDescending(md => md.MatchDate).ToList();

                // Trova la sequenza più lunga di occorrenze consecutive del valore EvenOdd più recente
                string lastEvenOdd = orderedMatchDetails.First().EvenOdd;
                int count = 0;

                foreach (var detail in orderedMatchDetails)
                {
                    if (detail.EvenOdd == lastEvenOdd)
                    {
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }

                team.LastEvenOddCount = count + " " + lastEvenOdd;
            }
            else
            {
                team.LastEvenOddCount = "N/A";
            }

        }).ToList();

        await Task.WhenAll(tasks);

        ViewBag.LeagueName = leagueName;
        return View(teams);
    }

    public async Task<ActionResult> MatchDetails(string teamId, string teamName, string currentSeason)
    {
        InitializeCacheManager();

        var matchDetails = await GetMatchDetailsLogsAsync(teamId, currentSeason);
        _matchDetails = matchDetails;
        ViewBag.TeamName = teamName;
        return View(matchDetails);
    }

    public ActionResult MatchStats(string matchId)
    {
        InitializeCacheManager();

        var matchDetail = _matchDetails.FirstOrDefault(m => m.MatchID == matchId);
        if (matchDetail == null)
        {
            return HttpNotFound();
        }

        return PartialView("_MatchStats", matchDetail);
    }

    private async Task<List<League>> GetLeaguesAsync()
    {
        string cacheKey = "leagues";
        if (_cache.Contains(cacheKey) && ((List<League>)_cache.Get(cacheKey)).Count() > 0)
        {
            // Prolungare la durata dell'oggetto nella cache
            _cache.Set(cacheKey, (List<League>)_cache.Get(cacheKey), DateTimeOffset.Now.AddHours(1));

            return (List<League>)_cache.Get(cacheKey);
        }

        await _rateLimiter.WaitForNextRequestAsync();

        string url = "https://v3.football.api-sports.io/leagues?current=true";
        var response = await _httpClient.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        var leagues = new List<League>();

        if (json["errors"].Count() > 0)
        {
            throw new Exception(json["errors"].ToString());
        }
        else
        {
            foreach (var league in json["response"])
            {
                if (DateTime.Now >= DateTime.Parse(league["seasons"][0]["start"].ToString()) && DateTime.Now <= DateTime.Parse(league["seasons"][0]["end"].ToString()))
                {
                    leagues.Add(new League
                    {
                        LeagueID = league["league"]["id"].ToString(),
                        LeagueName = league["league"]["name"].ToString(),
                        LeagueNation = league["country"]["name"].ToString(),
                        LeagueType = league["league"]["type"].ToString(),
                        CurrentSeason = league["seasons"][0]["year"].ToString()
                    });
                }
            }
        }

        _cache.Set(cacheKey, leagues, DateTimeOffset.Now.AddHours(1));
        return leagues;
    }

    private async Task<List<Team>> GetTeamsAsync(string leagueId, string currentSeason)
    {
        string cacheKey = $"teams_{leagueId}";
        if (_cache.Contains(cacheKey) && ((List<Team>)_cache.Get(cacheKey)).Count() > 0)
        {
            // Prolungare la durata dell'oggetto nella cache
            _cache.Set(cacheKey, (List<Team>)_cache.Get(cacheKey), DateTimeOffset.Now.AddHours(1));

            return (List<Team>)_cache.Get(cacheKey);
        }

        await _rateLimiter.WaitForNextRequestAsync();

        string url = $"https://v3.football.api-sports.io/teams?league={leagueId}&season={currentSeason}";
        var response = await _httpClient.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        var teams = new List<Team>();

        if (json["errors"].Count() > 0)
        {
            throw new Exception(json["errors"].ToString());
        }
        else
        {
            foreach (var team in json["response"])
            {
                teams.Add(new Team
                {
                    TeamID = team["team"]["id"].ToString(),
                    TeamName = team["team"]["name"].ToString(),
                    CurrentSeason = currentSeason
                });
            }
        }

        _cache.Set(cacheKey, teams, DateTimeOffset.Now.AddHours(1));
        return teams;
    }

    private async Task<List<MatchDetail>> GetMatchDetailsLogsAsync(string teamId, string season)
    {
        string cacheKey = $"match_{teamId}";
        if (_cache.Contains(cacheKey) && ((List<MatchDetail>)_cache.Get(cacheKey)).Count() > 0)
        {
            // Prolungare la durata dell'oggetto nella cache
            _cache.Set(cacheKey, (List<MatchDetail>)_cache.Get(cacheKey), DateTimeOffset.Now.AddHours(1));

            return (List<MatchDetail>)_cache.Get(cacheKey);
        }

        await _rateLimiter.WaitForNextRequestAsync();

        string url = $"https://v3.football.api-sports.io/fixtures?team={teamId}&season={season}&status=FT-AET-PEN";
        var response = await _httpClient.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        var matchDetails = new List<MatchDetail>();

        if (json["errors"].Count() > 0)
        {
            throw new Exception(json["errors"].ToString());
        }
        else
        {
            foreach (var match in json["response"])
            {
                var matchId = match["fixture"]["id"].ToString();

                JToken matchStats = await _localCacheManagerMatchStats.CheckStatsExistsAsync(matchId) ?
                    await _localCacheManagerMatchStats.GetStatsFromCacheAsync(matchId) :
                    await GetMatchStatsAsync(matchId);

                if (matchStats != null && !await _localCacheManagerMatchStats.CheckStatsExistsAsync(matchId))
                {
                    await _localCacheManagerMatchStats.AddStatsToCacheAsync(matchId, matchStats);
                }

                matchDetails.Add(new MatchDetail
                {
                    MatchID = matchId,
                    MatchDate = DateTime.Parse(match["fixture"]["date"].ToString()),
                    Referee = match["fixture"]["referee"].ToString(),
                    Status = match["fixture"]["status"]["long"].ToString(),
                    League = match["league"]["name"].ToString() + " (" + match["league"]["country"].ToString() + ")",
                    Matchup = match["teams"]["home"]["name"].ToString() + " - " + match["teams"]["away"]["name"].ToString(),
                    ResultRegularTime = match["score"]["fulltime"]["home"].ToString() + " - " + match["score"]["fulltime"]["away"].ToString(),
                    EvenOdd = ((int.Parse(match["score"]["fulltime"]["home"].ToString()) + int.Parse(match["score"]["fulltime"]["away"].ToString())) % 2 == 0) ? "P" : "D",
                    Team1Stats = matchStats != null && matchStats.Count() > 0 ? new Stats()
                    {
                        Id = matchStats[0]["team"]["id"].ToString(),
                        CornerKicks = GetStatValue(matchStats[0]["statistics"], "Corner Kicks"),
                        ShotsOnGoal = GetStatValue(matchStats[0]["statistics"], "Shots on Goal"),
                        ShotsOffGoal = GetStatValue(matchStats[0]["statistics"], "Shots off Goal"),
                        GoalkeeperSaves = GetStatValue(matchStats[0]["statistics"], "Goalkeeper Saves"),
                        Fouls = GetStatValue(matchStats[0]["statistics"], "Fouls"),
                        YellowCards = GetStatValue(matchStats[0]["statistics"], "Yellow Cards"),
                        RedCards = GetStatValue(matchStats[0]["statistics"], "Red Cards"),
                        Offsides = GetStatValue(matchStats[0]["statistics"], "Offsides")
                    } : null,
                    Team2Stats = matchStats != null && matchStats.Count() > 0 ? new Stats()
                    {
                        Id = matchStats[1]["team"]["id"].ToString(),
                        CornerKicks = GetStatValue(matchStats[1]["statistics"], "Corner Kicks"),
                        ShotsOnGoal = GetStatValue(matchStats[1]["statistics"], "Shots on Goal"),
                        ShotsOffGoal = GetStatValue(matchStats[1]["statistics"], "Shots off Goal"),
                        GoalkeeperSaves = GetStatValue(matchStats[1]["statistics"], "Goalkeeper Saves"),
                        Fouls = GetStatValue(matchStats[1]["statistics"], "Fouls"),
                        YellowCards = GetStatValue(matchStats[1]["statistics"], "Yellow Cards"),
                        RedCards = GetStatValue(matchStats[1]["statistics"], "Red Cards"),
                        Offsides = GetStatValue(matchStats[1]["statistics"], "Offsides")
                    } : null
                });
            }
        }
        _cache.Set(cacheKey, matchDetails, DateTimeOffset.Now.AddHours(1));
        return matchDetails;
    }

    private async Task<JToken> GetMatchStatsAsync(string matchId)
    {
        await _rateLimiter.WaitForNextRequestAsync();
        string url = $"https://v3.football.api-sports.io/fixtures/statistics?fixture={matchId}";
        var response = await _httpClient.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        if (json["errors"].Count() > 0)
        {
            throw new Exception(json["errors"].ToString());
        }

        return json["response"];
    }

    private async Task<JToken> GetPlayerStatsAsync(string matchId, string teamId)
    {
        await _rateLimiter.WaitForNextRequestAsync();
        string url = $"https://v3.football.api-sports.io/fixtures/players?fixture={matchId}&team={teamId}";
        var response = await _httpClient.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        if (json["errors"].Count() > 0)
        {
            throw new Exception(json["errors"].ToString());
        }

        return json["response"];
    }

    private int GetStatValue(JToken stats, string statType)
    {
        var stat = stats.FirstOrDefault(s => s["type"].ToString() == statType);
        return stat != null && stat["value"].Type != JTokenType.Null ? (int)stat["value"] : 0;
    }

    private void InitializeCacheManager()
    {
        if (_localCacheManagerMatchStats == null)
        {
            string cacheFilePath = Server.MapPath("~/App_Data/MatchStatsCache.json");
            _localCacheManagerMatchStats = new LocalCacheManager(cacheFilePath);
        }

        if (_localCacheManagerMatchPlayerStats == null)
        {
            string cacheFilePath = Server.MapPath("~/App_Data/PlayerStatsCache.json");
            _localCacheManagerMatchPlayerStats = new LocalCacheManager(cacheFilePath);
        }
    }
}
