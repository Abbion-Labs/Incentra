import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../api/client';
import type {
  CompensationCalculationStatus,
  CompensationParameters,
  OrganizationUnit,
  PagedResult,
} from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { currentYear } from '../../utils/status';
import { AdminCompensationResults } from './AdminCompensationResults';

const orgUnits: OrganizationUnit[] = [
  { id: 1, name: 'Prodaja', code: null, isActive: true },
  { id: 2, name: 'Razvoj', code: null, isActive: true },
];

const metaPath = (orgId: number) =>
  `/api/compensation-parameters?organizationUnitId=${orgId}&year=${currentYear}`;

const emptyResults: PagedResult<never> = {
  items: [],
  page: 1,
  pageSize: 30,
  totalCount: 0,
  totalPages: 0,
};

function parameters(id: number, orgId: number): CompensationParameters {
  return {
    id,
    organizationUnitId: orgId,
    organizationUnitName: orgUnits.find((u) => u.id === orgId)!.name,
    year: currentYear,
    monetaryPool: 1000000,
    currency: 'EUR',
    acceptablePerformanceRating: 3,
    dependencyWeight: 1,
    exponent: 1,
    allowNegativeVariable: false,
    isActive: true,
  };
}

const draftStatus = (parametersId: number): CompensationCalculationStatus => ({
  parametersId,
  totalResults: 3,
  finalizedResults: 0,
  isFinalized: false,
  lastCalculatedAt: null,
});

function renderPage(immediate: Record<string, unknown> = {}) {
  const pending = mockApiGetDeferred((path) => {
    if (path === '/api/organization-units') return orgUnits;
    if (path.startsWith('/api/compensation-results?')) return emptyResults;
    return immediate[path];
  });
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <MemoryRouter>
          <AdminCompensationResults />
        </MemoryRouter>
      </ToastProvider>
    </IntlProvider>,
  );
  return pending;
}

function selectOrg(orgId: number) {
  fireEvent.change(screen.getByLabelText('common.organizationUnit'), {
    target: { value: String(orgId) },
  });
}

describe('AdminCompensationResults', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("does not use the previous unit's parameters when its response arrives last", async () => {
    const pending = renderPage();
    await waitFor(() => expect(pending.has(metaPath(1))).toBe(true));
    selectOrg(2);
    await waitFor(() => expect(pending.has(metaPath(2))).toBe(true));

    await act(async () => {
      pending.get(metaPath(2))!.resolve([]);
    });
    await act(async () => {
      pending.get(metaPath(1))!.resolve([parameters(11, 1)]);
    });

    expect(screen.queryByText('EUR')).toBeNull();
    expect(
      pending.has('/api/compensation-parameters/11/calculation-status'),
    ).toBe(false);
  });

  it('disables finalization while parameters for a new selection are loading', async () => {
    const pending = renderPage({
      [metaPath(1)]: [parameters(11, 1)],
      '/api/compensation-parameters/11/calculation-status': draftStatus(11),
    });
    const finalize = await screen.findByRole('button', {
      name: 'admin.compensationResults.finalizeResults',
    });
    await waitFor(() => expect(finalize).toHaveProperty('disabled', false));

    selectOrg(2);
    await waitFor(() => expect(pending.has(metaPath(2))).toBe(true));
    expect(finalize).toHaveProperty('disabled', true);
  });

  it('locks unit and year selection while finalizing', async () => {
    renderPage({
      [metaPath(1)]: [parameters(11, 1)],
      '/api/compensation-parameters/11/calculation-status': draftStatus(11),
    });
    const finalize = await screen.findByRole('button', {
      name: 'admin.compensationResults.finalizeResults',
    });
    await waitFor(() => expect(finalize).toHaveProperty('disabled', false));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    vi.spyOn(api, 'post').mockReturnValue(new Promise(() => undefined));

    fireEvent.click(finalize);

    expect(screen.getByLabelText('common.organizationUnit')).toHaveProperty(
      'disabled',
      true,
    );
    expect(screen.getByLabelText('common.year')).toHaveProperty(
      'disabled',
      true,
    );
  });
});
