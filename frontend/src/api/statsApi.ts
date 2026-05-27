import { fetchJson } from './client';
import type { StatsStripData, StatsPageData } from '../types';

export const getStrip = () =>
  fetchJson<StatsStripData>('/api/stats/strip');

export const getStats = () =>
  fetchJson<StatsPageData>('/api/stats');
