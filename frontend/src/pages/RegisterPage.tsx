import { useState } from 'react';
import { useNavigate, Link } from 'react-router';
import { register } from '../api/authApi';
import { useAuth } from '../hooks/useAuth';
import { ApiError } from '../api/client';

interface FormData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
}

type TouchedFields = Record<keyof FormData, boolean>;

function validate(formData: FormData): Partial<Record<keyof FormData, string>> {
  const errors: Partial<Record<keyof FormData, string>> = {};
  if (!formData.email) errors.email = 'Email is required';
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) errors.email = 'Invalid email';
  if (!formData.password) errors.password = 'Password is required';
  else if (formData.password.length < 8) errors.password = 'Password must be at least 8 characters';
  if (!formData.firstName) errors.firstName = 'First name is required';
  if (!formData.lastName) errors.lastName = 'Last name is required';
  if (!formData.dateOfBirth) errors.dateOfBirth = 'Date of birth is required';
  return errors;
}

export function RegisterPage() {
  const navigate = useNavigate();
  const auth = useAuth();
  const [formData, setFormData] = useState<FormData>({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    dateOfBirth: '',
  });
  const [touched, setTouched] = useState<TouchedFields>({
    email: false,
    password: false,
    firstName: false,
    lastName: false,
    dateOfBirth: false,
  });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const errors = validate(formData);

  const handleBlur = (field: keyof FormData) => {
    setTouched((prev) => ({ ...prev, [field]: true }));
  };

  const handleChange = (field: keyof FormData) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData((prev) => ({ ...prev, [field]: e.target.value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setTouched({ email: true, password: true, firstName: true, lastName: true, dateOfBirth: true });
    if (Object.keys(errors).length > 0) return;

    setLoading(true);
    setError(null);
    try {
      const response = await register({
        email: formData.email,
        password: formData.password,
        firstName: formData.firstName,
        lastName: formData.lastName,
        dateOfBirth: `${formData.dateOfBirth}T00:00:00Z`,
      });
      auth.login(response);
      navigate('/shelf');
    } catch (err) {
      if (err instanceof ApiError && err.code === 'EMAIL_EXISTS') {
        setError('An account with this email already exists.');
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('An unexpected error occurred');
      }
    } finally {
      setLoading(false);
    }
  };

  const field = (
    id: keyof FormData,
    label: string,
    type = 'text'
  ) => (
    <div>
      <label className="block text-text-secondary text-sm mb-1" htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        type={type}
        value={formData[id]}
        onChange={handleChange(id)}
        onBlur={() => handleBlur(id)}
        className="w-full border border-warm-border rounded px-3 py-2 text-text-primary bg-warm-bg focus:outline-none focus:border-accent"
      />
      {touched[id] && errors[id] && (
        <p className="text-error text-xs mt-1">{errors[id]}</p>
      )}
    </div>
  );

  return (
    <div className="min-h-screen bg-warm-bg flex items-center justify-center px-4">
      <div className="bg-warm-surface rounded-card shadow-card-rest p-8 w-full max-w-sm">
        <h1 className="text-text-primary text-2xl font-semibold mb-6">Create account</h1>

        {error && (
          <div className="bg-error-bg text-error text-sm rounded px-3 py-2 mb-4">{error}</div>
        )}

        <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
          {field('firstName', 'First name')}
          {field('lastName', 'Last name')}
          {field('email', 'Email', 'email')}
          {field('password', 'Password', 'password')}
          {field('dateOfBirth', 'Date of birth', 'date')}

          <button
            type="submit"
            disabled={loading}
            className="bg-accent hover:bg-accent-hover text-white font-medium py-2 rounded disabled:opacity-50"
          >
            {loading ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <p className="text-text-secondary text-sm mt-4 text-center">
          Already have an account?{' '}
          <Link to="/login" className="text-accent hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
