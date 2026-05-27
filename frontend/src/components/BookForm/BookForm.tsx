import { useState, useCallback } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { VisuallyHidden } from '@radix-ui/react-visually-hidden';
import { lookupISBN, createBook } from '../../api/booksApi';
import { addToShelf } from '../../api/shelfApi';
import { ApiError } from '../../api/client';

const GENRES = [
  'Fiction',
  'Non-Fiction',
  'Mystery',
  'Science Fiction',
  'Fantasy',
  'Romance',
  'Biography & Memoir',
  'History',
  'Self-Help',
  'Other',
] as const;

type Step = 'isbn' | 'form';

interface FormData {
  isbn: string;
  title: string;
  author: string;
  totalPages: string;
  genre: string;
  coverImageUrl: string;
}

interface TouchedFields {
  isbn: boolean;
  title: boolean;
  author: boolean;
  totalPages: boolean;
  genre: boolean;
}

interface BookFormProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

const EMPTY_FORM: FormData = { isbn: '', title: '', author: '', totalPages: '', genre: '', coverImageUrl: '' };
const UNTOUCHED: TouchedFields = { isbn: false, title: false, author: false, totalPages: false, genre: false };

function getErrors(form: FormData) {
  return {
    isbn: !form.isbn.trim() ? 'ISBN is required' : '',
    title: !form.title.trim() ? 'Title is required' : '',
    author: !form.author.trim() ? 'Author is required' : '',
    totalPages:
      !form.totalPages || !Number.isInteger(+form.totalPages) || +form.totalPages < 1 || +form.totalPages > 10000
        ? 'Must be a number between 1 and 10,000'
        : '',
    genre: !form.genre ? 'Please select a genre' : '',
  };
}

