import { NavLink } from 'react-router';

export function NavBar() {
  return (
    <nav className="bg-warm-surface border-b border-warm-border">
      <div className="flex gap-6 px-4 py-3">
        <NavLink
          to="/shelf"
          className={({ isActive }) =>
            isActive ? 'text-accent font-medium' : 'text-text-secondary hover:text-text-primary'
          }
        >
          Shelf
        </NavLink>
        <NavLink
          to="/stats"
          className={({ isActive }) =>
            isActive ? 'text-accent font-medium' : 'text-text-secondary hover:text-text-primary'
          }
        >
          Stats
        </NavLink>
      </div>
    </nav>
  );
}
