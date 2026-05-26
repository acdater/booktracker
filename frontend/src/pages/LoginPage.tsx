import { useState } from 'react';
import { useNavigate, Link } from 'react-router';
import { login } from '../api/authApi';
import { useAuth } from '../hooks/useAuth';
import { ApiError } from '../api/client';

export function LoginPage() {
  const navigate = useNavigate();
  const auth = useAuth();
  const [formData, setFormData] = useState({ email: '', password: '' });
  const [touched, setTouched] = useState({ email: false, password: false });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const emailError = touched.email && !formData.email ? 'Email is required' : null;
  const passwordError = touched.password && !formData.password ? 'Password is required' : null;

  const handleBlur = (field: keyof typeof touched) => {
    setTouched((prev) => ({ ...prev, [field]: true }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setTouched({ email: true, password: true });
    if (!formData.email || !formData.password) return;

    setLoading(true);
    setError(null);
    try {
      const response = await login({ email: formData.email, password: formData.password });
      auth.login(response);
      navigate('/shelf');
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('An unexpected error occurred');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-warm-bg flex items-center justify-center px-4">
      <div className="bg-warm-surface rounded-card shadow-card-rest p-8 w-full max-w-sm">
        <h1 className="text-text-primary text-2xl font-semibold mb-6">Sign in</h1>

        {error && (
          <div className="bg-error-bg text-error text-sm rounded px-3 py-2 mb-4">{error}</div>
        )}

        <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
          <div>
            <label className="block text-text-secondary text-sm mb-1" htmlFor="email">
              Email
            </label>
            <input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => setFormData((p) => ({ ...p, email: e.target.value }))}
              onBlur={() => handleBlur('email')}
              className="w-full border border-warm-border rounded px-3 py-2 text-text-primary bg-warm-bg focus:outline-none focus:border-accent"
            />
            {emailError && <p className="text-error text-xs mt-1">{emailError}</p>}
          </div>

          <div>
            <label className="block text-text-secondary text-sm mb-1" htmlFor="password">
              Password
            </label>
            <input
              id="password"
              type="password"
              value={formData.password}
              onChange={(e) => setFormData((p) => ({ ...p, password: e.target.value }))}
              onBlur={() => handleBlur('password')}
              className="w-full border border-warm-border rounded px-3 py-2 text-text-primary bg-warm-bg focus:outline-none focus:border-accent"
            />
            {passwordError && <p className="text-error text-xs mt-1">{passwordError}</p>}
          </div>

          <button
            type="submit"
            disabled={loading}
            className="bg-accent hover:bg-accent-hover text-white font-medium py-2 rounded disabled:opacity-50"
          >
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        <p className="text-text-secondary text-sm mt-4 text-center">
          Don't have an account?{' '}
          <Link to="/register" className="text-accent hover:underline">
            Register
          </Link>
        </p>
      </div>
    </div>
  );
}
