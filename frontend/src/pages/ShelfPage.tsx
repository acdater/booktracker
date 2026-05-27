import { useState } from 'react';
import { useShelf } from '../hooks/useShelf';
import { BookCard } from '../components/BookCard/BookCard';
import { StatsStrip } from '../components/StatsStrip/StatsStrip';
import { EmptyState } from '../components/EmptyState/EmptyState';
import { BookForm } from '../components/BookForm/BookForm';
import { ProgressPopup } from '../components/ProgressPopup/ProgressPopup';
import { CelebrationOverlay } from '../components/CelebrationOverlay/CelebrationOverlay';
import { JournalPopup } from '../components/JournalPopup/JournalPopup';
import type { UserBook } from '../types';

export function ShelfPage() {
  const { shelf, loading, error, refetch } = useShelf();
  const [isAddBookOpen, setIsAddBookOpen] = useState(false);
  const [selectedBook, setSelectedBook] = useState<UserBook | null>(null);
  const [celebrationTitle, setCelebrationTitle] = useState('');
  const [showCelebration, setShowCelebration] = useState(false);
  const [journalBook, setJournalBook] = useState<UserBook | null>(null);

  return (
    <div className="bg-warm-bg min-h-screen">
      <StatsStrip />

      <div className="flex items-center justify-between px-4 sm:px-6 lg:px-8 pt-6 pb-2">
        <h1 className="text-[22px] font-semibold text-text-primary">My Shelf</h1>
        <button
          type="button"
          onClick={() => setIsAddBookOpen(true)}
          className="bg-accent hover:bg-accent-hover text-white px-4 py-2 rounded-button text-[15px] font-medium min-h-[44px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
        >
          + Add Book
        </button>
      </div>

      {loading && (
        <p className="text-text-secondary text-center py-12">Loading your shelf…</p>
      )}

      {error && (
        <div className="mx-4 sm:mx-6 mt-4 bg-error-bg text-error text-sm rounded px-4 py-3">{error}</div>
      )}

      {!loading && !error && shelf.length === 0 && (
        <EmptyState onAddBook={() => setIsAddBookOpen(true)} />
      )}

      {!loading && !error && shelf.length > 0 && (
        <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-3 gap-3 lg:gap-6 px-4 sm:px-6 lg:px-8 pb-6 max-w-[1200px] mx-auto">
          {shelf.map((ub) => (
            <BookCard
              key={ub.id}
              userBook={ub}
              onRefetch={refetch}
              onClick={ub.status === 'Started' ? () => setSelectedBook(ub) : undefined}
              onJournal={() => setJournalBook(ub)}
            />
          ))}
        </div>
      )}

      <BookForm
        isOpen={isAddBookOpen}
        onOpenChange={setIsAddBookOpen}
        onSuccess={() => {
          setIsAddBookOpen(false);
          refetch();
        }}
      />

      <ProgressPopup
        userBook={selectedBook}
        onClose={() => setSelectedBook(null)}
        onFinished={(title) => {
          setCelebrationTitle(title);
          setShowCelebration(true);
        }}
        onRefetch={refetch}
      />

      <CelebrationOverlay
        visible={showCelebration}
        bookTitle={celebrationTitle}
        onDismiss={() => setShowCelebration(false)}
      />

      <JournalPopup
        userBook={journalBook}
        onClose={() => setJournalBook(null)}
      />
    </div>
  );
}

