import { fireEvent, render, screen } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { describe, expect, it, vi } from 'vitest';
import type { RatingLevel } from '../../api/types';
import { RatingScale } from './RatingScale';

const levels: RatingLevel[] = [
  { id: 10, value: 0, label: '/', description: '' },
  { id: 11, value: 1, label: 'Nezadovoljavajuće', description: '' },
  { id: 12, value: 2, label: 'Zadovoljava', description: '' },
  { id: 13, value: 3, label: 'Dobro', description: '' },
];

function renderScale(value: number, onChange?: (id: number) => void) {
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <RatingScale
        ratingLevels={levels}
        value={value}
        label="Cilj"
        onChange={onChange}
      />
    </IntlProvider>,
  );
  return screen.getAllByRole('radio');
}

describe('RatingScale', () => {
  it('offers one button per rating, without the not-rated level', () => {
    const options = renderScale(10, vi.fn());
    expect(options.map((option) => option.textContent)).toEqual([
      '1',
      '2',
      '3',
    ]);
    expect(
      options.every(
        (option) => option.getAttribute('aria-checked') === 'false',
      ),
    ).toBe(true);
  });

  it('picks a rating with one click', () => {
    const onChange = vi.fn();
    const options = renderScale(10, onChange);
    fireEvent.click(options[1]);
    expect(onChange).toHaveBeenCalledWith(12);
  });

  it('clears the rating when the chosen one is clicked again', () => {
    const onChange = vi.fn();
    const options = renderScale(13, onChange);
    expect(options[2].getAttribute('aria-checked')).toBe('true');
    fireEvent.click(options[2]);
    expect(onChange).toHaveBeenCalledWith(10);
  });

  it('only shows the rating when it cannot be changed', () => {
    const options = renderScale(12);
    expect(
      options.every((option) => (option as HTMLButtonElement).disabled),
    ).toBe(true);
    expect(options[1].getAttribute('aria-checked')).toBe('true');
  });
});
