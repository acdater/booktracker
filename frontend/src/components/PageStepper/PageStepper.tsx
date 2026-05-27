import { useState, useEffect } from 'react';

interface PageStepperProps {
  value: number;
  totalPages: number;
  onChange: (n: number) => void;
}

export function PageStepper({ value, totalPages, onChange }: PageStepperProps) {
  const [inputStr, setInputStr] = useState(String(value));

  useEffect(() => {
    setInputStr(String(value));
  }, [value]);

  function clamp(n: number) {
    return Math.min(Math.max(0, n), totalPages);
  }

  function commitStr(str: string) {
    const parsed = parseInt(str, 10);
    const clamped = clamp(isNaN(parsed) ? 0 : parsed);
    setInputStr(String(clamped));
    onChange(clamped);
  }

  return (
    <div className="flex flex-col gap-1">
      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => {
            const n = clamp(value - 1);
            setInputStr(String(n));
            onChange(n);
          }}
          disabled={value <= 0}
          className="w-11 h-11 flex-shrink-0 flex items-center justify-center rounded-button border border-warm-border text-text-primary text-[20px] disabled:opacity-40 hover:bg-warm-surface-alt transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          aria-label="Decrease page count"
        >
          −
        </button>

        <input
          type="number"
          min={0}
          max={totalPages}
          value={inputStr}
          onChange={e => setInputStr(e.target.value)}
          onBlur={e => commitStr(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') commitStr(inputStr); }}
          className="flex-1 h-11 text-center border border-warm-border rounded-input px-2 text-[18px] font-semibold text-text-primary bg-warm-surface focus:outline-none focus:ring-2 focus:ring-accent"
          aria-label="Current page"
        />

        <button
          type="button"
          onClick={() => {
            const n = clamp(value + 1);
            setInputStr(String(n));
            onChange(n);
          }}
          disabled={value >= totalPages}
          className="w-11 h-11 flex-shrink-0 flex items-center justify-center rounded-button border border-warm-border text-text-primary text-[20px] disabled:opacity-40 hover:bg-warm-surface-alt transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          aria-label="Increase page count"
        >
          +
        </button>
      </div>
      <p className="text-center text-[12px] text-text-secondary">of {totalPages} pages</p>
    </div>
  );
}
