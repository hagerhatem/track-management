import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TrackListItem, TrackStatus } from '../../models/track.models';
import { TrackService } from '../../services/track.service';

@Component({
  selector: 'app-track-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './track-list.html',
  styleUrl: './track-list.css'
})
export class TrackListComponent {
  private readonly service = inject(TrackService);
  private readonly router = inject(Router);

  protected readonly statuses: TrackStatus[] = ['Draft', 'Submitted', 'Distributed'];
  protected readonly tracks = signal<TrackListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    this.load();
  }

  protected onFilterChange(value: string): void {
    this.load(this.statuses.find(s => s === value));
  }

  protected open(id: number): void {
    this.router.navigate(['/tracks', id]);
  }

  protected badgeClass(status: string): string {
    return 'badge badge-' + status.toLowerCase();
  }

  private load(status?: TrackStatus): void {
    this.loading.set(true);
    this.error.set(false);
    this.service.getTracks(status).subscribe({
      next: tracks => {
        this.tracks.set(tracks);
        this.loading.set(false);
      },
      error: () => {
        this.tracks.set([]);
        this.error.set(true);
        this.loading.set(false);
      }
    });
  }
}
