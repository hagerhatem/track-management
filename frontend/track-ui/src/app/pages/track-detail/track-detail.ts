import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TrackDetail } from '../../models/track.models';
import { TrackService } from '../../services/track.service';

@Component({
  selector: 'app-track-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe],
  templateUrl: './track-detail.html',
  styleUrl: './track-detail.css'
})
export class TrackDetailComponent {
  private readonly service = inject(TrackService);
  private readonly route = inject(ActivatedRoute);

  protected readonly track = signal<TrackDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly error = signal(false);

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id <= 0) {
      this.notFound.set(true);
      this.loading.set(false);
      return;
    }
    this.service.getTrack(id).subscribe({
      next: track => {
        this.track.set(track);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          this.notFound.set(true);
        } else {
          this.error.set(true);
        }
        this.loading.set(false);
      }
    });
  }

  protected badgeClass(status: string): string {
    return 'badge badge-' + status.toLowerCase();
  }
}
