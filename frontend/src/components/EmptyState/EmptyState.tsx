interface EmptyStateProps {
  onAddBook?: () => void;
}

export function EmptyState({ onAddBook }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-20 px-8 text-center">
      <div className="text-5xl mb-4">📚</div>
      <h2 className="text-[22px] font-semibold text-text-primary mb-2">Your shelf is empty</h2>
      <p className="text-text-secondary text-[15px] mb-8 max-w-xs">
        Start by adding your first book to track your reading.
      </p>
      <button
        type="button"
        onClick={onAddBook}
        className="bg-accent hover:bg-accent-hover text-white px-6 py-3 rounded-button min-h-[44px] font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
      >
        Add your first book
      </button>
    </div>
  );
}
