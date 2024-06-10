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
    private static readonly ApiRateLimiter _rateLimiter = new ApiRateLimiter(10, TimeSpan.FromMinutes(1));

    public FootballController()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", "");
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "v3.football.api-sports.io");
    }

    public async Task<ActionResult> Leagues()
    {
        var leagues = await GetLeaguesAsync();
        return View(leagues);
    }

    public async Task<ActionResult> Teams(string leagueId, string leagueName, string currentSeason)
    {
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
        var matchDetails = await GetMatchDetailsLogsAsync(teamId, currentSeason);
        ViewBag.TeamName = teamName;
        return View(matchDetails);
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
                matchDetails.Add(new MatchDetail
                {
                    MatchID = match["fixture"]["id"].ToString(),
                    MatchDate = DateTime.Parse(match["fixture"]["date"].ToString()),
                    Referee = match["fixture"]["referee"].ToString(),
                    Status = match["fixture"]["status"]["long"].ToString(),
                    League = match["league"]["name"].ToString() + " (" + match["league"]["country"].ToString() + ")",
                    Matchup = match["teams"]["home"]["name"].ToString() + " - " + match["teams"]["away"]["name"].ToString(),
                    ResultRegularTime = match["score"]["fulltime"]["home"].ToString() + " - " + match["score"]["fulltime"]["away"].ToString(),
                    EvenOdd = ((int.Parse(match["score"]["fulltime"]["home"].ToString()) + int.Parse(match["score"]["fulltime"]["away"].ToString())) % 2 == 0) ? "P" : "D"
                });
            }
        }
        _cache.Set(cacheKey, matchDetails, DateTimeOffset.Now.AddHours(1));
        return matchDetails;
    }
}
