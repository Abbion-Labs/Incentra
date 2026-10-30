import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Employee } from '../api/types';

/** Zaposleni za zaglavlje ocene; bez prava pristupa zaglavlje ostaje bez radnog mesta. */
export function useEmployee(employeeId: number | undefined) {
  const [employee, setEmployee] = useState<Employee | null>(null);

  useEffect(() => {
    if (!employeeId) {
      setEmployee(null);
      return;
    }

    let cancelled = false;
    api
      .get<Employee>(`/api/employees/${employeeId}`)
      .then((data) => {
        if (!cancelled) setEmployee(data);
      })
      .catch(() => {
        if (!cancelled) setEmployee(null);
      });

    return () => {
      cancelled = true;
    };
  }, [employeeId]);

  return employee;
}
