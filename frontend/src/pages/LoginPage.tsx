import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { BrandMark } from '../components/BrandMark';
import { LocaleSwitcher } from '../components/LocaleSwitcher';
import { useToast } from '../hooks';
import { useIntl } from '../i18n';

const HERO_POINTS = [
  'auth.heroPointGoals',
  'auth.heroPointWorkflow',
  'auth.heroPointPayout',
] as const;

const DEMO_ACCOUNTS = [
  { labelKey: 'auth.admin', email: 'admin@local.dev', password: 'Admin123!' },
  {
    labelKey: 'auth.evaluator',
    email: 'evaluator@local.dev',
    password: 'Eval123!',
  },
  {
    labelKey: 'auth.controller',
    email: 'controller@local.dev',
    password: 'Control123!',
  },
  {
    labelKey: 'auth.employee',
    email: 'zaposleni@local.dev',
    password: 'Zaposleni123!',
  },
  {
    labelKey: 'auth.payroll',
    email: 'payroll@local.dev',
    password: 'Payroll123!',
  },
];

export function LoginPage() {
  const { formatMessage } = useIntl();
  const { login, user } = useAuth();
  const toast = useToast();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [capsLockOn, setCapsLockOn] = useState(false);

  if (user) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      toast.error(
        err instanceof ApiError
          ? err.rawMessage
          : formatMessage({ id: 'auth.loginFailed' }),
      );
    } finally {
      setSubmitting(false);
    }
  }

  function fillDemo(account: (typeof DEMO_ACCOUNTS)[number]) {
    setEmail(account.email);
    setPassword(account.password);
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
            <h1 className="login-hero__wordmark">
              {formatMessage({ id: 'navigation.brand' })}
            </h1>
          </div>
          <p className="login-hero__tagline">
            {formatMessage({ id: 'auth.heroTagline' })}
          </p>
          <ul className="login-hero__points">
            {HERO_POINTS.map((pointKey) => (
              <li key={pointKey} className="login-hero__point">
                <span className="login-hero__point-icon" aria-hidden>
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth={3}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
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
            <div className="login-card__title-row">
              <h2 className="login-card__title">
                {formatMessage({ id: 'auth.welcome' })}
              </h2>
              <LocaleSwitcher />
            </div>
            <p className="login-card__subtitle">
              {formatMessage({ id: 'auth.subtitle' })}
            </p>
          </header>

          <form className="login-form" onSubmit={handleSubmit}>
            <div className="form-row login-form__field">
              <label htmlFor="email">
                {formatMessage({ id: 'common.email' })}
              </label>
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
              <label htmlFor="password">
                {formatMessage({ id: 'common.password' })}
              </label>
              <div className="password-field">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  // Caps Lock se vidi tek iz događaja tastature ili miša.
                  onKeyUp={(e) => setCapsLockOn(e.getModifierState('CapsLock'))}
                  onKeyDown={(e) =>
                    setCapsLockOn(e.getModifierState('CapsLock'))
                  }
                  onBlur={() => setCapsLockOn(false)}
                  required
                  autoComplete="current-password"
                  placeholder="••••••••"
                  aria-describedby={capsLockOn ? 'caps-lock-hint' : undefined}
                />
                <button
                  type="button"
                  className="password-field__toggle"
                  onClick={() => setShowPassword((value) => !value)}
                  aria-pressed={showPassword}
                  aria-label={formatMessage({
                    id: showPassword
                      ? 'auth.hidePassword'
                      : 'auth.showPassword',
                  })}
                  title={formatMessage({
                    id: showPassword
                      ? 'auth.hidePassword'
                      : 'auth.showPassword',
                  })}
                >
                  <svg
                    width="18"
                    height="18"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth={1.9}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    aria-hidden
                  >
                    {showPassword ? (
                      <>
                        <path d="M3 3l18 18" />
                        <path d="M10.6 10.6a2 2 0 0 0 2.8 2.8" />
                        <path d="M9.9 5.1A10.4 10.4 0 0 1 12 5c6.5 0 10 7 10 7a17.6 17.6 0 0 1-3.2 4.2" />
                        <path d="M6.6 6.6C3.9 8.4 2 12 2 12s3.5 7 10 7a9.7 9.7 0 0 0 5.4-1.6" />
                      </>
                    ) : (
                      <>
                        <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z" />
                        <circle cx="12" cy="12" r="3" />
                      </>
                    )}
                  </svg>
                </button>
              </div>
              {capsLockOn && (
                <p
                  id="caps-lock-hint"
                  className="login-form__caps"
                  role="status"
                >
                  {formatMessage({ id: 'auth.capsLockOn' })}
                </p>
              )}
            </div>
            <button
              type="submit"
              className="btn btn-primary login-form__submit"
              disabled={submitting}
            >
              {submitting
                ? formatMessage({ id: 'buttons.loggingIn' })
                : formatMessage({ id: 'buttons.login' })}
            </button>
          </form>

          <div className="login-demo">
            <p className="login-demo__label">
              {formatMessage({ id: 'auth.demoAccounts' })}
            </p>
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