export function BookForm({ isOpen, onOpenChange, onSuccess }: BookFormProps) {
  const [step, setStep] = useState<Step>('isbn');
  const [isbnInput, setIsbnInput] = useState('');
  const [isLookingUp, setIsLookingUp] = useState(false);
  const [isbnError, setIsbnError] = useState('');

  const [formData, setFormData] = useState<FormData>(EMPTY_FORM);
  const [touched, setTouched] = useState<TouchedFields>(UNTOUCHED);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [apiError, setApiError] = useState('');

  const resetState = useCallback(() => {
    setStep('isbn');
    setIsbnInput('');
    setIsLookingUp(false);
    setIsbnError('');
    setFormData(EMPTY_FORM);
    setTouched(UNTOUCHED);
    setIsSubmitting(false);
    setApiError('');
  }, []);

  const handleOpenChange = (open: boolean) => {
    if (!open) resetState();
    onOpenChange(open);
  };

  const handleLookup = async () => {
    const trimmed = isbnInput.trim();
    if (!trimmed) {
      setIsbnError('Please enter an ISBN');
      return;
    }
    setIsbnError('');
    setIsLookingUp(true);
    try {
      const book = await lookupISBN(trimmed);
      if (book) {
        setFormData({
          isbn: trimmed,
          title: book.title,
          author: book.author,
          totalPages: book.totalPages > 0 ? String(book.totalPages) : '',
          genre: book.genre || '',
          coverImageUrl: book.coverImageUrl ?? '',
        });
      } else {
        setFormData({ ...EMPTY_FORM, isbn: trimmed });
      }
      setStep('form');
    } catch {
      // Network or API error — still allow manual entry
      setFormData({ ...EMPTY_FORM, isbn: trimmed });
      setStep('form');
    } finally {
      setIsLookingUp(false);
    }
  };

  const handleFieldChange = (field: keyof FormData, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    setApiError('');
  };

  const handleBlur = (field: keyof TouchedFields) => {
    setTouched(prev => ({ ...prev, [field]: true }));
  };

  const handleSubmit = async () => {
    // Touch all fields to surface any remaining errors
    setTouched({ isbn: true, title: true, author: true, totalPages: true, genre: true });
    const errors = getErrors(formData);
    if (Object.values(errors).some(e => e)) return;

    setIsSubmitting(true);
    setApiError('');
    try {
      const book = await createBook({
        isbn: formData.isbn.trim(),
        title: formData.title.trim(),
        author: formData.author.trim(),
        totalPages: parseInt(formData.totalPages, 10),
        genre: formData.genre,
        coverImageUrl: formData.coverImageUrl || null,
      });
      await addToShelf(book.id);
      onSuccess();
    } catch (err) {
      setApiError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const errors = getErrors(formData);
  const isFormValid = Object.values(errors).every(e => e === '');

  const inputClass = (field: keyof TouchedFields) =>
    `w-full border rounded-input px-3 py-2 text-[15px] text-text-primary bg-warm-surface placeholder:text-text-disabled focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-0 ${
      touched[field] && errors[field] ? 'border-error' : 'border-warm-border'
    }`;

  return (
    <Dialog.Root open={isOpen} onOpenChange={handleOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/40 z-50 animate-in fade-in-0" />
        <Dialog.Content
          className="fixed left-0 right-0 bottom-0 sm:left-1/2 sm:top-1/2 sm:bottom-auto sm:-translate-x-1/2 sm:-translate-y-1/2 sm:w-full sm:max-w-md bg-warm-surface rounded-t-popup sm:rounded-popup shadow-popup z-50 p-6 focus:outline-none"
          aria-describedby={undefined}
        >
          <Dialog.Title asChild>
            <VisuallyHidden>Add a Book</VisuallyHidden>
          </Dialog.Title>

          {/* Header */}
          <div className="flex items-center justify-between mb-5">
            <h2 className="text-[18px] font-semibold text-text-primary">
              {step === 'isbn' ? 'Add a Book' : 'Book Details'}
            </h2>
            <Dialog.Close className="text-text-secondary hover:text-text-primary transition-colors p-1 rounded focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent">
              <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                <path d="M5 5L15 15M15 5L5 15" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
              </svg>
              <span className="sr-only">Close</span>
            </Dialog.Close>
          </div>

          {/* ISBN Step */}
          {step === 'isbn' && (
            <div className="flex flex-col gap-4">
              <div>
                <label htmlFor="isbn-input" className="block text-[13px] font-medium text-text-secondary mb-1">
                  ISBN (10 or 13 digits)
                </label>
                <input
                  id="isbn-input"
                  type="text"
                  value={isbnInput}
                  onChange={e => { setIsbnInput(e.target.value); setIsbnError(''); }}
                  onKeyDown={e => { if (e.key === 'Enter') handleLookup(); }}
                  placeholder="e.g. 9780141199078"
                  className={`w-full border rounded-input px-3 py-2 text-[15px] text-text-primary bg-warm-surface placeholder:text-text-disabled focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-0 ${
                    isbnError ? 'border-error' : 'border-warm-border'
                  }`}
                  autoFocus
                />
                {isbnError && <p className="text-error text-[13px] mt-1">{isbnError}</p>}
              </div>

              <button
                type="button"
                onClick={handleLookup}
                disabled={isLookingUp}
                className="w-full bg-accent hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed text-white py-3 rounded-button text-[15px] font-medium transition-colors min-h-[44px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
              >
                {isLookingUp ? 'Looking up…' : 'Look Up'}
              </button>

              <button
                type="button"
                onClick={() => { setFormData({ ...EMPTY_FORM, isbn: isbnInput.trim() }); setStep('form'); }}
                className="text-[14px] text-text-secondary hover:text-text-primary transition-colors text-center min-h-[44px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent rounded"
              >
                Skip — enter details manually
              </button>
            </div>
          )}

          {/* Book Form Step */}
          {step === 'form' && (
            <div className="flex flex-col gap-4">
              {/* API error banner */}
              {apiError && (
                <div role="alert" className="bg-error-bg text-error text-sm rounded px-4 py-3">
                  {apiError}
                </div>
              )}

              {/* ISBN */}
              <div>
                <label htmlFor="book-isbn-form" className="block text-[13px] font-medium text-text-secondary mb-1">
                  ISBN <span className="text-error">*</span>
                </label>
                <input
                  id="book-isbn-form"
                  type="text"
                  value={formData.isbn}
                  onChange={e => handleFieldChange('isbn', e.target.value)}
                  onBlur={() => handleBlur('isbn')}
                  placeholder="e.g. 9780141199078"
                  className={inputClass('isbn')}
                />
                {touched.isbn && errors.isbn && (
                  <p className="text-error text-[13px] mt-1">{errors.isbn}</p>
                )}
              </div>

              {/* Title */}
              <div>
                <label htmlFor="book-title" className="block text-[13px] font-medium text-text-secondary mb-1">
                  Title <span className="text-error">*</span>
                </label>
                <input
                  id="book-title"
                  type="text"
                  value={formData.title}
                  onChange={e => handleFieldChange('title', e.target.value)}
                  onBlur={() => handleBlur('title')}
                  placeholder="Book title"
                  className={inputClass('title')}
                />
                {touched.title && errors.title && (
                  <p className="text-error text-[13px] mt-1">{errors.title}</p>
                )}
              </div>

              {/* Author */}
              <div>
                <label htmlFor="book-author" className="block text-[13px] font-medium text-text-secondary mb-1">
                  Author <span className="text-error">*</span>
                </label>
                <input
                  id="book-author"
                  type="text"
                  value={formData.author}
                  onChange={e => handleFieldChange('author', e.target.value)}
                  onBlur={() => handleBlur('author')}
                  placeholder="Author name"
                  className={inputClass('author')}
                />
                {touched.author && errors.author && (
                  <p className="text-error text-[13px] mt-1">{errors.author}</p>
                )}
              </div>

              {/* Total Pages */}
              <div>
                <label htmlFor="book-pages" className="block text-[13px] font-medium text-text-secondary mb-1">
                  Total Pages <span className="text-error">*</span>
                </label>
                <input
                  id="book-pages"
                  type="number"
                  min={1}
                  max={10000}
                  value={formData.totalPages}
                  onChange={e => handleFieldChange('totalPages', e.target.value)}
                  onBlur={() => handleBlur('totalPages')}
                  placeholder="e.g. 320"
                  className={inputClass('totalPages')}
                />
                {touched.totalPages && errors.totalPages && (
                  <p className="text-error text-[13px] mt-1">{errors.totalPages}</p>
                )}
              </div>

              {/* Genre */}
              <div>
                <label htmlFor="book-genre" className="block text-[13px] font-medium text-text-secondary mb-1">
                  Genre <span className="text-error">*</span>
                </label>
                <select
                  id="book-genre"
                  value={formData.genre}
                  onChange={e => handleFieldChange('genre', e.target.value)}
                  onBlur={() => handleBlur('genre')}
                  className={`w-full border rounded-input px-3 py-2 text-[15px] bg-warm-surface focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-0 ${
                    !formData.genre ? 'text-text-disabled' : 'text-text-primary'
                  } ${touched.genre && errors.genre ? 'border-error' : 'border-warm-border'}`}
                >
                  <option value="" disabled>Select a genre…</option>
                  {GENRES.map(g => (
                    <option key={g} value={g}>{g}</option>
                  ))}
                </select>
                {touched.genre && errors.genre && (
                  <p className="text-error text-[13px] mt-1">{errors.genre}</p>
                )}
              </div>

              {/* Cover URL (read-only if pre-filled) */}
              {formData.coverImageUrl && (
                <div>
                  <label className="block text-[13px] font-medium text-text-secondary mb-1">
                    Cover Image
                  </label>
                  <p className="text-[13px] text-text-secondary truncate bg-warm-surface-alt rounded-input px-3 py-2 border border-warm-border">
                    {formData.coverImageUrl}
                  </p>
                </div>
              )}

              {/* Actions */}
              <div className="flex gap-3 pt-1">
                <button
                  type="button"
                  onClick={() => { setStep('isbn'); setApiError(''); }}
                  className="flex-1 border border-warm-border text-text-secondary hover:text-text-primary hover:border-text-secondary py-3 rounded-button text-[15px] font-medium transition-colors min-h-[44px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
                >
                  Back
                </button>
                <button
                  type="button"
                  onClick={handleSubmit}
                  disabled={!isFormValid || isSubmitting}
                  className="flex-[2] bg-accent hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed text-white py-3 rounded-button text-[15px] font-medium transition-colors min-h-[44px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
                >
                  {isSubmitting ? 'Adding…' : 'Add to Shelf'}
                </button>
              </div>
            </div>
          )}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
