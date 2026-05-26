export interface User {
  userId: number;
  email: string;
  firstName: string;
}

export interface AuthResponse {
  userId: number;
  email: string;
  firstName: string;
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
  totalUserBooks: number;
  finishedCount: number;
  startedCount: number;
  pagesThisMonth: number;
}

export interface StatsPageData {
  byStatus: {
    resting: number;
    started: number;
    finished: number;
    abandoned: number;
    total: number;
  };
  completionsBy: { days: number; count: number }[];
  pagesBy: { days: number; pages: number }[];
  unfinishedGenre: string | null;
}
