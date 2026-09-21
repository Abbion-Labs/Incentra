import { fireEvent, render, screen, within } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../../api/client';
import type { OrganizationUnit } from '../../../api/types';
import { ToastProvider } from '../../../components/common/Toast';
import { LookupCrudPanel } from './LookupCrudPanel';

const orgUnits: OrganizationUnit[] = [
  { id: 1, name: 'Prodaja', code: null, isActive: true },
  { id: 2, name: 'Razvoj', code: null, isActive: true },
];

function rowOf(name: string) {
  return within(screen.getByText(name).closest('tr')!);
}

describe('LookupCrudPanel', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('ignores selecting another item while the current one is saving', () => {
    render(
      <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
        <ToastProvider>
          <LookupCrudPanel
            kind="org"
            orgUnits={orgUnits}
            positions={[]}
            educationLevels={[]}
            onReload={() => Promise.resolve()}
          />
        </ToastProvider>
      </IntlProvider>,
    );

    fireEvent.click(
      rowOf('Prodaja').getByRole('button', { name: 'buttons.edit' }),
    );
    const name = screen.getByLabelText('common.name');
    expect(name).toHaveProperty('value', 'Prodaja');
    vi.spyOn(api, 'put').mockReturnValue(new Promise(() => undefined));

    fireEvent.submit(name.closest('form')!);
    const razvojEdit = rowOf('Razvoj').getByRole('button', {
      name: 'buttons.edit',
    });
    expect(razvojEdit).toHaveProperty('disabled', true);
    fireEvent.click(razvojEdit);

    expect(name).toHaveProperty('value', 'Prodaja');
  });
});
