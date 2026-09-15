import type { Employee } from '../../api/types';
import { getEmployeeInitials } from '../../utils/initials';

type EmployeeAvatarSource = Pick<Employee, 'fullName' | 'firstName' | 'lastName' | 'avatarUrl'>;

interface EmployeeAvatarProps {
  employee: EmployeeAvatarSource;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const sizeClass: Record<NonNullable<EmployeeAvatarProps['size']>, string> = {
  sm: 'employee-avatar--sm',
  md: 'employee-avatar--md',
  lg: 'employee-avatar--lg',
};

export function EmployeeAvatar({ employee, size = 'md', className }: EmployeeAvatarProps) {
  const initials = getEmployeeInitials(employee.fullName, employee.firstName, employee.lastName);
  const classes = ['employee-avatar', sizeClass[size], className].filter(Boolean).join(' ');

  if (employee.avatarUrl) {
    return (
      <img
        key={employee.avatarUrl}
        src={employee.avatarUrl}
        alt={employee.fullName}
        className={classes}
        loading="lazy"
      />
    );
  }

  return (
    <span className={classes} aria-hidden>
      {initials}
    </span>
  );
}
