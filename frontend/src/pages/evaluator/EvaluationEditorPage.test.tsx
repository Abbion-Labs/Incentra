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
import { api } from '../../api/client';
import type { EvaluationDetail } from '../../api/types';
import { ToastProvider } from '../../components/common/Toast';
import { stubMatchMedia } from '../../test/browserStubs';
import { mockApiGetDeferred } from '../../test/deferredApi';
import { EvaluationEditorPage } from './EvaluationEditorPage';

vi.mock('../../components/AppLayout', () => ({
  AppLayout: ({ children }: { children: ReactNode }) => <div>{children}</div>,
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

function renderEditor() {
  mockApiGetDeferred((path) => {
    if (path === '/api/evaluations/5') return evaluation;
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
});
