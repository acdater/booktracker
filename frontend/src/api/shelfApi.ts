import { fetchJson } from './client';
import type { UserBook, BookAction } from '../types';

export const getShelf = () => fetchJson<UserBook[]>('/api/shelf');

export const addToShelf = (bookId: number) =>
  fetchJson<UserBook>('/api/shelf', { method: 'POST', body: JSON.stringify({ bookId }) });

export const updateStatus = (userBookId: number, status: string) =>
  fetchJson<UserBook>(`/api/shelf/${userBookId}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  });

export const reread = (userBookId: number) =>
  fetchJson<UserBook>(`/api/shelf/${userBookId}/reread`, { method: 'POST' });

export const updatePages = (userBookId: number, pages: number) =>
  fetchJson<UserBook>(`/api/shelf/${userBookId}/pages`, {
    method: 'PATCH',
    body: JSON.stringify({ pages }),
  });

export const getJournal = (userBookId: number) =>
  fetchJson<BookAction[]>(`/api/shelf/${userBookId}/journal`);
