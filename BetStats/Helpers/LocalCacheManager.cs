using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class LocalCacheManager
{
    private readonly string _cacheFilePath;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public LocalCacheManager(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
    }

    public async Task<bool> CheckStatsExistsAsync(string matchId)
    {
        var cache = await LoadCacheAsync();
        return cache.ContainsKey(matchId);
    }

    public async Task AddStatsToCacheAsync(string matchId, JToken stats)
    {
        var cache = await LoadCacheAsync();
        cache[matchId] = stats;
        await SaveCacheAsync(cache);
    }

    public async Task<JToken> GetStatsFromCacheAsync(string matchId)
    {
        var cache = await LoadCacheAsync();
        return cache.ContainsKey(matchId) ? cache[matchId] : null;
    }

    private async Task<Dictionary<string, JToken>> LoadCacheAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return new Dictionary<string, JToken>();
            }

            var json = File.ReadAllText(_cacheFilePath);
            return JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SaveCacheAsync(Dictionary<string, JToken> cache)
    {
        await _semaphore.WaitAsync();
        try
        {
            var json = JsonConvert.SerializeObject(cache, Formatting.Indented);
            File.WriteAllText(_cacheFilePath, json);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
