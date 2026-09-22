import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../api/client';
import type {
  CompensationCalculationStatus,
  CompensationParameters,
  OrganizationUnit,
} from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { currentYear } from '../../utils/status';
import { AdminCompensation } from './AdminCompensation';

const orgUnits: OrganizationUnit[] = [
  { id: 1, name: 'Prodaja', code: null, isActive: true },
  { id: 2, name: 'Razvoj', code: null, isActive: true },
];

const paramsPath = (orgId: number) =>
  `/api/compensation-parameters?organizationUnitId=${orgId}&year=${currentYear}`;

function parameters(id: number, orgId: number): CompensationParameters {
  return {
    id,
    organizationUnitId: orgId,
    organizationUnitName: orgUnits.find((u) => u.id === orgId)!.name,
    year: currentYear,
    monetaryPool: 987654,
    currency: 'RSD',
    acceptablePerformanceRating: 3,
    upperLimitCoefficient: 0.25,
    dependencyWeight: 1,
    exponent: 1,
    allowNegativeVariable: false,
    isActive: true,
  };
}

const draftStatus = (parametersId: number): CompensationCalculationStatus => ({
  parametersId,
  totalResults: 0,
  finalizedResults: 0,
  isFinalized: false,
  lastCalculatedAt: null,
});

function renderPage(immediate: Record<string, unknown> = {}) {
  const pending = mockApiGetDeferred({
    '/api/organization-units': orgUnits,
    ...immediate,
  });
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <AdminCompensation />
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

describe('AdminCompensation', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("does not load the previous unit's parameters when its response arrives last", async () => {
    const pending = renderPage();
    await waitFor(() => expect(pending.has(paramsPath(1))).toBe(true));
    selectOrg(2);
    await waitFor(() => expect(pending.has(paramsPath(2))).toBe(true));

    await act(async () => {
      pending.get(paramsPath(2))!.resolve([]);
    });
    expect(
      screen.getByRole('button', {
        name: 'admin.compensation.createParameters',
      }),
    ).toBeTruthy();

    await act(async () => {
      pending.get(paramsPath(1))!.resolve([parameters(11, 1)]);
    });
    expect(screen.queryByDisplayValue('987654')).toBeNull();
    expect(
      screen.queryByRole('button', {
        name: 'admin.compensation.calculateCompensation',
      }),
    ).toBeNull();
  });

  it('disables calculation while parameters for a new selection are loading', async () => {
    const pending = renderPage({
      [paramsPath(1)]: [parameters(11, 1)],
      '/api/compensation-parameters/11/calculation-status': draftStatus(11),
    });
    const calculate = await screen.findByRole('button', {
      name: 'admin.compensation.calculateCompensation',
    });
    await waitFor(() => expect(calculate).toHaveProperty('disabled', false));

    selectOrg(2);
    await waitFor(() => expect(pending.has(paramsPath(2))).toBe(true));
    expect(calculate).toHaveProperty('disabled', true);
  });

  it('locks unit and year selection while saving', async () => {
    renderPage({
      [paramsPath(1)]: [parameters(11, 1)],
      '/api/compensation-parameters/11/calculation-status': draftStatus(11),
    });
    const save = await screen.findByRole('button', {
      name: 'buttons.saveChanges',
    });
    await waitFor(() => expect(save).toHaveProperty('disabled', false));
    vi.spyOn(api, 'put').mockReturnValue(new Promise(() => undefined));

    fireEvent.click(save);

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
