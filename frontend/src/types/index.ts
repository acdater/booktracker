export interface User {
  userId: number;
  email: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  userId: number;
  email: string;
  firstName: string;
  lastName: string;
  token: string;
}

export interface Book {
  id: number;
  isbn: string;
  title: string;
  author: string;
  totalPages: number;
  genre: string;
  coverImageUrl: string | null;
}

export interface UserBook {
  id: number;
  userId: number;
  bookId: number;
  book: Book;
  status: 'Resting' | 'Started' | 'Finished' | 'Abandoned';
  currentPages: number;
  readingNumber: number;
  startedAt: string | null;
  finishedAt: string | null;
  lastActivityAt: string;
  readerCount: number;
}

export interface BookAction {
  id: number;
  userBookId: number;
  readingNumber: number;
  actionType: string;
  oldValue: string | null;
  newValue: string | null;
  timestamp: string;
}

export interface StatsStripData {
  totalBooks: number;
  finishedCount: number;
  startedCount: number;
  pagesThisMonth: number;
}

export interface StatsPageData {
  byStatus: {
    total: number;
    resting: number;
    started: number;
    finished: number;
    abandoned: number;
  };
  booksCompleted: {
    days7: number;
    days30: number;
    days90: number;
    days180: number;
    days270: number;
    days365: number;
  };
  pagesRead: {
    days7: number;
    days30: number;
    days90: number;
    days180: number;
    days270: number;
    days365: number;
  };
  unfinishedGenre: string | null;
}
