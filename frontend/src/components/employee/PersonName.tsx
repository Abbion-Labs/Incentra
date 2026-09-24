import { EmployeeAvatar } from './EmployeeAvatar';

interface PersonNameProps {
  fullName: string;
  avatarUrl?: string | null;
}

/** Ime zaposlenog sa avatarom (slika ili inicijali), za redove tabela. */
export function PersonName({ fullName, avatarUrl = null }: PersonNameProps) {
  const [firstName = '', ...rest] = fullName.trim().split(/\s+/);
  return (
    <span className="employee-list-name">
      <EmployeeAvatar
        employee={{
          fullName,
          firstName,
          lastName: rest.join(' '),
          avatarUrl,
        }}
        size="sm"
      />
      <span className="employee-list-name__text">{fullName}</span>
    </span>
  );
}
