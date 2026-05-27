import { useState, useEffect } from 'react';
import { getStats } from '../api/statsApi';
import type { StatsPageData } from '../types';

const WINDOWS = [7, 30, 90, 180, 270, 365] as const;
type WindowDay = typeof WINDOWS[number];
type PeriodKey = `days${WindowDay}`;

function SectionCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-warm-surface rounded-card shadow-card-rest p-5">
      <h2 className="text-[16px] font-semibold text-text-primary mb-3">{title}</h2>
      {children}
    </div>
  );
}

function StatRow({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="flex justify-between items-center py-1.5 border-b border-warm-border last:border-0">
      <span className="text-[14px] text-text-secondary">{label}</span>
      <span className="text-[14px] font-semibold text-text-primary">{value}</span>
    </div>
  );
}

export function StatsPage() {
  const [data, setData] = useState<StatsPageData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getStats()
      .then(setData)
      .catch(() => setError('Failed to load stats. Please try again.'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-text-secondary">Loading stats…</p>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="mx-4 sm:mx-6 mt-6 bg-error-bg text-error text-sm rounded px-4 py-3">
        {error || 'Failed to load stats.'}
      </div>
    );
  }

  const periodKey = (days: WindowDay): PeriodKey => `days${days}`;

  return (
    <div className="bg-warm-bg min-h-screen">
      <div className="px-4 sm:px-6 lg:px-8 pt-6 pb-4">
        <h1 className="text-[22px] font-semibold text-text-primary">Reading Stats</h1>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 px-4 sm:px-6 lg:px-8 pb-8 max-w-[1200px] mx-auto">
        {/* FR-19: By-status counts */}
        <SectionCard title="Library">
          <StatRow label="Total books" value={data.byStatus.total} />
          <StatRow label="Reading now" value={data.byStatus.started} />
          <StatRow label="Finished"    value={data.byStatus.finished} />
          <StatRow label="Resting"     value={data.byStatus.resting} />
          <StatRow label="Abandoned"   value={data.byStatus.abandoned} />
        </SectionCard>

        {/* FR-22: Unfinished Genre insight */}
        <SectionCard title="Reading Habit Insight">
          {data.unfinishedGenre ? (
            <p className="text-[14px] text-text-secondary">
              You tend to leave{' '}
              <span className="font-semibold text-text-primary">{data.unfinishedGenre}</span>{' '}
              books unfinished.
            </p>
          ) : (
            <p className="text-[14px] text-text-secondary">Not enough data yet.</p>
          )}
        </SectionCard>

        {/* FR-20: Books completed by rolling windows */}
        <SectionCard title="Books Completed">
          {WINDOWS.map((days) => (
            <StatRow
              key={days}
              label={`Last ${days} days`}
              value={`${data.booksCompleted[periodKey(days)]} books`}
            />
          ))}
        </SectionCard>

        {/* FR-21: Pages read by rolling windows */}
        <SectionCard title="Pages Read">
          {WINDOWS.map((days) => (
            <StatRow
              key={days}
              label={`Last ${days} days`}
              value={`${data.pagesRead[periodKey(days)]} pages`}
            />
          ))}
        </SectionCard>
      </div>
    </div>
  );
}

