import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { IntlProvider } from 'react-intl';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { UserProfile } from '../api/types';
import { flattenMessages, translationsLocale } from '../i18n';
import { ProtectedRoute } from './ProtectedRoute';

const auth = vi.hoisted(() => {
  const readers = new Set<() => void>();
  return {
    user: null as UserProfile | null,
    activeRole: null as string | null,
    selectRole: vi.fn(),
    subscribe(reader: () => void) {
      readers.add(reader);
      return () => readers.delete(reader);
    },
    /** Kao kada sesija stigne spolja: iz drugog taba ili sa servera. */
    announce() {
      readers.forEach((reader) => reader());
    },
  };
});

vi.mock('../auth/AuthContext', async () => {
  const { useEffect, useState } = await import('react');
  return {
    useAuth: () => {
      const [, setVersion] = useState(0);
      useEffect(() => auth.subscribe(() => setVersion((v) => v + 1)), []);
      return auth;
    },
  };
});

function signIn(roles: string[], activeRole: string | null) {
  auth.activeRole = activeRole;
  auth.user = {
    id: 1,
    email: 'user@local.dev',
    roles,
    activeRole,
    employeeId: 7,
    employeeFullName: null,
    employeeFirstName: null,
    employeeLastName: null,
    employeeAvatarUrl: null,
    emailNotificationsEnabled: false,
  };
}

function renderAt(path: string | { pathname: string; state: unknown }) {
  const router = createMemoryRouter(
    [
      { path: '/', element: <span>home</span> },
      { path: '/login', element: <span>login</span> },
      {
        path: '/controller',
        element: (
          <ProtectedRoute roles={['CONTROLLER', 'ADMIN']}>
            <span>controller page</span>
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <span>evaluator page</span>
          </ProtectedRoute>
        ),
      },
    ],
    { initialEntries: [path] },
  );
  render(
    <IntlProvider
      locale="sr"
      messages={flattenMessages(translationsLocale('sr'))}
      onError={() => undefined}
    >
      <RouterProvider router={router} />
    </IntlProvider>,
  );
}

describe('ProtectedRoute', () => {
  beforeEach(() => {
    auth.selectRole.mockReset();
    auth.selectRole.mockResolvedValue(undefined);
  });

  it('opens the page when the session already works in its role', () => {
    signIn(['CONTROLLER', 'ADMIN'], 'CONTROLLER');

    renderAt('/controller');

    expect(screen.getByText('controller page')).toBeTruthy();
    expect(auth.selectRole).not.toHaveBeenCalled();
  });

  it('offers the role of the page instead of switching on its own', async () => {
    signIn(['EVALUATOR', 'CONTROLLER'], 'EVALUATOR');

    renderAt('/controller');

    expect(auth.selectRole).not.toHaveBeenCalled();
    await userEvent.click(
      screen.getByRole('button', { name: 'Pređi u ulogu Kontrolor' }),
    );
    expect(auth.selectRole).toHaveBeenCalledWith('CONTROLLER');
  });

  it('says so when moving into the role fails', async () => {
    signIn(['EVALUATOR', 'CONTROLLER'], 'EVALUATOR');
    auth.selectRole.mockRejectedValue(new Error('network'));

    renderAt('/controller');
    await userEvent.click(
      screen.getByRole('button', { name: 'Pređi u ulogu Kontrolor' }),
    );

    expect(
      await screen.findByText('Prelazak u drugu ulogu nije uspeo.'),
    ).toBeTruthy();
  });

  it('switches on its own when the user asked for that role in the switch', async () => {
    signIn(['EVALUATOR', 'CONTROLLER'], 'EVALUATOR');

    renderAt({ pathname: '/controller', state: { switchingTo: 'CONTROLLER' } });

    expect(auth.selectRole).toHaveBeenCalledWith('CONTROLLER');
    expect(await screen.findByText('Prelazak u drugu ulogu...')).toBeTruthy();
  });

  it('does not take the role back when another tab moves the session', async () => {
    signIn(['EVALUATOR', 'CONTROLLER'], 'EVALUATOR');

    renderAt({ pathname: '/controller', state: { switchingTo: 'CONTROLLER' } });
    expect(auth.selectRole).toHaveBeenCalledTimes(1);

    // Prelazak je prošao, pa stranica radi.
    act(() => {
      signIn(['EVALUATOR', 'CONTROLLER'], 'CONTROLLER');
      auth.announce();
    });
    expect(await screen.findByText('controller page')).toBeTruthy();

    // Drugi tab zatim prebaci sesiju u drugu ulogu.
    act(() => {
      signIn(['EVALUATOR', 'CONTROLLER'], 'EVALUATOR');
      auth.announce();
    });

    expect(auth.selectRole).toHaveBeenCalledTimes(1);
    expect(
      screen.getByRole('button', { name: 'Pređi u ulogu Kontrolor' }),
    ).toBeTruthy();
  });

  it('leaves the admin session alone on a page it may also open', () => {
    signIn(['EVALUATOR', 'ADMIN'], 'ADMIN');

    renderAt('/evaluator');

    expect(screen.getByText('evaluator page')).toBeTruthy();
    expect(auth.selectRole).not.toHaveBeenCalled();
  });

  it('moves out of the admin role when the switch asks for another one', () => {
    signIn(['EVALUATOR', 'ADMIN'], 'ADMIN');

    // Stranica ocenjivaca prima i admina, ali korisnik je trazio ulogu
    // ocenjivaca, pa sesija mora da predje u nju.
    renderAt({ pathname: '/evaluator', state: { switchingTo: 'EVALUATOR' } });

    expect(auth.selectRole).toHaveBeenCalledWith('EVALUATOR');
  });

  it('ignores a request for a role the user does not hold', () => {
    signIn(['EVALUATOR', 'ADMIN'], 'ADMIN');

    renderAt({ pathname: '/evaluator', state: { switchingTo: 'PAYROLL' } });

    expect(auth.selectRole).not.toHaveBeenCalled();
    expect(screen.getByText('evaluator page')).toBeTruthy();
  });

  it('sends users without a role for the page home', () => {
    signIn(['EVALUATOR'], 'EVALUATOR');

    renderAt('/controller');

    expect(screen.getByText('home')).toBeTruthy();
  });
});
