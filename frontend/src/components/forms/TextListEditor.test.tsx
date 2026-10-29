import { render, screen } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { describe, expect, it } from 'vitest';
import { TextListEditor } from './TextListEditor';

function renderEditor(description: string) {
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <TextListEditor
        items={[{ description, sortOrder: 0 }]}
        setItems={() => undefined}
        placeholder="Cilj"
        addLabel="Dodaj"
      />
    </IntlProvider>,
  );
}

describe('TextListEditor', () => {
  it('caps each item at the length the database holds', () => {
    renderEditor('Kratak cilj');

    expect(screen.getByPlaceholderText('Cilj').getAttribute('maxLength')).toBe(
      '500',
    );
    expect(screen.queryByText(/\/ 500/)).toBeNull();
  });

  it('shows how much room is left once the text nears the limit', () => {
    renderEditor('a'.repeat(450));

    expect(screen.getByText('450 / 500')).toBeTruthy();
  });
});
