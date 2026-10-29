import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, api } from '../../../api/client';
import type { OrganizationUnit } from '../../../api/types';
import { ToastProvider } from '../../../components/common/Toast';
import { LookupCrudPanel } from './LookupCrudPanel';

const unit: OrganizationUnit = {
  id: 7,
  version: 3,
  name: 'Sektor prodaje',
  code: 'SALES',
  isActive: true,
};

function renderPanel(onReload = vi.fn().mockResolvedValue(undefined)) {
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ToastProvider>
        <LookupCrudPanel
          kind="org"
          orgUnits={[unit]}
          positions={[]}
          educationLevels={[]}
          onReload={onReload}
        />
      </ToastProvider>
    </IntlProvider>,
  );
  return onReload;
}

describe('LookupCrudPanel', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('sends the version it was opened with and keeps the unit code', async () => {
    const put = vi.spyOn(api, 'put').mockResolvedValue(unit);
    renderPanel();

    fireEvent.click(screen.getByRole('button', { name: 'buttons.edit' }));
    fireEvent.click(screen.getByRole('button', { name: 'buttons.save' }));

    await waitFor(() => expect(put).toHaveBeenCalled());
    expect(put.mock.calls[0][1]).toMatchObject({ code: 'SALES', version: 3 });
  });

  it('reloads the list when someone else changed the item meanwhile', async () => {
    vi.spyOn(api, 'put').mockRejectedValue(
      new ApiError('vn-0090', 409, 'vn-0090', 'vn-0090'),
    );
    const onReload = renderPanel();

    fireEvent.click(screen.getByRole('button', { name: 'buttons.edit' }));
    fireEvent.click(screen.getByRole('button', { name: 'buttons.save' }));

    expect(
      await screen.findByText('errors.recordChangedMeanwhile'),
    ).toBeTruthy();
    expect(onReload).toHaveBeenCalled();
  });
});
