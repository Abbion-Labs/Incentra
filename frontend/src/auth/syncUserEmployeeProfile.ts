import type { Employee } from '../api/types';

export function syncUserEmployeeProfile(
  employee: Employee,
  update: (patch: {
    employeeAvatarUrl: string | null;
    employeeFullName: string;
    employeeFirstName: string;
    employeeLastName: string;
  }) => void,
) {
  update({
    employeeAvatarUrl: employee.avatarUrl,
    employeeFullName: employee.fullName,
    employeeFirstName: employee.firstName,
    employeeLastName: employee.lastName,
  });
}
