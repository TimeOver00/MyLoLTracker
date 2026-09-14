namespace MyLoLTracker.Models;

public record AccountDto(string Puuid, string GameName, string TagLine);

public record SummonerDto(
    string Id,
    string Puuid,
    long SummonerLevel,
    int ProfileIconId
);