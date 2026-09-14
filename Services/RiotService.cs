using MyLoLTracker.Models;

namespace MyLoLTracker.Services;

public class RiotService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public RiotService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var apiKey = config["Riot:ApiKey"]
            ?? throw new InvalidOperationException("Clé API Riot manquante");
        _http.DefaultRequestHeaders.Add("X-Riot-Token", apiKey);
    }

    public async Task<AccountDto?> GetAccountAsync(string gameName, string tagLine)
    {
        var url = $"https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{gameName}/{tagLine}";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AccountDto>();
    }

    public async Task<SummonerDto?> GetSummonerAsync(string puuid)
    {
        var url = $"https://euw1.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{puuid}";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SummonerDto>();
    }
}