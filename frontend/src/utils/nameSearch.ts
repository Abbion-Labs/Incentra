/**
 * Mirrors EmployeeNameSearch on the server: the term is matched against the full
 * name in both orders, so "Luka", "Petrović", "Luka P" and "Petrović Luka" all
 * find Luka Petrović. Matching the parts separately would miss anything typed
 * across the space.
 */
export function normalizeSearchTerm(search: string): string {
  return search.trim().toLowerCase().replace(/\s+/g, ' ');
}

export function matchesNameSearch(fullName: string, search: string): boolean {
  const term = normalizeSearchTerm(search);
  if (!term) return true;

  const name = normalizeSearchTerm(fullName);
  const reversed = name.split(' ').reverse().join(' ');

  return name.includes(term) || reversed.includes(term);
}
