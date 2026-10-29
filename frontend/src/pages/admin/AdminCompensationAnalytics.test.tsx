import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { CompensationAnalytics, OrganizationUnit } from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { currentYear } from '../../utils/status';
import { AdminCompensationAnalytics } from './AdminCompensationAnalytics';

const orgUnits: OrganizationUnit[] = [
  { id: 1, name: 'Prodaja', code: null, isActive: true },
];

const allOrgPath = `/api/compensation-results/analytics?year=${currentYear}&chartType=shareDistribution`;
const salesPath = `${allOrgPath}&organizationUnitId=1`;

function analytics(counts: number[]): CompensationAnalytics {
  return {
    chartType: 'shareDistribution',
    year: currentYear,
    currency: 'RSD',
    organizationUnitName: null,
    valueFormat: 'count',
    buckets: counts.map((count, i) => ({ label: `b${i}`, count })),
    series: [],
  };
}

async function renderAndSwitchToSales() {
  const pending = mockApiGetDeferred({
    '/api/organization-units': orgUnits,
  });
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <MemoryRouter>
          <AdminCompensationAnalytics />
        </MemoryRouter>
      </ToastProvider>
    </IntlProvider>,
  );

  await waitFor(() => expect(pending.has(allOrgPath)).toBe(true));
  await screen.findByRole('option', { name: 'Prodaja' });
  fireEvent.change(screen.getByLabelText('evaluation.orgUnitShort'), {
    target: { value: '1' },
  });
  await waitFor(() => expect(pending.has(salesPath)).toBe(true));
  return pending;
}

describe('AdminCompensationAnalytics', () => {
  beforeEach(() => {
    // Grafikoni mere kontejner, a jsdom nema ResizeObserver.
    vi.stubGlobal(
      'ResizeObserver',
      class {
        observe() {}
        unobserve() {}
        disconnect() {}
      },
    );
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('keeps the chart for the current filters when a stale response arrives last', async () => {
    const pending = await renderAndSwitchToSales();

    await act(async () => {
      pending.get(salesPath)!.resolve(analytics([0, 0]));
    });
    expect(screen.getByText('common.filtersNoData')).toBeTruthy();

    await act(async () => {
      pending.get(allOrgPath)!.resolve(analytics([3, 5]));
    });
    expect(screen.getByText('common.filtersNoData')).toBeTruthy();
  });

  it('stays loading until the latest request settles and ignores stale errors', async () => {
    const pending = await renderAndSwitchToSales();

    await act(async () => {
      pending.get(allOrgPath)!.reject(new Error('stale failure'));
    });
    expect(screen.getByText('common.loading')).toBeTruthy();
    expect(screen.queryByText('stale failure')).toBeNull();

    await act(async () => {
      pending.get(salesPath)!.resolve(analytics([0, 0]));
    });
    expect(screen.queryByText('common.loading')).toBeNull();
    expect(screen.getByText('common.filtersNoData')).toBeTruthy();
  });
});
