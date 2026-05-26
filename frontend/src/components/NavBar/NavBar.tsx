import { NavLink } from 'react-router';

export function NavBar() {
  const linkClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? 'text-accent font-medium' : 'text-text-secondary hover:text-text-primary transition-colors';

  return (
    <>
      {/* Desktop top bar */}
      <nav className="hidden sm:flex bg-warm-surface border-b border-warm-border px-6 items-center gap-6 h-12">
        <NavLink to="/shelf" className={linkClass}>
          Shelf
        </NavLink>
        <NavLink to="/stats" className={linkClass}>
          Stats
        </NavLink>
      </nav>

      {/* Mobile bottom tabs */}
      <nav className="sm:hidden fixed bottom-0 left-0 right-0 bg-warm-surface border-t border-warm-border flex z-50">
        <NavLink
          to="/shelf"
          className={({ isActive }) =>
            `flex-1 flex flex-col items-center justify-center py-2 min-h-[44px] text-[12px] font-medium transition-colors ${isActive ? 'text-accent' : 'text-text-secondary'}`
          }
        >
          <span className="text-lg leading-none mb-0.5">📚</span>
          <span>Shelf</span>
        </NavLink>
        <NavLink
          to="/stats"
          className={({ isActive }) =>
            `flex-1 flex flex-col items-center justify-center py-2 min-h-[44px] text-[12px] font-medium transition-colors ${isActive ? 'text-accent' : 'text-text-secondary'}`
          }
        >
          <span className="text-lg leading-none mb-0.5">📊</span>
          <span>Stats</span>
        </NavLink>
      </nav>
    </>
  );
}

