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
import { ToolbarSearch } from '../../components/common/ToolbarSearch';
import { TableIconButton } from '../../components/common/TableIconButton';
import { controllerIdFromForm } from './evaluatorController';
import { roleLabel } from '../../utils/status';
import { useToast } from '../../hooks';
import { isEditConflict } from '../../utils/editConflict';

export function AdminUsers() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [formValues, setFormValues] = useState<UserFormValues>(emptyUserForm());
  const [editingUser, setEditingUser] = useState<AdminUser | null>(null);
  // Forma za unos je skrivena dok se ne izabere dodavanje ili izmena.
  const [formOpen, setFormOpen] = useState(false);
  const [search, setSearch] = useState('');
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

  // Novi nalog se vezuje za aktivnog zaposlenog koji još nema nalog.
  const employeeOptions = useMemo(
    () => employees.filter((e) => e.userId == null),
    [employees],
  );

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

  function openCreate() {
    startCreate();
    setFormOpen(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function closeForm() {
    startCreate();
    setFormOpen(false);
  }

  function startEdit(user: AdminUser, e?: React.MouseEvent) {
    e?.stopPropagation();
    setEditingUser(user);
    setFormValues(userToForm(user));
    setFormOpen(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  /** Nalog je izmenjen posle otvaranja forme: forma se puni njegovim novim stanjem. */
  async function reopenAfterConflict(userId: number) {
    toast.warning(formatMessage({ id: 'errors.recordChangedMeanwhile' }));
    try {
      const fresh = (await api.get<AdminUser[]>('/api/users')).find(
        (u) => u.id === userId,
      );
      if (fresh) {
        startEdit(fresh);
      } else {
        closeForm();
      }
    } catch {
      closeForm();
    }
    await load();
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
          version: editingUser.version,
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
        closeForm();
      } else {
        const linksEmployee = ['EMPLOYEE', 'EVALUATOR', 'CONTROLLER'].some(
          (code) => formValues.roleCodes.includes(code),
        );
        await api.post('/api/auth/register', {
          email: formValues.email.trim(),
          password: formValues.password,
          roleCodes: formValues.roleCodes,
          employeeId: linksEmployee ? Number(formValues.employeeId) : null,
          controllerEmployeeId: controllerIdFromForm(
            formValues.controllerEmployeeId,
          ),
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
        closeForm();
      }
      await load();
    } catch (err) {
      if (editingUser && isEditConflict(err)) {
        await reopenAfterConflict(editingUser.id);
        return;
      }
      toast.error(
        err instanceof Error
          ? err.message
          : formatMessage({ id: 'errors.saveFailed' }),
      );
    } finally {
      setSaving(false);
    }
  }

  // Pretraga po e-mailu i imenu povezanog zaposlenog, bez novog zahteva.
  const query = search.trim().toLowerCase();
  const visibleUsers = query
    ? users.filter((user) =>
        [user.email, user.employeeFullName ?? '']
          .join(' ')
          .toLowerCase()
          .includes(query),
      )
    : users;

  return (
    <div className="admin-page">
      {formOpen && (
        <AdminUserForm
          values={formValues}
          editingUser={editingUser}
          saving={saving}
          controllerOptions={controllerOptions}
          employeeOptions={employeeOptions}
          alreadyConfiguredEvaluator={alreadyConfiguredEvaluator}
          onChange={setFormValues}
          onSubmit={handleSubmit}
          onCancel={closeForm}
        />
      )}

      <div className="card card--flush data-panel">
        <div className="data-panel__toolbar">
          <ToolbarSearch
            id="users-search"
            label={formatMessage({ id: 'common.search' })}
            placeholder={formatMessage({ id: 'admin.users.searchPlaceholder' })}
            value={search}
            onChange={setSearch}
          />
          {!formOpen && (
            <button
              type="button"
              className="btn btn-primary btn-sm"
              onClick={openCreate}
            >
              {formatMessage({ id: 'admin.users.addUser' })}
            </button>
          )}
        </div>
        {loading ? (
          <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
        ) : (
          <div className="table-wrap">
            <table className="table table--hover table--clickable table--stack">
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
                {visibleUsers.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="empty">
                      {formatMessage({ id: 'admin.users.noUsers' })}
                    </td>
                  </tr>
                ) : (
                  visibleUsers.map((user) => (
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
                      <td className="cell-primary col-text stack-title">
                        {user.email}
                      </td>
                      <td
                        className="col-text"
                        data-label={formatMessage({ id: 'admin.roles' })}
                      >
                        {ROLE_ORDER.filter((role) => user.roles.includes(role))
                          .map((role) => roleLabel(role, formatMessage))
                          .join(', ')}
                      </td>
                      <td
                        className="col-text"
                        data-label={formatMessage({
                          id: 'admin.users.linkedEmployee',
                        })}
                      >
                        {user.employeeFullName ?? '—'}
                      </td>
                      <td
                        className="col-meta table-col--compact"
                        data-label={formatMessage({ id: 'admin.active' })}
                      >
                        {user.isActive
                          ? formatMessage({ id: 'common.yes' })
                          : formatMessage({ id: 'common.no' })}
                      </td>
                      <td className="col-actions">
                        <TableIconButton
                          icon="edit"
                          label={formatMessage({ id: 'buttons.edit' })}
                          onClick={() => startEdit(user)}
                        />
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
