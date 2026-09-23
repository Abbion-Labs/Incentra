import type { ReactNode } from 'react';
import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { RouterProvider, createMemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, api } from '../../api/client';
import type { EvaluationDetail } from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { stubMatchMedia } from '../../test/browserStubs';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { EvaluationEditorPage } from './EvaluationEditorPage';

vi.mock('../../components/AppLayout', () => ({
  AppLayout: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}));

const auth = vi.hoisted(() => ({ activeRole: 'EVALUATOR' as string | null }));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({ activeRole: auth.activeRole }),
}));

// Uslovi nisu ispunjeni + komentar: ocena se može poslati bez ocenjivanja mera.
const evaluation = {
  id: 5,
  employeeId: 7,
  employeeFullName: 'Ana Anić',
  year: 2026,
  quarter: 3,
  status: 'Draft',
  version: 4,
  controllerEmployeeId: 3,
  goalsPlanningComplete: true,
  goalCount: 1,
  conditionsFulfilled: false,
  conditionsNotMetComment: 'Uslovi nisu ispunjeni',
  controllerComment: null,
  conversationAt: null,
  evaluatorComment: null,
  goalsAverage: null,
  measuresAverage: null,
  descriptiveRatingId: null,
  reviewedAt: null,
  rejectionReason: null,
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
  conditions: [{ id: 1, description: 'Uslov', sortOrder: 0 }],
  criteria: [{ id: 1, description: 'Kriterijum', sortOrder: 0 }],
  measures: [],
  training: null,
} as unknown as EvaluationDetail;

function renderEditor(detail: EvaluationDetail = evaluation) {
  mockApiGetDeferred((path) => {
    if (path === '/api/evaluations/5') return detail;
    if (path.startsWith('/api/lookups/')) return [];
    if (path.endsWith('/status-history')) return [];
    return undefined;
  });
  const router = createMemoryRouter(
    [{ path: '/evaluator/evaluations/:id', element: <EvaluationEditorPage /> }],
    { initialEntries: ['/evaluator/evaluations/5'] },
  );
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <RouterProvider router={router} />
      </ToastProvider>
    </IntlProvider>,
  );
}

describe('EvaluationEditorPage', () => {
  beforeEach(() => {
    stubMatchMedia();
    auth.activeRole = 'EVALUATOR';
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('submits only once when the confirm button is double-clicked', async () => {
    renderEditor();
    const put = vi
      .spyOn(api, 'put')
      .mockReturnValue(new Promise(() => undefined));

    const submit = await screen.findByRole('button', {
      name: 'buttons.submitToController',
    });
    // Dugme se iscrta pre nego što se ocena potpuno učita; klik na još
    // onemogućeno dugme ne otvara dijalog.
    await waitFor(() => expect(submit).toHaveProperty('disabled', false));
    fireEvent.click(submit);
    const dialog = await screen.findByRole('dialog');
    const confirm = within(dialog).getByRole('button', {
      name: 'buttons.submit',
    });
    fireEvent.click(confirm);
    fireEvent.click(confirm);

    await waitFor(() => expect(put).toHaveBeenCalled());
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 0));
    });
    expect(put).toHaveBeenCalledTimes(1);
    expect(confirm).toHaveProperty('disabled', true);
    expect(confirm.textContent).toBe('buttons.submitting');
  });

  it('finishes the evaluation for an evaluator without a controller', async () => {
    renderEditor({ ...evaluation, controllerEmployeeId: null });

    expect(
      await screen.findByRole('button', { name: 'buttons.submitFinal' }),
    ).toBeTruthy();
    expect(
      screen.queryByRole('button', { name: 'buttons.submitToController' }),
    ).toBeNull();
  });

  it('shows the draft read-only to an admin', async () => {
    auth.activeRole = 'ADMIN';
    renderEditor();

    await screen.findByText('Cilj');
    expect(screen.queryByRole('button', { name: 'buttons.save' })).toBeNull();
    expect(
      screen.queryByRole('button', { name: 'buttons.submitToController' }),
    ).toBeNull();
    expect(
      screen.queryByLabelText('evaluation.conditionsNotMetCommentLabel'),
    ).toBeNull();
  });

  it('locks the rating fields while the evaluation is saving', async () => {
    renderEditor();
    let resolvePut: (value: EvaluationDetail) => void = () => undefined;
    const put = vi.spyOn(api, 'put').mockReturnValue(
      new Promise((resolve) => {
        resolvePut = resolve as typeof resolvePut;
      }),
    );

    const comment = await screen.findByLabelText(
      'evaluation.conditionsNotMetCommentLabel',
    );
    expect(comment.matches(':disabled')).toBe(false);

    fireEvent.click(screen.getByRole('button', { name: 'buttons.save' }));
    await waitFor(() => expect(put).toHaveBeenCalled());
    expect(comment.matches(':disabled')).toBe(true);

    await act(async () => {
      resolvePut(evaluation);
    });
    expect(
      screen
        .getByLabelText('evaluation.conditionsNotMetCommentLabel')
        .matches(':disabled'),
    ).toBe(false);
  });

  it('saves with the version the page was loaded with', async () => {
    renderEditor();
    const put = vi.spyOn(api, 'put').mockResolvedValue(evaluation);

    await screen.findByLabelText('evaluation.conditionsNotMetCommentLabel');
    fireEvent.click(screen.getByRole('button', { name: 'buttons.save' }));

    await waitFor(() => expect(put).toHaveBeenCalled());
    expect(put.mock.calls[0][1]).toMatchObject({ version: 4 });
    // Ocena se pre čuvanja ne učitava ponovo, jer bi to prikrilo tuđu izmenu.
    const evaluationLoads = vi
      .mocked(api.get)
      .mock.calls.filter(([path]) => path === '/api/evaluations/5');
    expect(evaluationLoads).toHaveLength(1);
  });

  it('shows the new state when someone else saved in the meantime', async () => {
    renderEditor();
    vi.spyOn(api, 'put').mockRejectedValue(
      new ApiError('vn-0024', 400, 'vn-0024', 'vn-0024'),
    );

    await screen.findByLabelText('evaluation.conditionsNotMetCommentLabel');
    fireEvent.click(screen.getByRole('button', { name: 'buttons.save' }));

    expect(
      await screen.findByText('errors.evaluationChangedMeanwhile'),
    ).toBeTruthy();
    await waitFor(() =>
      expect(
        vi
          .mocked(api.get)
          .mock.calls.filter(([path]) => path === '/api/evaluations/5'),
      ).toHaveLength(2),
    );
  });
});
