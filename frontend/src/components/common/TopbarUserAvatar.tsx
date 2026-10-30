import type { UserProfile } from '../../api/types';
import { EmployeeAvatar } from '../employee/EmployeeAvatar';

interface TopbarUserAvatarProps {
  user: UserProfile;
}

export function TopbarUserAvatar({ user }: TopbarUserAvatarProps) {
  const fullName = user.employeeFullName ?? user.email;
  // Nalog bez zaposlenog (admin, plate) dobija inicijale iz e-mail adrese, ne „?“.
  const emailName = user.email.split('@')[0];

  return (
    <EmployeeAvatar
      employee={{
        fullName,
        firstName: user.employeeFirstName ?? emailName,
        lastName: user.employeeLastName ?? '',
        avatarUrl: user.employeeAvatarUrl,
      }}
      size="sm"
      className="topbar-user__avatar"
    />
  );
}
