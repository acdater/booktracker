import { fetchJson } from './client';
import type { Book } from '../types';

export interface CreateBookDto {
  isbn: string;
  title: string;
  author: string;
  totalPages: number;
  genre: string;
  coverImageUrl?: string | null;
}

export const lookupISBN = (isbn: string) =>
  fetchJson<Book | null>(`/api/books/${encodeURIComponent(isbn)}`);

export const createBook = (dto: CreateBookDto) =>
  fetchJson<Book>('/api/books', { method: 'POST', body: JSON.stringify(dto) });
