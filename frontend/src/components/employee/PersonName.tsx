import { EmployeeAvatar } from './EmployeeAvatar';

interface PersonNameProps {
  fullName: string;
  avatarUrl?: string | null;
  /** Sporedna informacija ispod imena, npr. organizaciona jedinica. */
  subtitle?: string | null;
  /** Oznaka uz ime (tačka na avataru i kratak tekst), npr. „Nova ocena“. */
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
          <span className="employee-list-name__dot" aria-hidden />
        )}
      </span>
      <span className="employee-list-name__body">
        <span className="employee-list-name__text">
          {fullName}
          {/* Sama tačka ne govori šta znači, pa uz ime stoji i tekst. */}
          {highlightLabel && (
            <span className="employee-list-name__flag">{highlightLabel}</span>
          )}
        </span>
        {subtitle && (
          <span className="employee-list-name__subtitle">{subtitle}</span>
        )}
      </span>
    </span>
  );
}
