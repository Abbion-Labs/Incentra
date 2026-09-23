import type { ReactNode } from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Employee, EvaluationDetail } from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { stubMatchMedia } from '../../test/browserStubs';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { GoalsPlanningPage } from './GoalsPlanningPage';

vi.mock('../../components/AppLayout', () => ({
  AppLayout: ({ children }: { children: ReactNode }) => <div>{children}</div>,
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
        name: 'evaluation.copyFromPreviousQuarter',
      }),
    );
    await waitFor(() => expect(pending.has(previousQuarterPath)).toBe(true));

    expect(save).toHaveProperty('disabled', true);
  });
});
