import { EmployeeAvatar } from './EmployeeAvatar';

interface PersonNameProps {
  fullName: string;
  avatarUrl?: string | null;
  /** Sporedna informacija ispod imena, npr. organizaciona jedinica. */
  subtitle?: string | null;
  /** Diskretna oznaka (tačka na avataru) sa ovim tekstom za čitače ekrana. */
  highlightLabel?: string;
}

/** Ime zaposlenog sa avatarom (slika ili inicijali), za redove tabela. */
export function PersonName({
  fullName,
  avatarUrl = null,
  subtitle,
  highlightLabel,
}: PersonNameProps) {
  const [firstName = '', ...rest] = fullName.trim().split(/\s+/);
  return (
    <span
      className={`employee-list-name${highlightLabel ? ' employee-list-name--highlight' : ''}`}
      title={highlightLabel}
    >
      <span className="employee-list-name__avatar">
        <EmployeeAvatar
          employee={{
            fullName,
            firstName,
            lastName: rest.join(' '),
            avatarUrl,
          }}
          size="sm"
        />
        {highlightLabel && (
          <span className="employee-list-name__dot">
            <span className="sr-only">{highlightLabel}</span>
          </span>
        )}
      </span>
      <span className="employee-list-name__body">
        <span className="employee-list-name__text">{fullName}</span>
        {subtitle && (
          <span className="employee-list-name__subtitle">{subtitle}</span>
        )}
      </span>
    </span>
  );
}
