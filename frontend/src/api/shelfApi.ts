import { fetchJson } from './client';
import type { UserBook } from '../types';

export const getShelf = () => fetchJson<UserBook[]>('/api/shelf');

export const addToShelf = (bookId: number) =>
  fetchJson<UserBook>('/api/shelf', { method: 'POST', body: JSON.stringify({ bookId }) });
