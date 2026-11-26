import { Routes } from '@angular/router';
import { TrackDetailComponent } from './pages/track-detail/track-detail';
import { TrackListComponent } from './pages/track-list/track-list';

export const routes: Routes = [
  { path: '', component: TrackListComponent },
  { path: 'tracks/:id', component: TrackDetailComponent },
  { path: '**', redirectTo: '' }
];
