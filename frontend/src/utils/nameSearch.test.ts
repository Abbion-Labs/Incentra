import { describe, expect, it } from 'vitest';
import { matchesNameSearch } from './nameSearch';

describe('matchesNameSearch', () => {
  const name = 'Luka Petrović';

  it('matches a first name, a surname and the empty term', () => {
    expect(matchesNameSearch(name, 'Luka')).toBe(true);
    expect(matchesNameSearch(name, 'Petrović')).toBe(true);
    expect(matchesNameSearch(name, '')).toBe(true);
    expect(matchesNameSearch(name, '   ')).toBe(true);
  });

  it('matches across the space, which is what used to fail', () => {
    expect(matchesNameSearch(name, 'Luka P')).toBe(true);
    expect(matchesNameSearch(name, 'Luka Petrović')).toBe(true);
  });

  it('matches the reversed order', () => {
    expect(matchesNameSearch(name, 'Petrović Luka')).toBe(true);
    expect(matchesNameSearch(name, 'Petrović L')).toBe(true);
  });

  it('ignores case and surrounding or repeated whitespace', () => {
    expect(matchesNameSearch(name, '  luka   petrović ')).toBe(true);
    expect(matchesNameSearch('Luka   Petrović', 'luka petrović')).toBe(true);
  });

  it('does not match a different person', () => {
    expect(matchesNameSearch(name, 'Ana')).toBe(false);
    expect(matchesNameSearch(name, 'Luka Jovanović')).toBe(false);
  });
});
