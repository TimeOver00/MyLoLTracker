using Microsoft.Extensions.Caching.Memory;
using MyLoLTracker.Models;

namespace MyLoLTracker.Services;

public class RiotService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public RiotService(HttpClient http, IConfiguration config, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
        var apiKey = config["Riot:ApiKey"]
            ?? throw new InvalidOperationException("Clé API Riot manquante");
        _http.DefaultRequestHeaders.Add("X-Riot-Token", apiKey);
    }

    public Task<AccountDto?> GetAccountAsync(string gameName, string tagLine) =>
        GetCachedAsync<AccountDto>(
            $"account:{gameName}#{tagLine}".ToLowerInvariant(),
            TimeSpan.FromHours(1),
            $"https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}");

    public Task<SummonerDto?> GetSummonerAsync(string puuid) =>
        GetCachedAsync<SummonerDto>(
            $"summoner:{puuid}",
            TimeSpan.FromMinutes(5),
            $"https://euw1.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{puuid}");

    public async Task<List<LeagueEntryDto>> GetRankedAsync(string puuid) =>
        await GetCachedAsync<List<LeagueEntryDto>>(
            $"ranked:{puuid}",
            TimeSpan.FromMinutes(5),
            $"https://euw1.api.riotgames.com/lol/league/v4/entries/by-puuid/{puuid}")
        ?? [];

    public async Task<List<MatchSummary>> GetRecentMatchesAsync(string puuid, int count)
    {
        var ids = await GetCachedAsync<List<string>>(
            $"matchids:{puuid}:{count}",
            TimeSpan.FromMinutes(2),
            $"https://europe.api.riotgames.com/lol/match/v5/matches/by-puuid/{puuid}/ids?start=0&count={count}")
            ?? [];

        var matches = await Task.WhenAll(ids.Select(id => GetMatchSummaryAsync(id, puuid)));
        return matches.Where(m => m is not null).Select(m => m!).ToList();
    }

    private async Task<MatchSummary?> GetMatchSummaryAsync(string matchId, string puuid)
    {
        // Un match terminé ne change plus : on le garde longtemps en cache.
        var match = await GetCachedAsync<MatchDto>(
            $"match:{matchId}",
            TimeSpan.FromHours(24),
            $"https://europe.api.riotgames.com/lol/match/v5/matches/{matchId}");

        var player = match?.Info.Participants.FirstOrDefault(p => p.Puuid == puuid);
        if (match is null || player is null)
            return null;

        return new MatchSummary(
            matchId,
            match.Info.GameMode,
            match.Info.GameDuration,
            player.ChampionName,
            $"{player.Kills}/{player.Deaths}/{player.Assists}",
            player.Win);
    }

    private async Task<T?> GetCachedAsync<T>(string cacheKey, TimeSpan ttl, string url) where T : class
    {
        if (_cache.TryGetValue(cacheKey, out T? cached))
            return cached;

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<T>();
        if (result is not null)
            _cache.Set(cacheKey, result, ttl);

        return result;
    }
}
