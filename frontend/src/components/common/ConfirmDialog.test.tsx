import { fireEvent, render, screen } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { describe, expect, it, vi } from 'vitest';
import { ConfirmDialog } from './ConfirmDialog';

function renderDialog(busy: boolean) {
  const onConfirm = vi.fn();
  const onCancel = vi.fn();
  const { container } = render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ConfirmDialog
        open
        title="Slanje"
        message="Poslati?"
        confirmLabel="Pošalji"
        cancelLabel="Otkaži"
        busy={busy}
        busyLabel="Slanje..."
        onConfirm={onConfirm}
        onCancel={onCancel}
      />
    </IntlProvider>,
  );
  const backdrop = container.querySelector('.dialog-backdrop')!;
  return { onConfirm, onCancel, backdrop };
}

describe('ConfirmDialog', () => {
  it('confirms and cancels when not busy', () => {
    const { onConfirm, onCancel, backdrop } = renderDialog(false);

    fireEvent.click(screen.getByRole('button', { name: 'Pošalji' }));
    fireEvent.click(backdrop);

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('blocks confirm, cancel and backdrop close while busy', () => {
    const { onConfirm, onCancel, backdrop } = renderDialog(true);

    const confirm = screen.getByRole('button', { name: 'Slanje...' });
    const cancel = screen.getByRole('button', { name: 'Otkaži' });
    expect(confirm).toHaveProperty('disabled', true);
    expect(cancel).toHaveProperty('disabled', true);

    fireEvent.click(confirm);
    fireEvent.click(cancel);
    fireEvent.click(backdrop);

    expect(onConfirm).not.toHaveBeenCalled();
    expect(onCancel).not.toHaveBeenCalled();
  });
});
