export class ApiError extends Error {
  readonly code: string;

  constructor(message: string, code: string) {
    super(message);
    this.name = 'ApiError';
    this.code = code;
  }
}

export async function fetchJson<T>(url: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('token');
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...((options.headers as Record<string, string>) ?? {}),
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const res = await fetch(url, { ...options, headers });

  if (!res.ok) {
    const body = await res
      .json()
      .catch(() => ({ error: 'An unexpected error occurred.', code: 'UNKNOWN_ERROR' }));
    throw new ApiError(
      body.error ?? 'An unexpected error occurred.',
      body.code ?? 'UNKNOWN_ERROR'
    );
  }

  if (res.status === 204) return undefined as T;

  return res.json() as Promise<T>;
}
