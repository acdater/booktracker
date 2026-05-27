import { useState, useEffect } from 'react';
import { getStrip } from '../../api/statsApi';
import type { StatsStripData } from '../../types';

export function StatsStrip() {
  const [data, setData] = useState<StatsStripData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getStrip()
      .then(setData)
      .catch(() => {/* stay null — placeholders remain */})
      .finally(() => setLoading(false));
  }, []);

  const stats = [
    { value: data?.totalBooks,     label: 'books' },
    { value: data?.startedCount,   label: 'reading' },
    { value: data?.finishedCount,  label: 'finished' },
    { value: data?.pagesThisMonth, label: 'pages this month' },
  ];

  return (
    <div className="bg-warm-surface-alt border-b border-warm-border px-4 sm:px-6 py-3 flex gap-6 overflow-x-auto">
      {stats.map(({ value, label }) => (
        <div key={label} className="flex flex-col items-center min-w-[80px] shrink-0">
          <span className="text-lg font-semibold text-text-primary">
            {loading || value === undefined ? '—' : value}
          </span>
          <span className="text-[12px] text-text-secondary whitespace-nowrap">{label}</span>
        </div>
      ))}
    </div>
  );
}
