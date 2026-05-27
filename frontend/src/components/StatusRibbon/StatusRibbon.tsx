const STATUS_COLORS = {
  Resting: '#8C98A8',
  Started: '#C4874A',
  Finished: '#6B8F71',
  Abandoned: '#B07880',
} as const;

interface StatusRibbonProps {
  status: 'Resting' | 'Started' | 'Finished' | 'Abandoned';
}

export function StatusRibbon({ status }: StatusRibbonProps) {
  return (
    <span
      className="inline-flex items-center px-2 py-0.5 rounded text-white text-[11px] font-medium self-start"
      style={{ backgroundColor: STATUS_COLORS[status], transition: 'background-color 0.3s ease' }}
    >
      {status}
    </span>
  );
}
