export interface Profile {
  gameName: string;
  tagLine: string;
  summonerLevel: number;
  profileIconId: number;
}

export interface RankedEntry {
  queueType: string;
  tier: string;
  rank: string;
  leaguePoints: number;
  wins: number;
  losses: number;
  winRate: number;
}

export interface MatchSummary {
  matchId: string;
  gameMode: string;
  durationSeconds: number;
  champion: string;
  kda: string;
  win: boolean;
}

export interface PlayerResult {
  profile: Profile;
  ranked: RankedEntry[];
  matches: MatchSummary[];
}
