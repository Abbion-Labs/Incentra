import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import { fetchAllPages } from '../../api/paged';
import type { AdminUser, EducationLevel, Employee, EvaluatorSettings, JobPosition, OrganizationUnit } from '../../api/types';
import { InfiniteScrollSentinel } from '../../components/common/InfiniteScrollSentinel';
import { useDebouncedSearch, usePagedList, useToast } from '../../hooks';
import {
  AdminEmployeeForm,
  emptyEmployeeForm,
  employeeToForm,
  type EmployeeFormValues,
} from './components/AdminEmployeeForm';
import { AdminPageHeader } from './components/AdminPageHeader';
import { adminEmployeeProfileState, adminEvaluatorAnalyticsState } from './adminNavigation';
import { useIntl } from '../../i18n';

function buildEmployeePayload(values: EmployeeFormValues, includeActive: boolean) {
  return {
    firstName: values.firstName.trim(),
    lastName: values.lastName.trim(),
    organizationUnitId: Number(values.organizationUnitId),
    jobPositionId: Number(values.jobPositionId),
    educationLevelId: Number(values.educationLevelId),
    evaluatorEmployeeId: values.evaluatorEmployeeId ? Number(values.evaluatorEmployeeId) : null,
    hiredAt: values.hiredAt || null,
    ...(includeActive ? { isActive: values.isActive } : {}),
  };
}

