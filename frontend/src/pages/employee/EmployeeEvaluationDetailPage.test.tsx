import type { ReactNode } from 'react';
import { render, screen } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { EvaluationDetail } from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { stubMatchMedia } from '../../test/browserStubs';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { EmployeeEvaluationDetailPage } from './EmployeeEvaluationDetailPage';

vi.mock('../../components/AppLayout', () => ({
  AppLayout: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}));

const evaluation = {
  id: 5,
  employeeId: 7,
  employeeFullName: 'Ana Anić',
  evaluatorFullName: 'Marko Marković',
  organizationUnitName: 'Prodaja',
  year: 2026,
  quarter: 3,
  status: 'Approved',
  version: 4,
  conditionsFulfilled: true,
  conditionsNotMetComment: null,
  controllerComment: 'Slažem se sa ocenom.',
  evaluatorComment: 'Odličan kvartal.',
  conversationAt: null,
  goalsAverage: 3,
  measuresAverage: 3,
  overallAverage: 3,
  descriptiveRatingId: null,
  descriptiveRatingName: null,
  goals: [
    {
      id: 1,
      description: 'Cilj',
      ratingLevelId: 2,
      ratingLevelValue: 3,
      ratingLevelLabel: 'Dobro',
      comment: null,
      weight: null,
      sortOrder: 0,
    },
  ],
  conditions: [],
  criteria: [],
  measures: [],
  training: null,
} as unknown as EvaluationDetail;

function renderDetail(detail: EvaluationDetail) {
  mockApiGetDeferred((path) => {
    if (path === '/api/evaluations/5') return detail;
    if (path.startsWith('/api/lookups/')) return [];
    if (path.endsWith('/status-history')) return [];
    return undefined;
  });
  const router = createMemoryRouter(
    [
      {
        path: '/employee/evaluations/:id',
        element: <EmployeeEvaluationDetailPage />,
      },
    ],
    { initialEntries: ['/employee/evaluations/5'] },
  );
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <RouterProvider router={router} />
      </ToastProvider>
    </IntlProvider>,
  );
}

describe('EmployeeEvaluationDetailPage', () => {
  beforeEach(() => {
    stubMatchMedia();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('shows a sent evaluation the way the evaluator sees it', async () => {
    renderDetail(evaluation);

    expect(await screen.findByText('Cilj')).toBeTruthy();
    expect(screen.getByText('evaluation.goalShare')).toBeTruthy();
    expect(screen.getByText('evaluation.measuresTitle')).toBeTruthy();
    expect(screen.getByText('evaluation.trainingAndDevelopment')).toBeTruthy();
    expect(screen.getByText('evaluation.summaryOverall')).toBeTruthy();
    expect(screen.getAllByText('Slažem se sa ocenom.')).toHaveLength(1);
    expect(screen.getAllByText('Odličan kvartal.')).toHaveLength(1);
  });

  it('shows only the plan while the evaluation is a draft', async () => {
    renderDetail({ ...evaluation, status: 'Draft' });

    expect(await screen.findByText('Cilj')).toBeTruthy();
    expect(screen.queryByText('evaluation.goalShare')).toBeNull();
    expect(screen.queryByText('evaluation.measuresTitle')).toBeNull();
    expect(screen.queryByText('evaluation.summaryOverall')).toBeNull();
    expect(screen.getAllByText('Odličan kvartal.')).toHaveLength(1);
  });
});
