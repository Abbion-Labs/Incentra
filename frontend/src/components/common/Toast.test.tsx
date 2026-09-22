import { act, fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { IntlProvider } from 'react-intl';
import { describe, expect, it, vi } from 'vitest';
import { flattenMessages, translationsLocale } from '../../i18n';
import { ToastProvider, useToast } from './Toast';

function Harness() {
  const toast = useToast();
  return (
    <div>
      <button type="button" onClick={() => toast.success('Saved')}>
        success
      </button>
      <button type="button" onClick={() => toast.info('Copied')}>
        info
      </button>
      <button type="button" onClick={() => toast.warning('Incomplete')}>
        warning
      </button>
      <button type="button" onClick={() => toast.error('vn-0016')}>
        error
      </button>
      <button type="button" onClick={() => toast.error('vn-0016')}>
        error-again
      </button>
    </div>
  );
}

function renderToasts() {
  return render(
    <IntlProvider
      locale="en-US"
      messages={flattenMessages(translationsLocale('en'))}
    >
      <ToastProvider>
        <Harness />
      </ToastProvider>
    </IntlProvider>,
  );
}

describe('useToast', () => {
  it('shows success, info, warning and localized error toasts', async () => {
    renderToasts();

    await userEvent.click(screen.getByText('success'));
    await userEvent.click(screen.getByText('info'));
    await userEvent.click(screen.getByText('warning'));
    await userEvent.click(screen.getByText('error'));

    expect(screen.getByText('Saved')).toBeDefined();
    expect(screen.getByText('Copied')).toBeDefined();
    expect(screen.getByText('Incomplete')).toBeDefined();
    expect(screen.getByText('Invalid email or password.')).toBeDefined();
    expect(screen.queryByText('vn-0016')).toBeNull();
    expect(document.querySelectorAll('.toast__icon svg')).toHaveLength(4);
  });

  it('does not duplicate the same error message', async () => {
    renderToasts();

    await userEvent.click(screen.getByText('error'));
    await userEvent.click(screen.getByText('error-again'));

    expect(screen.getAllByText('Invalid email or password.')).toHaveLength(1);
  });

  it('dismisses a toast when the close button is clicked', async () => {
    renderToasts();

    await userEvent.click(screen.getByText('error'));
    expect(screen.getByText('Invalid email or password.')).toBeDefined();

    await userEvent.click(screen.getByRole('button', { name: 'Close' }));
    expect(screen.queryByText('Invalid email or password.')).toBeNull();
  });

  it('auto-dismisses success and error toasts', () => {
    vi.useFakeTimers();
    renderToasts();

    fireEvent.click(screen.getByText('success'));
    fireEvent.click(screen.getByText('error'));
    expect(screen.getByText('Saved')).toBeDefined();
    expect(screen.getByText('Invalid email or password.')).toBeDefined();

    act(() => {
      vi.advanceTimersByTime(4000);
    });
    expect(screen.queryByText('Saved')).toBeNull();
    expect(screen.queryByText('Invalid email or password.')).toBeNull();

    vi.useRealTimers();
  });
});
