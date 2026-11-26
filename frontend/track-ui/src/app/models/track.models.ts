export type TrackStatus = 'Draft' | 'Submitted' | 'Distributed';
export type DistributionStatus = 'Pending' | 'Live' | 'Rejected';

export interface TrackListItem {
  id: number;
  title: string;
  artistName: string;
  genre: string;
  status: TrackStatus;
  isrc: string;
  releaseDate: string;
}

export interface TrackArtist {
  id: number;
  name: string;
  country: string;
}

export interface TrackDistribution {
  dspName: string;
  status: DistributionStatus;
  submittedAt: string;
}

export interface TrackDetail {
  id: number;
  title: string;
  isrc: string;
  releaseDate: string;
  genre: string;
  status: TrackStatus;
  artist: TrackArtist;
  distributions: TrackDistribution[];
}
