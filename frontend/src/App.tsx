import { BrowserRouter, Routes, Route, Navigate } from 'react-router';
import { AuthProvider } from './context/AuthContext';
import { RequireAuth } from './components/RequireAuth/RequireAuth';
import { NavBar } from './components/NavBar/NavBar';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ShelfPage } from './pages/ShelfPage';
import { StatsPage } from './pages/StatsPage';

function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <NavBar />
      {children}
    </>
  );
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route
            path="/shelf"
            element={
              <RequireAuth>
                <AuthenticatedLayout>
                  <ShelfPage />
                </AuthenticatedLayout>
              </RequireAuth>
            }
          />
          <Route
            path="/stats"
            element={
              <RequireAuth>
                <AuthenticatedLayout>
                  <StatsPage />
                </AuthenticatedLayout>
              </RequireAuth>
            }
          />
          <Route path="*" element={<Navigate to="/shelf" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
