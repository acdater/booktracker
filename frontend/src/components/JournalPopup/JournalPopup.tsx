import { useEffect, useState } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { VisuallyHidden } from '@radix-ui/react-visually-hidden';
import * as shelfApi from '../../api/shelfApi';
import { ApiError } from '../../api/client';
import type { UserBook, BookAction } from '../../types';

interface JournalPopupProps {
  userBook: UserBook | null;
  onClose: () => void;
}

function formatTimestamp(ts: string): string {
  return new Date(ts).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

function actionLabel(actionType: string): string {
  if (actionType === 'StatusChange') return 'Status Change';
  if (actionType === 'PageUpdate') return 'Page Update';
  return actionType;
}

export function JournalPopup({ userBook, onClose }: JournalPopupProps) {
  const [entries, setEntries] = useState<BookAction[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!userBook) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    setEntries([]);
    shelfApi.getJournal(userBook.id)
      .then(data => { if (!cancelled) setEntries(data); })
      .catch(err => {
        if (!cancelled)
          setError(err instanceof ApiError ? err.message : 'Failed to load journal.');
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [userBook]);

  // Group consecutive entries by readingNumber (API returns newest-first,
  // so readingNumber groups appear in descending order)
  const groups: { readingNumber: number; items: BookAction[] }[] = [];
  for (const entry of entries) {
    const last = groups[groups.length - 1];
    if (last && last.readingNumber === entry.readingNumber) {
      last.items.push(entry);
    } else {
      groups.push({ readingNumber: entry.readingNumber, items: [entry] });
    }
  }

  return (
    <Dialog.Root
      open={userBook !== null}
      onOpenChange={open => { if (!open) onClose(); }}
    >
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/40 z-40" />
        <Dialog.Content
          className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50
            bg-warm-surface rounded-popup shadow-popup
            w-[92vw] max-w-md max-h-[80vh] flex flex-col
            focus:outline-none"
        >
          <VisuallyHidden>
            <Dialog.Title>{userBook?.book.title} Journal</Dialog.Title>
          </VisuallyHidden>

          {/* Header */}
          <div className="flex items-center justify-between px-5 pt-5 pb-3 border-b border-warm-border shrink-0">
            <div className="min-w-0 mr-3">
              <h2 className="text-[17px] font-semibold text-text-primary leading-tight">
                Reading Journal
              </h2>
              <p className="text-[13px] text-text-secondary line-clamp-1 mt-0.5">
                {userBook?.book.title}
              </p>
            </div>
            <Dialog.Close
              className="w-8 h-8 shrink-0 flex items-center justify-center rounded-full
                text-text-secondary hover:bg-warm-surface-alt
                focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              aria-label="Close journal"
            >
              ✕
            </Dialog.Close>
          </div>

          {/* Scrollable body */}
          <div className="overflow-y-auto flex-1 px-5 py-4">
            {loading && (
              <p className="text-center text-text-secondary text-[14px] py-8">Loading…</p>
            )}
            {error && (
              <p className="text-center text-error text-[14px] py-8">{error}</p>
            )}
            {!loading && !error && entries.length === 0 && (
              <p className="text-center text-text-secondary text-[14px] py-8">
                No journal entries yet.
              </p>
            )}
            {!loading && !error && groups.map(group => (
              <div key={group.readingNumber} className="mb-5">
                <p className="text-[12px] font-semibold text-text-secondary uppercase tracking-wide mb-2">
                  Read #{group.readingNumber}
                </p>
                <div className="flex flex-col gap-2">
                  {group.items.map((entry, idx) => (
                    <div
                      key={idx}
                      className="bg-warm-surface-alt rounded-input px-3 py-2.5"
                    >
                      <div className="flex items-center justify-between gap-2 mb-1">
                        <span className="text-[13px] font-medium text-text-primary">
                          {actionLabel(entry.actionType)}
                        </span>
                        <span className="text-[11px] text-text-secondary shrink-0">
                          {formatTimestamp(entry.timestamp)}
                        </span>
                      </div>
                      <p className="text-[12px] text-text-secondary">
                        {entry.oldValue ?? '—'} → {entry.newValue ?? '—'}
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
