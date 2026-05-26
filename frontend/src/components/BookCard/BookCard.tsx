import { StatusRibbon } from '../StatusRibbon/StatusRibbon';
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
}

export function BookCard({ userBook, onClick }: BookCardProps) {
  const { book, status, currentPages, readerCount } = userBook;
  const progressPct = book.totalPages > 0 ? (currentPages / book.totalPages) * 100 : 0;

  return (
    <button
      type="button"
      onClick={onClick}
      className="bg-warm-surface rounded-card shadow-card-rest hover:shadow-card-hover active:scale-[0.98] transition-all duration-150 text-left w-full overflow-hidden focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
    >
      {/* Cover image — 2:3 aspect ratio */}
      <div className="aspect-[2/3] w-full overflow-hidden bg-warm-surface-alt flex items-center justify-center">
        {book.coverImageUrl ? (
          <img src={book.coverImageUrl} alt={book.title} className="w-full h-full object-cover" />
        ) : (
          <PlaceholderCover />
        )}
      </div>

      {/* Card body */}
      <div className="p-3 flex flex-col gap-1">
        <p className="text-[17px] font-semibold text-text-primary leading-[1.35] line-clamp-2">{book.title}</p>
        <p className="text-[15px] text-text-secondary leading-[1.5] line-clamp-1">{book.author}</p>
        <StatusRibbon status={status} />
        <p className="text-[13px] text-text-secondary mt-1">
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
    </button>
  );
}
