import { act, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { IntlProvider } from 'react-intl';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { EmployeeSalary, PagedResult } from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { AdminSalaries } from './AdminSalaries';

function salary(
  employeeId: number,
  employeeFullName: string,
  points: number,
): EmployeeSalary {
  return {
    id: employeeId * 100 + points,
    employeeId,
    employeeFullName,
    organizationUnitName: 'Prodaja',
    points,
    salaryPerPoint: 1000,
    currency: 'RSD',
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    isCurrent: true,
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

const rows = [salary(1, 'Ana Anić', 10), salary(2, 'Bora Borić', 20)];
const list: PagedResult<EmployeeSalary> = {
  items: rows,
  page: 1,
  pageSize: 30,
  totalCount: rows.length,
  totalPages: 1,
};

const anaHistoryPath = '/api/employee-salaries/1/history';
const boraHistoryPath = '/api/employee-salaries/2/history';

function rowOf(name: string) {
  return within(screen.getByText(name).closest('tr')!);
}

async function openAnaThenBoraHistory() {
  const pending = mockApiGetDeferred({
    '/api/employee-salaries?page=1&pageSize=30': list,
  });
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <AdminSalaries />
      </ToastProvider>
    </IntlProvider>,
  );

  const user = userEvent.setup();
  await screen.findByText('Ana Anić');
  await user.click(rowOf('Ana Anić').getByRole('button', { name: 'Istorija' }));
  await user.click(
    rowOf('Bora Borić').getByRole('button', { name: 'Istorija' }),
  );
  expect(pending.has(anaHistoryPath)).toBe(true);
  expect(pending.has(boraHistoryPath)).toBe(true);
  return pending;
}

describe('AdminSalaries history', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("does not show the previous employee's history when its response arrives last", async () => {
    const pending = await openAnaThenBoraHistory();

    await act(async () => {
      pending.get(boraHistoryPath)!.resolve([salary(2, 'Bora Borić', 222)]);
    });
    expect(screen.getByText('222')).toBeTruthy();

    await act(async () => {
      pending.get(anaHistoryPath)!.resolve([salary(1, 'Ana Anić', 111)]);
    });
    expect(screen.getByText('222')).toBeTruthy();
    expect(screen.queryByText('111')).toBeNull();
  });

  it('keeps the open history when a stale request fails', async () => {
    const pending = await openAnaThenBoraHistory();

    await act(async () => {
      pending.get(anaHistoryPath)!.reject(new Error('stale failure'));
    });
    expect(screen.queryByText('stale failure')).toBeNull();
    expect(
      rowOf('Bora Borić').getByRole('button', { name: 'Sakrij' }),
    ).toBeTruthy();
    expect(screen.getByText('admin.salaries.loadingHistory')).toBeTruthy();

    await act(async () => {
      pending.get(boraHistoryPath)!.resolve([salary(2, 'Bora Borić', 222)]);
    });
    expect(screen.getByText('222')).toBeTruthy();
  });
});
