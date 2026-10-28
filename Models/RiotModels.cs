namespace MyLoLTracker.Models;

public record AccountDto(string Puuid, string GameName, string TagLine);

public record SummonerDto(
    string Id,
    string Puuid,
    long SummonerLevel,
    int ProfileIconId
);

public record LeagueEntryDto(
    string QueueType,
    string Tier,
    string Rank,
    int LeaguePoints,
    int Wins,
    int Losses
);

public record MatchDto(MatchInfoDto Info);

public record MatchInfoDto(
    long GameDuration,
    string GameMode,
    List<ParticipantDto> Participants
);

public record ParticipantDto(
    string Puuid,
    string ChampionName,
    int Kills,
    int Deaths,
    int Assists,
    bool Win
);

public record MatchSummary(
    string MatchId,
    string GameMode,
    long DurationSeconds,
    string Champion,
    string Kda,
    bool Win
);
