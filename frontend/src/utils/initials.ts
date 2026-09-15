export function getEmployeeInitials(
  fullName: string,
  firstName?: string,
  lastName?: string,
): string {
  const first = (firstName ?? fullName.split(/\s+/)[0] ?? '').trim();
  const last = (lastName ?? fullName.split(/\s+/).slice(1).join(' ') ?? '').trim();
  const firstInitial = first.charAt(0);
  const lastInitial = last.charAt(0) || first.charAt(1) || '';
  const initials = `${firstInitial}${lastInitial}`.toUpperCase();
  return initials || '?';
}
