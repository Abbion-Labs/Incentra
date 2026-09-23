/**
 * Vrednost u padajućem meniju za ocenjivača bez kontrolora: njegove ocene niko
 * ne kontroliše, pa su odobrene čim ih pošalje.
 */
export const NO_CONTROLLER = 'none';

export function controllerIdFromForm(value: string): number | null {
  return value && value !== NO_CONTROLLER ? Number(value) : null;
}
