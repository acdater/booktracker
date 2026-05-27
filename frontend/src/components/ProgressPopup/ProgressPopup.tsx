import { useState, useEffect } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { VisuallyHidden } from '@radix-ui/react-visually-hidden';
import { PageStepper } from '../PageStepper/PageStepper';
import * as shelfApi from '../../api/shelfApi';
import { ApiError } from '../../api/client';
import type { UserBook } from '../../types';

interface ProgressPopupProps {
  userBook: UserBook | null;
  onClose: () => void;
  onFinished: (title: string) => void;
  onRefetch: () => void;
}

export function ProgressPopup({ userBook, onClose, onFinished, onRefetch }: ProgressPopupProps) {
  const [pageValue, setPageValue] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (userBook) {
      setPageValue(userBook.currentPages);
      setError(null);
    }
  }, [userBook]);

  async function handleUpdate() {
    if (!userBook) return;
    setError(null);
    setIsSubmitting(true);
    try {
      const updated = await shelfApi.updatePages(userBook.id, pageValue);
      onClose();
      onRefetch();
      if (updated.status === 'Finished') {
        onFinished(userBook.book.title);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  }

  const isDirty = userBook !== null && pageValue !== userBook.currentPages;

  return (
    <Dialog.Root open={userBook !== null} onOpenChange={open => { if (!open) onClose(); }}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/40 z-50 animate-in fade-in-0" />
        <Dialog.Content
          className="fixed left-0 right-0 bottom-0 sm:left-1/2 sm:top-1/2 sm:bottom-auto sm:-translate-x-1/2 sm:-translate-y-1/2 sm:w-full sm:max-w-md bg-warm-surface rounded-t-popup sm:rounded-popup shadow-popup z-50 p-6 focus:outline-none"
          aria-describedby={undefined}
        >
          <Dialog.Title asChild>
            <VisuallyHidden>Update reading progress</VisuallyHidden>
          </Dialog.Title>

          {userBook && (
            <>
              {/* Header row */}
              <div className="flex items-start justify-between mb-5 gap-3">
                <div className="flex items-start gap-3">
                  {/* Cover thumbnail */}
                  <div className="w-10 h-[60px] flex-shrink-0 overflow-hidden rounded bg-warm-surface-alt">
                    {userBook.book.coverImageUrl ? (
                      <img
                        src={userBook.book.coverImageUrl}
                        alt={userBook.book.title}
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center">
                        <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                          <rect x="3" y="2" width="14" height="16" rx="1.5" fill="#E2D9CE" />
                        </svg>
                      </div>
                    )}
                  </div>

                  <div>
                    <h2 className="text-[17px] font-semibold text-text-primary leading-[1.35] line-clamp-2">
                      {userBook.book.title}
                    </h2>
                    <p className="text-[13px] text-text-secondary mt-0.5">{userBook.book.author}</p>
                  </div>
                </div>

                <Dialog.Close className="text-text-secondary hover:text-text-primary transition-colors p-1 rounded flex-shrink-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent">
                  <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                    <path d="M5 5L15 15M15 5L5 15" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
                  </svg>
                  <span className="sr-only">Close</span>
                </Dialog.Close>
              </div>

              {/* Page stepper */}
              <div className="mb-5">
                <p className="text-[13px] font-medium text-text-secondary mb-3">Current page</p>
                <PageStepper
                  value={pageValue}
                  totalPages={userBook.book.totalPages}
                  onChange={setPageValue}
                />
              </div>

              {/* Error */}
              {error && (
                <div role="alert" className="bg-error-bg text-error text-sm rounded px-4 py-3 mb-4">
                  {error}
                </div>
              )}

              {/* Update button */}
              <button
                type="button"
                onClick={handleUpdate}
                disabled={!isDirty || isSubmitting}
                className="w-full bg-accent hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed text-white py-3 rounded-button text-[15px] font-medium transition-colors min-h-[44px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
              >
                {isSubmitting ? 'Updating…' : 'Update'}
              </button>
            </>
          )}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
