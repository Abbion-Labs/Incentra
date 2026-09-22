import { useCallback, useEffect, useState } from 'react';
import { api } from '../../api/client';
import type {
  EducationLevel,
  JobPosition,
  OrganizationUnit,
} from '../../api/types';
import { useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import { LookupCrudPanel } from './components/LookupCrudPanel';

export function AdminLookups() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [orgUnits, setOrgUnits] = useState<OrganizationUnit[]>([]);
  const [positions, setPositions] = useState<JobPosition[]>([]);
  const [education, setEducation] = useState<EducationLevel[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    const [ou, jp, ed] = await Promise.all([
      api.get<OrganizationUnit[]>('/api/organization-units'),
      api.get<JobPosition[]>('/api/job-positions'),
      api.get<EducationLevel[]>('/api/education-levels'),
    ]);
    setOrgUnits(ou);
    setPositions(jp);
    setEducation(ed);
  }, []);

  useEffect(() => {
    setLoading(true);
    load()
      .catch((e) =>
        toast.error(
          e instanceof Error
            ? e.message
            : formatMessage({ id: 'errors.generic' }),
        ),
      )
      .finally(() => setLoading(false));
  }, [load, formatMessage, toast]);

  if (loading) {
    return (
      <div className="admin-page">
        <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
      </div>
    );
  }

  return (
    <div className="admin-page">
      <div className="admin-lookups-grid">
        <LookupCrudPanel
          kind="org"
          orgUnits={orgUnits}
          positions={positions}
          educationLevels={education}
          onReload={load}
        />
        <LookupCrudPanel
          kind="position"
          orgUnits={orgUnits}
          positions={positions}
          educationLevels={education}
          onReload={load}
        />
        <LookupCrudPanel
          kind="education"
          orgUnits={orgUnits}
          positions={positions}
          educationLevels={education}
          onReload={load}
        />
      </div>
    </div>
  );
}
