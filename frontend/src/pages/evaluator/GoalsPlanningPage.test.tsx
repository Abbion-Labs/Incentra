import type { ReactNode } from 'react';
import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Employee, EvaluationDetail } from '../../api/types';
import { api } from '../../api/client';
import { ToastProvider } from '../../components/common/Toast';
import { stubMatchMedia } from '../../test/browserStubs';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { GoalsPlanningPage } from './GoalsPlanningPage';

vi.mock('../../components/AppLayout', () => ({
  AppLayout: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}));

const auth = vi.hoisted(() => ({ activeRole: 'EVALUATOR' as string | null }));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({ activeRole: auth.activeRole }),
}));

const evaluation = {
  id: 5,
  employeeId: 7,
  employeeFullName: 'Ana Anić',
  year: 2026,
  quarter: 3,
  status: 'Draft',
  version: 1,
  goalsPlanningComplete: false,
  goalCount: 1,
  controllerComment: null,
  conversationAt: null,
  evaluatorComment: null,
  goals: [{ id: 1, description: 'Cilj', sortOrder: 0 }],
  conditions: [{ id: 1, description: 'Uslov', sortOrder: 0 }],
  criteria: [{ id: 1, description: 'Kriterijum', sortOrder: 0 }],
  measures: [],
  training: null,
} as unknown as EvaluationDetail;

const employee = {
  id: 7,
  fullName: 'Ana Anić',
  jobPositionName: 'Analitičar',
} as unknown as Employee;

const previousQuarterPath =
  '/api/evaluator/evaluations?pageSize=5&year=2026&quarter=2&employeeId=7';

const previousEvaluation = {
  ...evaluation,
  id: 4,
  quarter: 2,
  status: 'Approved',
  goalsPlanningComplete: true,
  descriptiveRatingName: 'Ističe se',
  goals: [{ id: 2, description: 'Prošli cilj', sortOrder: 0, weight: 100 }],
  conditions: [{ id: 2, description: 'Prošli uslov', sortOrder: 0 }],
  criteria: [{ id: 2, description: 'Prošli kriterijum', sortOrder: 0 }],
} as unknown as EvaluationDetail;

function renderPage(responses: (path: string) => unknown) {
  const pending = mockApiGetDeferred((path) => {
    const response = responses(path);
    if (response !== undefined) return response;
    if (path === '/api/evaluations/5') return evaluation;
    if (path === '/api/employees/7') return employee;
    if (path.startsWith('/api/lookups/')) return [];
    if (path.endsWith('/status-history')) return [];
    return undefined;
  });
  const router = createMemoryRouter(
    [
      {
        path: '/evaluator/goals/evaluations/:id',
        element: <GoalsPlanningPage />,
      },
      { path: '/evaluator/goals', element: <div /> },
    ],
    { initialEntries: ['/evaluator/goals/evaluations/5'] },
  );
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <RouterProvider router={router} />
      </ToastProvider>
    </IntlProvider>,
  );
  return pending;
}

