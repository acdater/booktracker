const STATS = [
  { value: 0, label: 'books' },
  { value: 0, label: 'in progress' },
  { value: 0, label: 'finished' },
  { value: 0, label: 'pages this month' },
] as const;

export function StatsStrip() {
  return (
    <div className="bg-warm-surface-alt border-b border-warm-border px-4 sm:px-6 py-3 flex gap-6 overflow-x-auto">
      {STATS.map(({ value, label }) => (
        <div key={label} className="flex flex-col items-center min-w-[80px] shrink-0">
          <span className="text-lg font-semibold text-text-primary">{value}</span>
          <span className="text-[12px] text-text-secondary whitespace-nowrap">{label}</span>
        </div>
      ))}
    </div>
  );
}
