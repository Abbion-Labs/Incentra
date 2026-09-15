import type { UserProfile } from '../../api/types';
import { EmployeeAvatar } from '../employee/EmployeeAvatar';

interface TopbarUserAvatarProps {
  user: UserProfile;
}

export function TopbarUserAvatar({ user }: TopbarUserAvatarProps) {
  const fullName = user.employeeFullName ?? user.email;

  return (
    <EmployeeAvatar
      employee={{
        fullName,
        firstName: user.employeeFirstName ?? '',
        lastName: user.employeeLastName ?? '',
        avatarUrl: user.employeeAvatarUrl,
      }}
      size="sm"
      className="topbar-user__avatar"
    />
  );
}