describe('GoalsPlanningPage', () => {
  beforeEach(() => {
    stubMatchMedia();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('disables saving while goals are being copied from the previous quarter', async () => {
    const pending = mockApiGetDeferred((path) => {
      if (path === '/api/evaluations/5') return evaluation;
      if (path === '/api/employees/7') return employee;
      if (path.startsWith('/api/lookups/')) return [];
      if (path.endsWith('/status-history')) return [];
      return undefined;
    });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const router = createMemoryRouter(
      [
        {
          path: '/evaluator/goals/evaluations/:id',
          element: <GoalsPlanningPage />,
        },
      ],
      { initialEntries: ['/evaluator/goals/evaluations/5'] },
    );
    render(
      <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
        <ToastProvider>
          <RouterProvider router={router} />
        </ToastProvider>
      </IntlProvider>,
    );

    const save = await screen.findByRole('button', {
      name: 'evaluation.setGoals',
    });
    await waitFor(() => expect(save).toHaveProperty('disabled', false));

    fireEvent.click(
      screen.getByRole('button', {
        name: 'evaluation.planning.copyPreviousPlanLink',
      }),
    );
    await waitFor(() => expect(pending.has(previousQuarterPath)).toBe(true));

    expect(save).toHaveProperty('disabled', true);
  });

  it('saves the plan only when the goal weights make 100%, and sends them', async () => {
    mockApiGetDeferred((path) => {
      if (path === '/api/evaluations/5') return evaluation;
      if (path === '/api/employees/7') return employee;
      if (path.startsWith('/api/lookups/')) return [];
      if (path.endsWith('/status-history')) return [];
      return undefined;
    });
    const put = vi
      .spyOn(api, 'put')
      .mockResolvedValue({ ...evaluation, version: 2 } as never);
    const router = createMemoryRouter(
      [
        {
          path: '/evaluator/goals/evaluations/:id',
          element: <GoalsPlanningPage />,
        },
        { path: '/evaluator/goals', element: <div /> },
      ],
      { initialEntries: ['/evaluator/goals/evaluations/5'] },
    );
    render(
      <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
        <ToastProvider>
          <RouterProvider router={router} />
        </ToastProvider>
      </IntlProvider>,
    );

    const save = await screen.findByRole('button', {
      name: 'evaluation.setGoals',
    });
    const weight = screen.getByRole('spinbutton', {
      name: 'evaluation.goalWeightLabel',
    });
    expect(weight).toHaveProperty('value', '100');

    fireEvent.change(weight, { target: { value: '60' } });
    expect(save).toHaveProperty('disabled', true);

    fireEvent.change(weight, { target: { value: '100' } });
    await waitFor(() => expect(save).toHaveProperty('disabled', false));
    fireEvent.click(save);

    await waitFor(() => expect(put).toHaveBeenCalled());
    const body = put.mock.calls[0][1] as {
      goals: { description: string; weight: number | null }[];
    };
    expect(body.goals).toEqual([
      expect.objectContaining({ description: 'Cilj', weight: 100 }),
    ]);
  });

  it('says why the goals cannot be set while the plan is incomplete', async () => {
    renderPage((path) =>
      path === '/api/evaluations/5'
        ? { ...evaluation, criteria: [] }
        : undefined,
    );

    const save = await screen.findByRole('button', {
      name: 'evaluation.setGoals',
    });
    expect(save).toHaveProperty('disabled', true);
    expect(screen.getByText('evaluation.planning.incompleteHint')).toBeTruthy();

    fireEvent.change(
      screen.getByPlaceholderText('evaluation.planning.criterionPlaceholder'),
      { target: { value: 'Kriterijum' } },
    );

    await waitFor(() => expect(save).toHaveProperty('disabled', false));
    expect(screen.queryByText('evaluation.planning.incompleteHint')).toBeNull();
  });

  it('fills an empty form with the previous quarter plan without loading it again', async () => {
    renderPage((path) => {
      if (path === '/api/evaluations/5') {
        return { ...evaluation, goals: [], conditions: [], criteria: [] };
      }
      if (path === previousQuarterPath) return { items: [previousEvaluation] };
      if (path === '/api/evaluations/4') return previousEvaluation;
      return undefined;
    });
    const confirm = vi.spyOn(window, 'confirm');

    expect(
      await screen.findByText('evaluation.planning.previousRating'),
    ).toBeTruthy();
    const get = vi.mocked(api.get);
    const requests = get.mock.calls.length;

    fireEvent.click(
      screen.getByRole('button', {
        name: 'evaluation.planning.copyPreviousPlanLink',
      }),
    );

    await waitFor(() =>
      expect(screen.getByDisplayValue('Prošli kriterijum')).toBeTruthy(),
    );
    expect(screen.getByDisplayValue('Prošli cilj')).toBeTruthy();
    expect(screen.getByDisplayValue('Prošli uslov')).toBeTruthy();
    expect(get.mock.calls.length).toBe(requests);
    // Prazna forma nema šta da izgubi, pa se ne traži potvrda.
    expect(confirm).not.toHaveBeenCalled();
  });

  it('shows the previous quarter plan when hovering over the copy button', async () => {
    renderPage((path) => {
      if (path === previousQuarterPath) return { items: [previousEvaluation] };
      if (path === '/api/evaluations/4') return previousEvaluation;
      return undefined;
    });

    await screen.findByText('evaluation.planning.previousRating');
    const copy = screen.getByRole('button', {
      name: 'evaluation.planning.copyPreviousPlanLink',
    });
    expect(screen.queryByRole('tooltip')).toBeNull();

    fireEvent.mouseEnter(copy);

    const preview = screen.getByRole('tooltip');
    expect(preview.textContent).toContain('Prošli cilj');
    expect(preview.textContent).toContain('100%');
    expect(preview.textContent).toContain('Prošli uslov');
    expect(preview.textContent).toContain('Prošli kriterijum');

    fireEvent.mouseLeave(copy);
    expect(screen.queryByRole('tooltip')).toBeNull();
  });

  it('offers nothing to copy when the previous quarter has no plan', async () => {
    renderPage((path) => {
      if (path === '/api/evaluations/5') {
        return { ...evaluation, goals: [], conditions: [], criteria: [] };
      }
      if (path === previousQuarterPath) return { items: [] };
      return undefined;
    });

    await screen.findByRole('button', { name: 'evaluation.setGoals' });
    await waitFor(() =>
      expect(vi.mocked(api.get)).toHaveBeenCalledWith(previousQuarterPath),
    );
    await act(async () => {});

    expect(
      screen.queryByRole('button', {
        name: 'evaluation.planning.copyPreviousPlanLink',
      }),
    ).toBeNull();
  });
});
