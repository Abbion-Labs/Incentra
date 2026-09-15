import { useCallback, useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { EducationLevel, JobPosition, OrganizationUnit } from '../../api/types';
import { useIntl } from '../../i18n';
import { LookupCrudPanel } from './components/LookupCrudPanel';
import { AdminPageHeader } from './components/AdminPageHeader';

export function AdminLookups() {
  const { formatMessage } = useIntl();
  const [orgUnits, setOrgUnits] = useState<OrganizationUnit[]>([]);
  const [positions, setPositions] = useState<JobPosition[]>([]);
  const [education, setEducation] = useState<EducationLevel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

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
      .catch((e) => setError(e instanceof Error ? e.message : formatMessage({ id: 'errors.generic' })))
      .finally(() => setLoading(false));
  }, [load]);

  if (loading) {
    return (
      <div className="admin-page">
        <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
      </div>
    );
  }

  return (
    <div className="admin-page">
      <AdminPageHeader error={error} />
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
