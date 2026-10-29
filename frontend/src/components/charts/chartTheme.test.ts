import { describe, expect, it } from 'vitest';
import { niceAxis } from './chartTheme';

describe('niceAxis', () => {
  it('rounds a count axis up to whole, even steps', () => {
    expect(niceAxis(11, { integer: true })).toEqual({
      max: 15,
      ticks: [0, 5, 10, 15],
    });
    expect(niceAxis(36, { integer: true })).toEqual({
      max: 40,
      ticks: [0, 10, 20, 30, 40],
    });
    expect(niceAxis(2, { integer: true }).ticks).toEqual([0, 1, 2]);
  });

  it('uses round amounts for money', () => {
    expect(niceAxis(100_551)).toEqual({
      max: 150_000,
      ticks: [0, 50_000, 100_000, 150_000],
    });
  });

  it('keeps small fractions tidy', () => {
    expect(niceAxis(0.8).ticks).toEqual([0, 0.2, 0.4, 0.6, 0.8]);
  });
});
