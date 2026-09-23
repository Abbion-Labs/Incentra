import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import { fetchAllPages } from '../../api/paged';
import type { AdminUser, Employee, EvaluatorSettings } from '../../api/types';
import { useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import { isEditConflict } from '../../utils/editConflict';
import { AdminPageHeader } from './components/AdminPageHeader';
import { adminEvaluatorAnalyticsState } from './adminNavigation';
import { NO_CONTROLLER, controllerIdFromForm } from './evaluatorController';

interface SettingsFormValues {
  evaluatorId: string;
  controllerId: string;
}

const emptyForm = (): SettingsFormValues => ({
  evaluatorId: '',
  controllerId: '',
});

function settingsToForm(settings: EvaluatorSettings): SettingsFormValues {
  return {
    evaluatorId: String(settings.employeeId),
    controllerId:
      settings.controllerEmployeeId === null
        ? NO_CONTROLLER
        : String(settings.controllerEmployeeId),
  };
}

export function AdminEvaluatorSettings() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const navigate = useNavigate();
  const [settings, setSettings] = useState<EvaluatorSettings[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  // Verzija podešavanja sa kojom je forma otvorena; šalje se uz izmenu.
  const [editingVersion, setEditingVersion] = useState<number | null>(null);
  const [form, setForm] = useState<SettingsFormValues>(emptyForm());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [items, emp, userList] = await Promise.all([
        api.get<EvaluatorSettings[]>('/api/evaluator-settings'),
        fetchAllPages<Employee>(
          (page, pageSize) =>
            `/api/employees?page=${page}&pageSize=${pageSize}&isActive=true`,
        ),
        api.get<AdminUser[]>('/api/users'),
      ]);
      setSettings(items);
      setEmployees(emp);
      setUsers(userList);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.generic' }),
      );
    } finally {
      setLoading(false);
    }
  }, [formatMessage, toast]);

  useEffect(() => {
    load();
  }, [load]);

  // A controller is an account carrying the CONTROLLER role; nothing else marks one.
  const controllerOptions = useMemo(() => {
    const controllerEmployeeIds = new Set(
      users
        .filter((u) => u.roles.includes('CONTROLLER') && u.employeeId != null)
        .map((u) => u.employeeId as number),
    );
    // Ocenjivač ne može biti sam sebi kontrolor; za to postoji „Bez kontrolora“.
    return employees.filter(
      (e) => controllerEmployeeIds.has(e.id) && e.id !== editingId,
    );
  }, [employees, users, editingId]);

  const editingEvaluatorName = useMemo(
    () =>
      settings.find((s) => s.employeeId === editingId)?.employeeFullName ?? '',
    [settings, editingId],
  );

  function closeEditor() {
    setEditingId(null);
    setForm(emptyForm());
  }

  function openAnalytics(employeeId: number, e?: React.MouseEvent) {
    e?.stopPropagation();
    navigate(`/controller/evaluators/${employeeId}/analytics`, {
      state: adminEvaluatorAnalyticsState(),
    });
  }

  function startEdit(item: EvaluatorSettings, e?: React.MouseEvent) {
    e?.stopPropagation();
    setEditingId(item.employeeId);
    setEditingVersion(item.version);
    setForm(settingsToForm(item));
  }

  function setField<K extends keyof SettingsFormValues>(
    key: K,
    value: SettingsFormValues[K],
  ) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  /** Podešavanja su izmenjena posle otvaranja forme: forma se puni novim stanjem. */
  async function reopenAfterConflict(employeeId: number) {
    toast.warning(formatMessage({ id: 'errors.recordChangedMeanwhile' }));
    try {
      startEdit(
        await api.get<EvaluatorSettings>(
          `/api/evaluator-settings/${employeeId}`,
        ),
      );
    } catch {
      closeEditor();
    }
    await load();
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (editingId === null) return;

    setSaving(true);
    const payload = {
      controllerEmployeeId: controllerIdFromForm(form.controllerId),
      version: editingVersion,
    };
    try {
      const saved = await api.put<EvaluatorSettings>(
        `/api/evaluator-settings/${editingId}`,
        payload,
      );
      setEditingVersion(saved.version);
      toast.success(formatMessage({ id: 'alerts.settingsUpdated' }));
      await load();
    } catch (err) {
      if (isEditConflict(err)) {
        await reopenAfterConflict(editingId);
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

  return (
    <div className="admin-page">
      <AdminPageHeader
        actions={
          editingId ? (
            <button
              type="button"
              className="btn btn-secondary"
              onClick={closeEditor}
            >
              {formatMessage({ id: 'admin.evaluatorSettings.closeEditor' })}
            </button>
          ) : null
        }
      />

      {editingId === null ? (
        <div className="card">
          <p>{formatMessage({ id: 'admin.evaluatorSettings.selectToEdit' })}</p>
        </div>
      ) : (
        <form className="card admin-form" onSubmit={handleSubmit}>
          <div className="form-grid admin-form__grid">
            <div className="form-row">
              <label>{formatMessage({ id: 'admin.evaluators' })}</label>
              <p className="admin-form__static-value">{editingEvaluatorName}</p>
            </div>
            <div className="form-row">
              <label htmlFor="eval-controller">
                {formatMessage({ id: 'roles.CONTROLLER' })}
              </label>
              <select
                id="eval-controller"
                value={form.controllerId}
                onChange={(e) => setField('controllerId', e.target.value)}
                required
              >
                <option value="">
                  {formatMessage({ id: 'common.selectPlaceholder' })}
                </option>
                <option value={NO_CONTROLLER}>
                  {formatMessage({
                    id: 'admin.evaluatorSettings.noController',
                  })}
                </option>
                {controllerOptions.map((emp) => (
                  <option key={emp.id} value={emp.id}>
                    {emp.fullName}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving
                ? formatMessage({ id: 'buttons.saving' })
                : formatMessage({ id: 'buttons.saveChanges' })}
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={closeEditor}
            >
              {formatMessage({ id: 'buttons.cancel' })}
            </button>
          </div>
        </form>
      )}

      <div className="card">
        {loading ? (
          <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
        ) : (
          <div className="table-wrap">
            <table className="table table--hover table--compact table--clickable">
              <thead>
                <tr>
                  <th className="col-text">
                    {formatMessage({ id: 'admin.evaluators' })}
                  </th>
                  <th className="col-text">
                    {formatMessage({ id: 'roles.CONTROLLER' })}
                  </th>
                  <th
                    className="col-actions"
                    aria-label={formatMessage({ id: 'admin.actions' })}
                  />
                </tr>
              </thead>
              <tbody>
                {settings.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="empty">
                      {formatMessage({
                        id: 'admin.evaluatorSettings.noSettings',
                      })}
                    </td>
                  </tr>
                ) : (
                  settings.map((item) => (
                    <tr
                      key={item.employeeId}
                      onClick={() => openAnalytics(item.employeeId)}
                      tabIndex={0}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          openAnalytics(item.employeeId);
                        }
                      }}
                    >
                      <td className="cell-primary col-text">
                        {item.employeeFullName}
                      </td>
                      <td className="col-text">
                        {item.controllerFullName ??
                          formatMessage({
                            id: 'admin.evaluatorSettings.noControllerShort',
                          })}
                      </td>
                      <td className="col-actions">
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          onClick={(e) => startEdit(item, e)}
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
