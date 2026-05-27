import { useEffect } from 'react';

interface CelebrationOverlayProps {
  visible: boolean;
  bookTitle: string;
  onDismiss: () => void;
}

export function CelebrationOverlay({ visible, bookTitle, onDismiss }: CelebrationOverlayProps) {
  useEffect(() => {
    if (!visible) return;
    const timer = setTimeout(onDismiss, 3000);
    return () => clearTimeout(timer);
  }, [visible, onDismiss]);

  return (
    <div
      role="status"
      aria-live="polite"
      onClick={onDismiss}
      className="fixed bottom-0 left-0 right-0 z-[60] cursor-pointer transition-transform duration-300"
      style={{ transform: visible ? 'translateY(0)' : 'translateY(100%)' }}
    >
      <div className="bg-celebration text-white px-6 py-5 flex items-center gap-4">
        <span className="text-[32px] leading-none select-none" aria-hidden="true">🎉</span>
        <div>
          <p className="text-[17px] font-semibold leading-tight">You finished reading!</p>
          <p className="text-[14px] opacity-90 mt-0.5 line-clamp-1">{bookTitle}</p>
        </div>
      </div>
    </div>
  );
}
