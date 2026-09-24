import type { ReactNode } from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../api/client';
import type { EvaluationDetail } from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { stubMatchMedia } from '../../test/browserStubs';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { ControllerReviewPage } from './ControllerReviewPage';

vi.mock('../../components/AppLayout', () => ({
  AppLayout: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({ activeRole: 'CONTROLLER' }),
}));

// Ocena ponovo poslata posle vraćanja na doradu.
const evaluation = {
  id: 5,
  employeeId: 7,
  employeeFullName: 'Ana Anić',
  year: 2026,
  quarter: 3,
  status: 'Submitted',
  version: 4,
  controllerEmployeeId: 3,
  goalsPlanningComplete: true,
  goalCount: 1,
  conditionsFulfilled: true,
  conditionsNotMetComment: null,
  controllerComment: null,
  rejectionReason: 'Dopuniti ciljeve.',
  conversationAt: null,
  evaluatorComment: null,
  goalsAverage: null,
  measuresAverage: null,
  descriptiveRatingId: null,
  reviewedAt: null,
  goals: [],
  conditions: [],
  criteria: [],
  measures: [],
  training: null,
} as unknown as EvaluationDetail;

function renderReview(detail: EvaluationDetail = evaluation) {
  mockApiGetDeferred((path) => {
    if (path === '/api/evaluations/5') return detail;
    if (path.startsWith('/api/lookups/')) return [];
    if (path.endsWith('/status-history')) return [];
    return undefined;
  });
  const router = createMemoryRouter(
    [
      {
        path: '/controller/evaluations/:id',
        element: <ControllerReviewPage />,
      },
    ],
    { initialEntries: ['/controller/evaluations/5'] },
  );
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <RouterProvider router={router} />
      </ToastProvider>
    </IntlProvider>,
  );
}

describe('ControllerReviewPage', () => {
  beforeEach(() => {
    stubMatchMedia();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('shows the earlier return reason apart and approves without it', async () => {
    // Stariji podaci još nose komentar vraćanja kao komentar kontrolora.
    renderReview({ ...evaluation, controllerComment: 'Dopuniti ciljeve.' });
    const post = vi
      .spyOn(api, 'post')
      .mockImplementation(async (path: string) =>
        path.endsWith('/start-review')
          ? { ...evaluation, status: 'UnderReview', version: 5 }
          : { ...evaluation, status: 'Approved', version: 6 },
      );

    const comment = await screen.findByLabelText('controller.commentOptional');
    expect((comment as HTMLTextAreaElement).value).toBe('');
    expect(
      screen.getByText('evaluation.previousRevisionReasonLabel'),
    ).toBeTruthy();
    expect(screen.getByText('Dopuniti ciljeve.')).toBeTruthy();

    fireEvent.click(
      screen.getByRole('button', { name: 'controller.approveEvaluation' }),
    );

    await waitFor(() =>
      expect(post).toHaveBeenCalledWith('/api/evaluations/5/approve', {
        version: 5,
        controllerComment: null,
      }),
    );
  });
});
