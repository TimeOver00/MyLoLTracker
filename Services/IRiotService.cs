using MyLoLTracker.Models;

namespace MyLoLTracker.Services;

public interface IRiotService
{
    Task<AccountDto?> GetAccountAsync(string gameName, string tagLine);
    Task<SummonerDto?> GetSummonerAsync(string puuid);
    Task<List<LeagueEntryDto>> GetRankedAsync(string puuid);
    Task<List<MatchSummary>> GetRecentMatchesAsync(string puuid, int count);
}
