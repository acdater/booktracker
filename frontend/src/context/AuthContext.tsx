import { createContext, useState } from 'react';
import type { AuthResponse } from '../types';

interface AuthContextValue {
  token: string | null;
  userId: string | null;
  firstName: string | null;
  lastName: string | null;
  login: (response: AuthResponse) => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'));
  const [userId, setUserId] = useState<string | null>(() => localStorage.getItem('userId'));
  const [firstName, setFirstName] = useState<string | null>(() => localStorage.getItem('firstName'));
  const [lastName, setLastName] = useState<string | null>(() => localStorage.getItem('lastName'));

  const login = (response: AuthResponse) => {
    localStorage.setItem('token', response.token);
    localStorage.setItem('userId', String(response.userId));
    localStorage.setItem('firstName', response.firstName);
    localStorage.setItem('lastName', response.lastName);
    setToken(response.token);
    setUserId(String(response.userId));
    setFirstName(response.firstName);
    setLastName(response.lastName);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    localStorage.removeItem('firstName');
    localStorage.removeItem('lastName');
    setToken(null);
    setUserId(null);
    setFirstName(null);
    setLastName(null);
  };

  return (
    <AuthContext.Provider value={{ token, userId, firstName, lastName, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
