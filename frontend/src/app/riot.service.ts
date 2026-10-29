import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { MatchSummary, PlayerResult, Profile, RankedEntry } from './models';

export const API_URL = 'https://localhost:7165';

@Injectable({ providedIn: 'root' })
export class RiotService {
  private readonly http = inject(HttpClient);

  getPlayer(gameName: string, tagLine: string): Observable<PlayerResult> {
    const base = `${API_URL}/summoner/${encodeURIComponent(gameName)}/${encodeURIComponent(tagLine)}`;

    // Le profil d'abord : l'API met le compte en cache, les deux appels suivants en profitent.
    return this.http.get<Profile>(base).pipe(
      switchMap((profile) =>
        forkJoin({
          ranked: this.http.get<RankedEntry[]>(`${base}/ranked`),
          matches: this.http.get<MatchSummary[]>(`${base}/matches?count=10`),
        }).pipe(map(({ ranked, matches }) => ({ profile, ranked, matches }))),
      ),
    );
  }

  getLatestGameVersion(): Observable<string> {
    return this.http
      .get<string[]>('https://ddragon.leagueoflegends.com/api/versions.json')
      .pipe(
        map((versions) => versions[0]),
        catchError(() => of('15.1.1')),
      );
  }
}
