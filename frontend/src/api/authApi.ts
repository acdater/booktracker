import { fetchJson } from './client';
import type { AuthResponse } from '../types';

export interface RegisterData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string; // ISO 8601: "YYYY-MM-DDT00:00:00Z"
}

export interface LoginData {
  email: string;
  password: string;
}

export const register = (data: RegisterData) =>
  fetchJson<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const login = (data: LoginData) =>
  fetchJson<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(data),
  });
