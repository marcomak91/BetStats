using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
public static class ApiKeyHelper
{
    public static string GetApiKey()
    {
        // Leggi il percorso del file dal web.config
        string apiKeyFilePath = ConfigurationManager.AppSettings["ApiKeyFilePath"];
        if (string.IsNullOrEmpty(apiKeyFilePath))
        {
            throw new InvalidOperationException("Il percorso del file API key non è configurato nel web.config.");
        }

        // Leggi l'API key dal file
        if (!File.Exists(apiKeyFilePath))
        {
            throw new FileNotFoundException("Il file contenente l'API key non è stato trovato.", apiKeyFilePath);
        }

        return File.ReadAllText(apiKeyFilePath).Trim();
    }
}


public class ApiRateLimiter
{
    private readonly int _maxRequests;
    private readonly TimeSpan _resetInterval;
    private readonly Queue<DateTime> _requestTimes;
    private readonly SemaphoreSlim _semaphore;

    public ApiRateLimiter(int maxRequests, TimeSpan resetInterval)
    {
        _maxRequests = maxRequests;
        _resetInterval = resetInterval;
        _requestTimes = new Queue<DateTime>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task WaitForNextRequestAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            while (_requestTimes.Count >= _maxRequests)
            {
                var now = DateTime.UtcNow;
                var earliestRequest = _requestTimes.Peek();

                if (now - earliestRequest < _resetInterval)
                {
                    var timeUntilEarliestRequest = _resetInterval - (now - earliestRequest);
                    await Task.Delay(timeUntilEarliestRequest + new TimeSpan(0, 0, 2));
                }

                _requestTimes.Dequeue();
            }

            _requestTimes.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
