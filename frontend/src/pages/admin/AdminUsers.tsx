import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from '../../api/client';
import { fetchAllPages } from '../../api/paged';
import type { AdminUser, Employee, EvaluatorSettings } from '../../api/types';
import { useIntl } from '../../i18n';
import {
  AdminUserForm,
  emptyUserForm,
  ROLE_ORDER,
  userToForm,
  type UserFormValues,
} from './components/AdminUserForm';
import { AdminPageHeader } from './components/AdminPageHeader';
import { controllerIdFromForm } from './evaluatorController';
import { roleLabel } from '../../utils/status';
import { useToast } from '../../hooks';

export function AdminUsers() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [formValues, setFormValues] = useState<UserFormValues>(emptyUserForm());
  const [editingUser, setEditingUser] = useState<AdminUser | null>(null);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [configuredEvaluatorIds, setConfiguredEvaluatorIds] = useState<
    Set<number>
  >(new Set());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [list, employeeList, settings] = await Promise.all([
        api.get<AdminUser[]>('/api/users'),
        fetchAllPages<Employee>(
          (page, pageSize) =>
            `/api/employees?page=${page}&pageSize=${pageSize}&isActive=true`,
        ),
        api.get<EvaluatorSettings[]>('/api/evaluator-settings'),
      ]);
      setUsers(list);
      setEmployees(employeeList);
      setConfiguredEvaluatorIds(new Set(settings.map((s) => s.employeeId)));
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.loadFailed' }),
      );
    } finally {
      setLoading(false);
    }
  }, [formatMessage, toast]);

  // Only real controllers, and an evaluator cannot be their own.
  const controllerOptions = useMemo(() => {
    const controllerEmployeeIds = new Set(
      users
        .filter((u) => u.roles.includes('CONTROLLER') && u.employeeId != null)
        .map((u) => u.employeeId as number),
    );
    return employees.filter(
      (e) =>
        controllerEmployeeIds.has(e.id) && e.id !== editingUser?.employeeId,
    );
  }, [employees, users, editingUser]);

  const alreadyConfiguredEvaluator =
    editingUser?.employeeId != null &&
    configuredEvaluatorIds.has(editingUser.employeeId);

  useEffect(() => {
    load();
  }, [load]);

  function startCreate() {
    setEditingUser(null);
    setFormValues(emptyUserForm());
  }

  function startEdit(user: AdminUser, e?: React.MouseEvent) {
    e?.stopPropagation();
    setEditingUser(user);
    setFormValues(userToForm(user));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function handleSubmit() {
    setSaving(true);
    try {
      if (editingUser) {
        await api.put(`/api/users/${editingUser.id}`, {
          email: formValues.email.trim(),
          isActive: formValues.isActive,
          roleCodes: formValues.roleCodes,
          controllerEmployeeId: controllerIdFromForm(
            formValues.controllerEmployeeId,
          ),
        });
        if (formValues.password.trim()) {
          await api.put(`/api/users/${editingUser.id}/password`, {
            newPassword: formValues.password,
          });
        }
        toast.success(
          formatMessage(
            { id: 'alerts.userUpdated' },
            { email: formValues.email.trim() },
          ),
        );
        startCreate();
      } else {
        await api.post('/api/auth/register', {
          email: formValues.email.trim(),
          password: formValues.password,
          roleCodes: formValues.roleCodes,
        });
        toast.success(
          formatMessage(
            { id: 'alerts.userCreated' },
            {
              email: formValues.email.trim(),
              roles: formValues.roleCodes.join(', '),
            },
          ),
        );
        startCreate();
      }
      await load();
    } catch (err) {
      toast.error(
        err instanceof Error
          ? err.message
          : formatMessage({ id: 'errors.saveFailed' }),
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="admin-page">
      <AdminPageHeader
        actions={
          editingUser ? (
            <button
              type="button"
              className="btn btn-secondary"
              onClick={startCreate}
            >
              {formatMessage({ id: 'admin.users.newUser' })}
            </button>
          ) : null
        }
      />

      <AdminUserForm
        values={formValues}
        editingUser={editingUser}
        saving={saving}
        controllerOptions={controllerOptions}
        alreadyConfiguredEvaluator={alreadyConfiguredEvaluator}
        onChange={setFormValues}
        onSubmit={handleSubmit}
        onCancel={startCreate}
      />

      <div className="card">
        {loading ? (
          <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
        ) : (
          <div className="table-wrap">
            <table className="table table--hover table--clickable">
              <thead>
                <tr>
                  <th className="col-text">
                    {formatMessage({ id: 'common.email' })}
                  </th>
                  <th className="col-text">
                    {formatMessage({ id: 'admin.roles' })}
                  </th>
                  <th className="col-text">
                    {formatMessage({ id: 'admin.users.linkedEmployee' })}
                  </th>
                  <th className="col-meta table-col--compact">
                    {formatMessage({ id: 'admin.active' })}
                  </th>
                  <th
                    className="col-actions"
                    aria-label={formatMessage({ id: 'admin.actions' })}
                  />
                </tr>
              </thead>
              <tbody>
                {users.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="empty">
                      {formatMessage({ id: 'admin.users.noUsers' })}
                    </td>
                  </tr>
                ) : (
                  users.map((user) => (
                    <tr
                      key={user.id}
                      onClick={() => startEdit(user)}
                      tabIndex={0}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          startEdit(user);
                        }
                      }}
                    >
                      <td className="cell-primary col-text">{user.email}</td>
                      <td className="col-text">
                        {ROLE_ORDER.filter((role) => user.roles.includes(role))
                          .map((role) => roleLabel(role, formatMessage))
                          .join(', ')}
                      </td>
                      <td className="col-text">
                        {user.employeeFullName ?? '—'}
                      </td>
                      <td className="col-meta table-col--compact">
                        {user.isActive
                          ? formatMessage({ id: 'common.yes' })
                          : formatMessage({ id: 'common.no' })}
                      </td>
                      <td className="col-actions">
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          onClick={(e) => startEdit(user, e)}
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
        )}
      </div>
    </div>
  );
}
