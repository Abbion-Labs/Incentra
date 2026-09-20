import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useIntl } from '../../i18n';
import { localizeApiError } from '../../utils/errorLocalization';

export type ToastVariant = 'success' | 'info' | 'warning' | 'error';

interface ToastItem {
  id: number;
  message: string;
  variant: ToastVariant;
}

interface ToastContextValue {
  show: (message: string, variant?: ToastVariant) => void;
  success: (message: string) => void;
  info: (message: string) => void;
  warning: (message: string) => void;
  error: (message: string) => void;
}

const TOAST_DURATION_MS: Record<ToastVariant, number> = {
  success: 4000,
  info: 4000,
  warning: 6000,
  error: 4000,
};

const toastIconProps = {
  width: 22,
  height: 22,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.75,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  'aria-hidden': true,
};

function ToastIcon({ variant }: { variant: ToastVariant }) {
  switch (variant) {
    case 'success':
      return (
        <svg {...toastIconProps}>
          <circle cx="12" cy="12" r="10" />
          <path d="M9 12l2 2 4-4" />
        </svg>
      );
    case 'error':
      return (
        <svg {...toastIconProps}>
          <circle cx="12" cy="12" r="10" />
          <path d="M15 9l-6 6" />
          <path d="M9 9l6 6" />
        </svg>
      );
    case 'warning':
      return (
        <svg {...toastIconProps}>
          <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
          <path d="M12 9v4" />
          <path d="M12 17h.01" />
        </svg>
      );
    default:
      return (
        <svg {...toastIconProps}>
          <circle cx="12" cy="12" r="10" />
          <path d="M12 16v-4" />
          <path d="M12 8h.01" />
        </svg>
      );
  }
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const { formatMessage } = useIntl();
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const timeoutsRef = useRef(new Map<number, number>());

  const dismiss = useCallback((id: number) => {
    const timeoutId = timeoutsRef.current.get(id);
    if (timeoutId != null) {
      window.clearTimeout(timeoutId);
      timeoutsRef.current.delete(id);
    }
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  useEffect(() => () => {
    timeoutsRef.current.forEach((timeoutId) => window.clearTimeout(timeoutId));
    timeoutsRef.current.clear();
  }, []);

  const show = useCallback((message: string, variant: ToastVariant = 'info') => {
    const text = variant === 'error' ? localizeApiError(message, formatMessage) : message;
    if (!text) return;

    setToasts((prev) => {
      if (prev.some((t) => t.message === text && t.variant === variant)) {
        return prev;
      }

      const id = Date.now() + Math.random();
      timeoutsRef.current.set(id, window.setTimeout(() => dismiss(id), TOAST_DURATION_MS[variant]));
      return [...prev, { id, message: text, variant }];
    });
  }, [dismiss, formatMessage]);

  const success = useCallback((message: string) => show(message, 'success'), [show]);
  const info = useCallback((message: string) => show(message, 'info'), [show]);
  const warning = useCallback((message: string) => show(message, 'warning'), [show]);
  const error = useCallback((message: string) => show(message, 'error'), [show]);

  const value = useMemo<ToastContextValue>(
    () => ({ show, success, info, warning, error }),
    [show, success, info, warning, error],
  );

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="toast-stack">
        {toasts.map((toast) => {
          const assertive = toast.variant === 'error' || toast.variant === 'warning';
          return (
            <div
              key={toast.id}
              className={`toast toast--${toast.variant}`}
              role={assertive ? 'alert' : 'status'}
              aria-live={assertive ? 'assertive' : 'polite'}
            >
              <span className="toast__icon" aria-hidden="true">
                <ToastIcon variant={toast.variant} />
              </span>
              <span className="toast__message">{toast.message}</span>
              <button
                type="button"
                className="toast__close"
                onClick={() => dismiss(toast.id)}
                aria-label={formatMessage({ id: 'buttons.close' })}
              >
                ×
              </button>
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error('useToast must be used within ToastProvider');
  }
  return ctx;
}
