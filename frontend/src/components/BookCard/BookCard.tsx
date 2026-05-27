import { useState } from 'react';
import { StatusRibbon } from '../StatusRibbon/StatusRibbon';
import * as shelfApi from '../../api/shelfApi';
import { ApiError } from '../../api/client';
import type { UserBook } from '../../types';

function PlaceholderCover() {
  return (
    <div className="w-full h-full flex items-center justify-center bg-warm-surface-alt">
      <svg width="48" height="48" viewBox="0 0 48 48" fill="none" aria-hidden="true">
        <rect x="8" y="6" width="32" height="36" rx="3" fill="#E2D9CE" />
        <rect x="12" y="14" width="24" height="2" rx="1" fill="#ADA49A" />
        <rect x="12" y="20" width="18" height="2" rx="1" fill="#ADA49A" />
        <rect x="12" y="26" width="20" height="2" rx="1" fill="#ADA49A" />
      </svg>
    </div>
  );
}

interface BookCardProps {
  userBook: UserBook;
  onClick?: () => void;
  onRefetch?: () => void;
}

export function BookCard({ userBook, onClick, onRefetch }: BookCardProps) {
  const { book, status, currentPages, readerCount } = userBook;
  const progressPct = book.totalPages > 0 ? (currentPages / book.totalPages) * 100 : 0;
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  async function handleAction() {
    setActionError(null);
    setActionLoading(true);
    try {
      if (status === 'Resting') {
        await shelfApi.updateStatus(userBook.id, 'Started');
      } else if (status === 'Started') {
        await shelfApi.updateStatus(userBook.id, 'Abandoned');
      } else {
        await shelfApi.reread(userBook.id);
      }
      onRefetch?.();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setActionLoading(false);
    }
  }

  const actionLabel =
    status === 'Resting' ? 'Start Reading' :
    status === 'Started' ? 'Abandon' :
    'Read Again';

  const isAbandonAction = status === 'Started';

  return (
    <article className="bg-warm-surface rounded-card shadow-card-rest hover:shadow-card-hover transition-shadow duration-150 overflow-hidden">
      {/* Clickable upper section — for Story 3.5 progress popup */}
      <div
        onClick={onClick}
        className={onClick ? 'cursor-pointer active:scale-[0.98] transition-transform duration-150' : ''}
      >
        {/* Cover image — fixed height to keep cards compact */}
        <div className="h-24 w-full overflow-hidden bg-warm-surface-alt flex items-center justify-center">
          {book.coverImageUrl ? (
            <img src={book.coverImageUrl} alt={book.title} className="w-full h-full object-cover" />
          ) : (
            <PlaceholderCover />
          )}
        </div>

        {/* Card body */}
        <div className="p-2 flex flex-col gap-0.5">
          <p className="text-[14px] font-semibold text-text-primary leading-[1.35] line-clamp-2">{book.title}</p>
          <p className="text-[12px] text-text-secondary leading-[1.4] line-clamp-1">{book.author}</p>
          <StatusRibbon status={status} />
          <p className="text-[11px] text-text-secondary mt-0.5">
            👥 {readerCount} {readerCount === 1 ? 'reader' : 'readers'}
          </p>
        </div>

        {/* Progress strip */}
        <div
          className="bg-warm-border h-1 w-full overflow-hidden"
          role="progressbar"
          aria-label={`Page ${currentPages} of ${book.totalPages}`}
          aria-valuenow={currentPages}
          aria-valuemin={0}
          aria-valuemax={book.totalPages}
        >
          <div
            className="bg-accent h-full transition-all duration-300"
            style={{ width: `${progressPct}%` }}
          />
        </div>
      </div>

      {/* Action area */}
      <div className="px-2 pb-2 pt-1">
        <button
          type="button"
          onClick={handleAction}
          disabled={actionLoading}
          className={[
            'w-full py-2 rounded-button text-[14px] font-medium min-h-[44px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 disabled:opacity-50',
            isAbandonAction
              ? 'text-text-secondary bg-warm-surface-alt hover:bg-warm-border'
              : 'bg-accent text-white hover:bg-accent-hover',
          ].join(' ')}
        >
          {actionLoading ? '…' : actionLabel}
        </button>
        {actionError && (
          <p className="text-error text-[13px] mt-1">{actionError}</p>
        )}
      </div>
    </article>
  );
}
