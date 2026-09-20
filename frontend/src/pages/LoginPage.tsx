import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { BrandMark } from '../components/BrandMark';
import { useIntl } from '../i18n';
import { localizeApiError } from '../utils/errorLocalization';

const HERO_POINTS = ['auth.heroPointGoals', 'auth.heroPointWorkflow', 'auth.heroPointPayout'] as const;

const DEMO_ACCOUNTS = [
  { labelKey: 'auth.admin', email: 'admin@local.dev', password: 'Admin123!' },
  { labelKey: 'auth.evaluator', email: 'evaluator@local.dev', password: 'Eval123!' },
  { labelKey: 'auth.controller', email: 'controller@local.dev', password: 'Control123!' },
  { labelKey: 'auth.payroll', email: 'payroll@local.dev', password: 'Payroll123!' },
];

export function LoginPage() {
  const { formatMessage } = useIntl();
  const { login, user } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (user) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setSubmitting(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      setError(err instanceof ApiError
        ? localizeApiError(err.rawMessage, formatMessage)
        : formatMessage({ id: 'auth.loginFailed' }));
    } finally {
      setSubmitting(false);
    }
  }

  function fillDemo(account: (typeof DEMO_ACCOUNTS)[number]) {
    setEmail(account.email);
    setPassword(account.password);
    setError('');
  }

  return (
    <div className="login-page">
      <aside className="login-page__hero">
        <div className="login-hero__orb login-hero__orb--1" aria-hidden />
        <div className="login-hero__orb login-hero__orb--2" aria-hidden />
        <div className="login-hero__orb login-hero__orb--3" aria-hidden />
        <div className="login-hero__content">
          <div className="login-hero__lockup">
            <span className="app-brand__mark login-hero__mark">
              <BrandMark size={32} />
            </span>
            <h1 className="login-hero__wordmark">{formatMessage({ id: 'navigation.brand' })}</h1>
          </div>
          <p className="login-hero__tagline">{formatMessage({ id: 'auth.heroTagline' })}</p>
          <ul className="login-hero__points">
            {HERO_POINTS.map((pointKey) => (
              <li key={pointKey} className="login-hero__point">
                <span className="login-hero__point-icon" aria-hidden>
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={3} strokeLinecap="round" strokeLinejoin="round">
                    <path d="M4 12.5l5 5L20 7" />
                  </svg>
                </span>
                {formatMessage({ id: pointKey })}
              </li>
            ))}
          </ul>
        </div>
      </aside>

      <main className="login-page__main">
        <div className="login-card">
          <header className="login-card__header">
            <h2 className="login-card__title">{formatMessage({ id: 'auth.welcome' })}</h2>
            <p className="login-card__subtitle">{formatMessage({ id: 'auth.subtitle' })}</p>
          </header>

          {error && <div className="alert alert-error login-card__alert">{error}</div>}

          <form className="login-form" onSubmit={handleSubmit}>
            <div className="form-row login-form__field">
              <label htmlFor="email">{formatMessage({ id: 'common.email' })}</label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="username"
                placeholder={formatMessage({ id: 'auth.placeholderEmail' })}
              />
            </div>
            <div className="form-row login-form__field">
              <label htmlFor="password">{formatMessage({ id: 'common.password' })}</label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
                placeholder="••••••••"
              />
            </div>
            <button type="submit" className="btn btn-primary login-form__submit" disabled={submitting}>
              {submitting ? formatMessage({ id: 'buttons.loggingIn' }) : formatMessage({ id: 'buttons.login' })}
            </button>
          </form>

          <div className="login-demo">
            <p className="login-demo__label">{formatMessage({ id: 'auth.demoAccounts' })}</p>
            <div className="login-demo__chips">
              {DEMO_ACCOUNTS.map((account) => (
                <button
                  key={account.email}
                  type="button"
                  className="login-demo__chip"
                  onClick={() => fillDemo(account)}
                >
                  {formatMessage({ id: account.labelKey as never })}
                </button>
              ))}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
