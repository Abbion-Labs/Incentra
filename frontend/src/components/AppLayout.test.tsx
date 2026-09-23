import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { IntlProvider } from 'react-intl';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import type { UserProfile } from '../api/types';
import { ToastProvider } from './common/Toast';
import { flattenMessages, translationsLocale } from '../i18n';
import { LocaleProvider } from '../localization';
import { AppLayout } from './AppLayout';

const auth = vi.hoisted(() => ({
  user: null as UserProfile | null,
  activeRole: null as string | null,
  selectRole: vi.fn(),
  logout: vi.fn(),
}));

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => auth,
}));

function renderLayout(activeRole: string) {
  auth.activeRole = activeRole;
  auth.selectRole.mockResolvedValue(undefined);
  const router = createMemoryRouter(
    [
      {
        path: '/evaluator/workflow',
        element: <AppLayout title="Ocenjivanje">page</AppLayout>,
      },
      {
        path: '/controller/workflow',
        element: <span>controller home</span>,
      },
    ],
    { initialEntries: ['/evaluator/workflow'] },
  );
  render(
    <LocaleProvider>
      <IntlProvider
        locale="sr"
        messages={flattenMessages(translationsLocale('sr'))}
        onError={() => undefined}
      >
        <ToastProvider>
          <RouterProvider router={router} />
        </ToastProvider>
      </IntlProvider>
    </LocaleProvider>,
  );
  return router;
}

describe('AppLayout', () => {
  it('shows only the pages of the active role and switches to another one', async () => {
    auth.user = {
      id: 1,
      email: 'milan@local.dev',
      roles: ['EVALUATOR', 'CONTROLLER'],
      activeRole: 'EVALUATOR',
      employeeId: 7,
      employeeFullName: 'Milan Petrović',
      employeeFirstName: 'Milan',
      employeeLastName: 'Petrović',
      employeeAvatarUrl: null,
      emailNotificationsEnabled: false,
    };
    const router = renderLayout('EVALUATOR');

    const sidebar = within(screen.getByRole('navigation'));
    expect(sidebar.getByText('Ocenjivanje')).toBeTruthy();
    expect(sidebar.queryByText('Kontrola ocena')).toBeNull();

    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: 'Aktivna uloga' }),
      'CONTROLLER',
    );

    expect(await screen.findByText('controller home')).toBeTruthy();
    expect(router.state.location.pathname).toBe('/controller/workflow');
    expect(router.state.location.state).toEqual({ switchingTo: 'CONTROLLER' });
  });

  it('shows no switch to a user with a single role', () => {
    auth.user = {
      id: 2,
      email: 'eva@local.dev',
      roles: ['EVALUATOR'],
      activeRole: 'EVALUATOR',
      employeeId: 8,
      employeeFullName: 'Eva Ević',
      employeeFirstName: 'Eva',
      employeeLastName: 'Ević',
      employeeAvatarUrl: null,
      emailNotificationsEnabled: false,
    };
    renderLayout('EVALUATOR');

    expect(screen.queryByRole('combobox')).toBeNull();
    expect(screen.getByText('Ocenjivač')).toBeTruthy();
  });
});