export function AdminEmployees() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const navigate = useNavigate();
  const [orgUnits, setOrgUnits] = useState<OrganizationUnit[]>([]);
  const [positions, setPositions] = useState<JobPosition[]>([]);
  const [educationLevels, setEducationLevels] = useState<EducationLevel[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [evaluatorOptions, setEvaluatorOptions] = useState<Employee[]>([]);
  const [saving, setSaving] = useState(false);

  const { input: search, debounced: debouncedSearch, setInput: setSearchInput } = useDebouncedSearch();
  const [filterOrgId, setFilterOrgId] = useState('');
  const [filterActive, setFilterActive] = useState('');

  const [formValues, setFormValues] = useState<EmployeeFormValues>(emptyEmployeeForm());
  const [editingId, setEditingId] = useState<number | null>(null);
  const [linkedUserId, setLinkedUserId] = useState('');

  const listQueryKey = `${debouncedSearch}|${filterOrgId}|${filterActive}`;

  const {
    items: employees,
    loading,
    loadingMore,
    hasMore,
    loadMore,
    reload,
    error: listError,
  } = usePagedList<Employee>({
    queryKey: listQueryKey,
    fetchPage: (page, pageSize) => {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
      });
      if (debouncedSearch.trim()) params.set('search', debouncedSearch.trim());
      if (filterOrgId) params.set('organizationUnitId', filterOrgId);
      if (filterActive !== '') params.set('isActive', filterActive);
      return `/api/employees?${params}`;
    },
  });

  const loadLookups = useCallback(async () => {
    const [ou, jp, ed, userList, evaluatorResult, evaluatorSettings] = await Promise.all([
      api.get<OrganizationUnit[]>('/api/organization-units'),
      api.get<JobPosition[]>('/api/job-positions'),
      api.get<EducationLevel[]>('/api/education-levels'),
      api.get<AdminUser[]>('/api/users'),
      fetchAllPages<Employee>((page, pageSize) =>
        `/api/employees?page=${page}&pageSize=${pageSize}&isActive=true`),
      api.get<EvaluatorSettings[]>('/api/evaluator-settings'),
    ]);
    setOrgUnits(ou);
    setPositions(jp);
    setEducationLevels(ed);
    setUsers(userList);
    // Only people who are set up as evaluators; the backend rejects anyone else.
    const evaluatorIds = new Set(evaluatorSettings.map((setting) => setting.employeeId));
    setEvaluatorOptions(evaluatorResult.filter((employee) => evaluatorIds.has(employee.id)));
  }, []);

  useEffect(() => {
    loadLookups().catch((e) => {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'errors.loadFailed' }));
    });
  }, [loadLookups, formatMessage, toast]);

  useEffect(() => {
    if (listError) toast.error(listError);
  }, [listError, toast]);

  function openProfile(employeeId: number) {
    navigate(`/evaluator/employees/${employeeId}`, { state: adminEmployeeProfileState() });
  }

  function openEvaluatorAnalytics(evaluatorEmployeeId: number, e: React.MouseEvent) {
    e.stopPropagation();
    navigate(`/controller/evaluators/${evaluatorEmployeeId}/analytics`, {
      state: adminEvaluatorAnalyticsState(),
    });
  }

  function startCreate() {
    setEditingId(null);
    setFormValues(emptyEmployeeForm());
    setLinkedUserId('');
  }

  function startEdit(employee: Employee, e?: React.MouseEvent) {
    e?.stopPropagation();
    setEditingId(employee.id);
    setFormValues(employeeToForm(employee));
    setLinkedUserId(employee.userId ? String(employee.userId) : '');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function handleSubmit() {
    if (!formValues.educationLevelId) {
      toast.warning(formatMessage({ id: 'errors.educationRequired' }));
      return;
    }

    setSaving(true);
    try {
      if (editingId) {
        const payload = buildEmployeePayload(formValues, true);
        await api.put(`/api/employees/${editingId}`, payload);
        const currentUserId = employees.find((e) => e.id === editingId)?.userId ?? null;
        const nextUserId = linkedUserId ? Number(linkedUserId) : null;
        if (nextUserId !== currentUserId) {
          await api.put(`/api/employees/${editingId}/user`, { userId: nextUserId });
        }
        toast.success(formatMessage({ id: 'alerts.employeeUpdated' }));
      } else {
        const payload = buildEmployeePayload(formValues, false);
        await api.post('/api/employees', payload);
        toast.success(formatMessage({ id: 'alerts.employeeAdded' }));
        startCreate();
      }
      await Promise.all([reload(), loadLookups()]);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'errors.saveFailed' }));
    } finally {
      setSaving(false);
    }
  }

  const formEvaluators = useMemo(
    () => evaluatorOptions.filter((e) => e.id !== editingId),
    [evaluatorOptions, editingId],
  );

  return (
    <div className="admin-page">
      <AdminPageHeader
        actions={
          editingId ? (
            <button type="button" className="btn btn-secondary" onClick={startCreate}>
              {formatMessage({ id: 'admin.employeeForm.newEmployee' })}
            </button>
          ) : null
        }
      />

      <AdminEmployeeForm
        values={formValues}
        orgUnits={orgUnits}
        positions={positions}
        educationLevels={educationLevels}
        evaluators={formEvaluators}
        users={users}
        linkedUserId={linkedUserId}
        editingId={editingId}
        saving={saving}
        onChange={setFormValues}
        onLinkedUserChange={setLinkedUserId}
        onSubmit={handleSubmit}
        onCancel={startCreate}
      />

      <div className="card">
        <div className="filter-bar admin-filters">
          <div className="form-row filter-bar-search">
            <label htmlFor="emp-search">{formatMessage({ id: 'evaluation.search' })}</label>
            <input
              id="emp-search"
              value={search}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder={formatMessage({ id: 'admin.searchNamePlaceholder' })}
            />
          </div>
          <div className="form-row">
            <label htmlFor="emp-filter-org">{formatMessage({ id: 'evaluation.orgUnitShort' })}</label>
            <select id="emp-filter-org" value={filterOrgId} onChange={(e) => setFilterOrgId(e.target.value)}>
              <option value="">{formatMessage({ id: 'common.all' })}</option>
              {orgUnits.map((o) => (
                <option key={o.id} value={o.id}>{o.name}</option>
              ))}
            </select>
          </div>
          <div className="form-row">
            <label htmlFor="emp-filter-active">{formatMessage({ id: 'evaluation.status' })}</label>
            <select id="emp-filter-active" value={filterActive} onChange={(e) => setFilterActive(e.target.value)}>
              <option value="">{formatMessage({ id: 'evaluation.all' })}</option>
              <option value="true">{formatMessage({ id: 'admin.activePlural' })}</option>
              <option value="false">{formatMessage({ id: 'admin.inactivePlural' })}</option>
            </select>
          </div>
        </div>

        {loading ? (
          <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
        ) : (
          <>
          <div className="table-wrap">
            <table className="table table--hover table--clickable">
              <thead>
                <tr>
                  <th className="col-text">{formatMessage({ id: 'common.firstName' })}</th>
                  <th className="col-text">{formatMessage({ id: 'evaluation.orgUnitShort' })}</th>
                  <th className="col-text">{formatMessage({ id: 'common.position' })}</th>
                  <th className="col-text">{formatMessage({ id: 'admin.evaluators' })}</th>
                  <th className="col-text">{formatMessage({ id: 'common.account' })}</th>
                  <th className="col-meta table-col--compact">{formatMessage({ id: 'common.active' })}</th>
                  <th className="col-actions" aria-label={formatMessage({ id: 'admin.actions' })} />
                </tr>
              </thead>
              <tbody>
                {employees.length === 0 ? (
                  <tr>
                      <td colSpan={7} className="empty">{formatMessage({ id: 'evaluation.noSearchResults' })}</td>
                  </tr>
                ) : (
                  employees.map((employee) => (
                    <tr
                      key={employee.id}
                      onClick={() => openProfile(employee.id)}
                      tabIndex={0}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          openProfile(employee.id);
                        }
                      }}
                    >
                      <td className="cell-primary col-text">{employee.fullName}</td>
                      <td className="col-text">{employee.organizationUnitName}</td>
                      <td className="col-text">{employee.jobPositionName}</td>
                      <td className="col-text">
                        {employee.evaluatorEmployeeId && employee.evaluatorFullName ? (
                          <button
                            type="button"
                            className="table-link"
                            onClick={(e) => openEvaluatorAnalytics(employee.evaluatorEmployeeId!, e)}
                          >
                            {employee.evaluatorFullName}
                          </button>
                        ) : (
                          '—'
                        )}
                      </td>
                      <td className="col-text">
                        {employee.userId
                          ? users.find((u) => u.id === employee.userId)?.email ?? `#${employee.userId}`
                          : '—'}
                      </td>
                      <td className="col-meta table-col--compact">{employee.isActive ? formatMessage({ id: 'common.yes' }) : formatMessage({ id: 'common.no' })}</td>
                      <td className="col-actions">
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          onClick={(e) => startEdit(employee, e)}
                        >
                          {formatMessage({ id: 'buttons.edit' })}
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
          <InfiniteScrollSentinel
            hasMore={hasMore}
            isLoading={loadingMore}
            onLoadMore={loadMore}
          />
          </>
        )}
      </div>
    </div>
  );
}
