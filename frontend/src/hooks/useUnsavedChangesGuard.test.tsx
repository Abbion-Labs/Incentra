import { useState } from 'react';
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RouterProvider, createMemoryRouter, useNavigate } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { useUnsavedChangesGuard } from './useUnsavedChangesGuard';

const MESSAGE = 'Imate nesačuvane izmene. Napustiti stranicu?';

function Editor({ allowAfterSave }: { allowAfterSave: boolean }) {
  const navigate = useNavigate();
  const [isDirty, setIsDirty] = useState(false);
  const { allowNextNavigation } = useUnsavedChangesGuard(isDirty, MESSAGE);

  async function save() {
    await Promise.resolve();
    setIsDirty(false);
    if (allowAfterSave) allowNextNavigation();
    navigate('/dashboard');
  }

  return (
    <>
      <button onClick={() => setIsDirty(true)}>edit</button>
      <button onClick={save}>save</button>
      <button onClick={() => navigate('/dashboard')}>leave</button>
    </>
  );
}

function renderEditor(allowAfterSave: boolean) {
  const router = createMemoryRouter(
    [
      { path: '/', element: <Editor allowAfterSave={allowAfterSave} /> },
      { path: '/dashboard', element: <span>dashboard</span> },
    ],
    { initialEntries: ['/'] },
  );
  render(<RouterProvider router={router} />);
}

describe('useUnsavedChangesGuard', () => {
  it('does not warn when navigating right after a successful save', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    renderEditor(true);

    await userEvent.click(screen.getByText('edit'));
    await act(async () => {
      await userEvent.click(screen.getByText('save'));
    });

    expect(confirmSpy).not.toHaveBeenCalled();
    expect(screen.getByText('dashboard')).toBeDefined();
    confirmSpy.mockRestore();
  });

  it('still warns when leaving with unsaved changes', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    renderEditor(true);

    await userEvent.click(screen.getByText('edit'));
    await act(async () => {
      await userEvent.click(screen.getByText('leave'));
    });

    expect(confirmSpy).toHaveBeenCalledWith(MESSAGE);
    expect(screen.getByText('edit')).toBeDefined();
    confirmSpy.mockRestore();
  });

  it('asks only once per blocked navigation', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    renderEditor(true);

    await userEvent.click(screen.getByText('edit'));
    await act(async () => {
      await userEvent.click(screen.getByText('leave'));
    });

    expect(confirmSpy).toHaveBeenCalledTimes(1);
    confirmSpy.mockRestore();
  });
});
