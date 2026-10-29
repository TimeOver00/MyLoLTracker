import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { PlayerResult } from './models';
import { RiotService } from './riot.service';

const QUEUE_LABELS: Record<string, string> = {
  RANKED_SOLO_5x5: 'Classée Solo/Duo',
  RANKED_FLEX_SR: 'Classée Flex',
};

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly riot = inject(RiotService);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly player = signal<PlayerResult | null>(null);
  protected readonly gameVersion = signal('15.1.1');

  constructor() {
    this.riot.getLatestGameVersion().subscribe((version) => this.gameVersion.set(version));
  }

  protected search(gameName: string, tagLine: string): void {
    gameName = gameName.trim();
    tagLine = tagLine.trim().replace(/^#/, '');
    if (!gameName || !tagLine) {
      this.error.set('Renseigne le pseudo et le tag.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.player.set(null);

    this.riot.getPlayer(gameName, tagLine).subscribe({
      next: (result) => {
        this.player.set(result);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(this.toMessage(err));
        this.loading.set(false);
      },
    });
  }

  protected queueLabel(queueType: string): string {
    return QUEUE_LABELS[queueType] ?? queueType;
  }

  protected duration(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    const rest = String(seconds % 60).padStart(2, '0');
    return `${minutes}:${rest}`;
  }

  protected profileIcon(id: number): string {
    return `https://ddragon.leagueoflegends.com/cdn/${this.gameVersion()}/img/profileicon/${id}.png`;
  }

  protected championIcon(name: string): string {
    return `https://ddragon.leagueoflegends.com/cdn/${this.gameVersion()}/img/champion/${name}.png`;
  }

  protected hideBrokenImage(event: Event): void {
    (event.target as HTMLImageElement).style.visibility = 'hidden';
  }

  private toMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 0) return "Impossible de joindre l'API. Est-elle lancée ?";
      if (err.status === 404) return 'Joueur introuvable. Vérifie le pseudo et le tag.';
      return err.error?.detail ?? `Erreur inattendue (${err.status}).`;
    }
    return 'Erreur inattendue.';
  }
}
