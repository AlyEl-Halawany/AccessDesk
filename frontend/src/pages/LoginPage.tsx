import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { btnPrimary, fieldClass } from '../components/ui';

const DEMO_ACCOUNTS = [
  { label: 'Employee (Charlie)', email: 'charlie@accessdesk.dev', pass: 'Employee@123!', role: 'Employee' },
  { label: 'Manager (Alice)', email: 'alice@accessdesk.dev', pass: 'Manager@123!', role: 'Manager' },
  { label: 'App Owner (Bob)', email: 'bob@accessdesk.dev', pass: 'Owner@123!', role: 'App owner' },
  { label: 'Security Admin', email: 'admin@accessdesk.dev', pass: 'Admin@123!', role: 'Admin' },
];

export const LoginPage: React.FC = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Invalid credentials');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen grid lg:grid-cols-2 bg-paper">
      <aside className="hidden lg:flex flex-col justify-between bg-sidebar text-[#d8d5cc] p-12">
        <div>
          <div className="flex items-baseline gap-2">
            <span className="font-serif text-2xl text-white">AccessDesk</span>
            <span className="text-[10px] tracking-[0.18em] uppercase text-[#8d9286]">IGA</span>
          </div>
          <h1 className="mt-16 font-serif text-4xl leading-[1.15] text-white max-w-md">
            Govern who can access what, and for how long.
          </h1>
          <p className="mt-5 max-w-sm text-sm leading-relaxed text-[#b7b3a8]">
            Request time-bound application access, route it through manager and owner approval, and keep SoD, reviews, and audit search in one console.
          </p>
        </div>
        <ul className="space-y-3 text-sm text-[#b7b3a8]">
          <li className="flex gap-3"><span className="text-[#7dcdc4]">01</span> Two-stage approvals with SoD checks</li>
          <li className="flex gap-3"><span className="text-[#7dcdc4]">02</span> Joiner, mover, and leaver events</li>
          <li className="flex gap-3"><span className="text-[#7dcdc4]">03</span> Append-only audit search</li>
        </ul>
      </aside>

      <div className="flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-[24rem]">
          <div className="lg:hidden mb-10">
            <span className="font-serif text-2xl text-ink">AccessDesk</span>
            <p className="text-sm text-muted mt-1">Enterprise access governance</p>
          </div>

          <p className="text-[11px] tracking-[0.14em] uppercase text-muted">Sign in</p>
          <h2 className="mt-1 font-serif text-2xl text-ink">Welcome back</h2>
          <p className="mt-2 text-sm text-muted">Use a development account or enter credentials.</p>

          {error && (
            <div className="mt-5 p-3 rounded-md bg-rose-50 border border-rose-200 text-danger text-sm">{error}</div>
          )}

          <form onSubmit={handleSubmit} className="mt-6 space-y-4">
            <div>
              <label className="block text-xs font-medium text-ink-soft mb-1.5">Email</label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className={fieldClass}
                placeholder="you@accessdesk.dev"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-ink-soft mb-1.5">Password</label>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={fieldClass}
                placeholder="Enter your password"
              />
            </div>
            <button type="submit" disabled={loading} className={`${btnPrimary} w-full h-11`}>
              {loading ? 'Signing in…' : 'Sign In'}
            </button>
          </form>

          <div className="mt-8 pt-6 border-t border-line">
            <p className="text-xs font-medium text-muted mb-3">Development accounts</p>
            <div className="grid grid-cols-2 gap-2">
              {DEMO_ACCOUNTS.map((acc) => (
                <button
                  key={acc.email}
                  type="button"
                  onClick={() => {
                    setEmail(acc.email);
                    setPassword(acc.pass);
                  }}
                  className="text-left px-3 py-2.5 rounded-md border border-line bg-surface hover:border-line-strong hover:bg-paper transition-colors"
                >
                  <span className="block text-[10px] tracking-wide uppercase text-muted">{acc.role}</span>
                  <span className="block mt-0.5 text-xs font-medium text-ink">{acc.label}</span>
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
