import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../api/client';
import type { AdminUser } from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { AdminUsers } from './AdminUsers';

const users: AdminUser[] = [
  {
    id: 1,
    email: 'ana@example.com',
    roles: ['ADMIN'],
    isActive: true,
    employeeId: null,
    employeeFullName: null,
  },
  {
    id: 2,
    email: 'bora@example.com',
    roles: ['ADMIN'],
    isActive: true,
    employeeId: null,
    employeeFullName: null,
  },
];

function rowOf(email: string) {
  return screen.getByText(email).closest('tr')!;
}

describe('AdminUsers', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('ignores selecting another user while the current one is saving', async () => {
    vi.spyOn(window, 'scrollTo').mockImplementation(() => undefined);
    mockApiGetDeferred((path) => {
      if (path === '/api/users') return users;
      if (path === '/api/evaluator-settings') return [];
      if (path.startsWith('/api/employees?')) {
        return {
          items: [],
          page: 1,
          pageSize: 100,
          totalCount: 0,
          totalPages: 0,
        };
      }
      return undefined;
    });
    render(
      <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
        <ToastProvider>
          <MemoryRouter>
            <AdminUsers />
          </MemoryRouter>
        </ToastProvider>
      </IntlProvider>,
    );

    await screen.findByText('ana@example.com');
    fireEvent.click(
      within(rowOf('ana@example.com')).getByRole('button', {
        name: 'buttons.edit',
      }),
    );
    const email = screen.getByLabelText('common.email');
    await waitFor(() =>
      expect(email).toHaveProperty('value', 'ana@example.com'),
    );
    vi.spyOn(api, 'put').mockReturnValue(new Promise(() => undefined));

    fireEvent.click(
      screen.getByRole('button', { name: 'buttons.saveChanges' }),
    );
    const boraEdit = within(rowOf('bora@example.com')).getByRole('button', {
      name: 'buttons.edit',
    });
    expect(boraEdit).toHaveProperty('disabled', true);
    fireEvent.click(boraEdit);
    fireEvent.click(rowOf('bora@example.com'));

    expect(email).toHaveProperty('value', 'ana@example.com');
  });
});
