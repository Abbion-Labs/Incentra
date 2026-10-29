import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { AppIntlProvider } from './components/AppIntlProvider';
import { ToastProvider } from './components/common/Toast';
import { LocaleProvider } from './localization';
import { router } from './router';
import './styles/index.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <LocaleProvider>
      <AppIntlProvider>
        <AuthProvider>
          <ToastProvider>
            {/* Navigacija kao tranzicija: dok stiže kod sledeće stranice, ostaje prikazana trenutna. */}
            <RouterProvider
              router={router}
              future={{ v7_startTransition: true }}
            />
          </ToastProvider>
        </AuthProvider>
      </AppIntlProvider>
    </LocaleProvider>
  </StrictMode>,
);
