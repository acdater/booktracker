import { useState, useEffect, useCallback } from 'react';
import { getShelf } from '../api/shelfApi';
import { ApiError } from '../api/client';
import type { UserBook } from '../types';

export function useShelf() {
  const [shelf, setShelf] = useState<UserBook[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [fetchCount, setFetchCount] = useState(0);

  useEffect(() => {
    setLoading(true);
    setError(null);
    getShelf()
      .then(setShelf)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load shelf'))
      .finally(() => setLoading(false));
  }, [fetchCount]);

  const refetch = useCallback(() => setFetchCount(n => n + 1), []);

  return { shelf, loading, error, refetch };
}
