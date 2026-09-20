import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import { fetchAllPages } from '../../api/paged';
import type { Employee, EvaluatorSettings } from '../../api/types';
import { useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import { AdminPageHeader } from './components/AdminPageHeader';
import { adminEvaluatorAnalyticsState } from './adminNavigation';

interface SettingsFormValues {
  evaluatorId: string;
  controllerId: string;
  thresholdDoesNotMeet: string;
  thresholdMeets: string;
  thresholdGood: string;
  thresholdExceeds: string;
  percentDoesNotMeet: string;
  percentMeets: string;
  percentGood: string;
  percentExceeds: string;
}

const DEFAULT_THRESHOLDS = {
  thresholdDoesNotMeet: '2',
  thresholdMeets: '2.5',
  thresholdGood: '3.5',
  thresholdExceeds: '4.5',
  percentDoesNotMeet: '0',
  percentMeets: '25',
  percentGood: '50',
  percentExceeds: '100',
};

const emptyForm = (): SettingsFormValues => ({
  evaluatorId: '',
  controllerId: '',
  ...DEFAULT_THRESHOLDS,
});

function settingsToForm(settings: EvaluatorSettings): SettingsFormValues {
  return {
    evaluatorId: String(settings.employeeId),
    controllerId: String(settings.controllerEmployeeId),
    thresholdDoesNotMeet: String(settings.thresholdDoesNotMeet),
    thresholdMeets: String(settings.thresholdMeets),
    thresholdGood: String(settings.thresholdGood),
    thresholdExceeds: String(settings.thresholdExceeds),
    percentDoesNotMeet: String(settings.percentDoesNotMeet),
    percentMeets: String(settings.percentMeets),
    percentGood: String(settings.percentGood),
    percentExceeds: String(settings.percentExceeds),
  };
}

export function AdminEvaluatorSettings() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const navigate = useNavigate();
  const [settings, setSettings] = useState<EvaluatorSettings[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<SettingsFormValues>(emptyForm());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [items, emp] = await Promise.all([
        api.get<EvaluatorSettings[]>('/api/evaluator-settings'),
        fetchAllPages<Employee>(
          (page, pageSize) =>
            `/api/employees?page=${page}&pageSize=${pageSize}&isActive=true`,
        ),
      ]);
      setSettings(items);
      setEmployees(emp);
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
    setForm(settingsToForm(item));
  }

  function setField<K extends keyof SettingsFormValues>(
    key: K,
    value: SettingsFormValues[K],
  ) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (editingId === null) return;

    setSaving(true);
    const payload = {
      controllerEmployeeId: Number(form.controllerId),
      thresholdDoesNotMeet: Number(form.thresholdDoesNotMeet),
      thresholdMeets: Number(form.thresholdMeets),
      thresholdGood: Number(form.thresholdGood),
      thresholdExceeds: Number(form.thresholdExceeds),
      percentDoesNotMeet: Number(form.percentDoesNotMeet),
      percentMeets: Number(form.percentMeets),
      percentGood: Number(form.percentGood),
      percentExceeds: Number(form.percentExceeds),
    };
    try {
      await api.put(`/api/evaluator-settings/${editingId}`, payload);
      toast.success(formatMessage({ id: 'alerts.settingsUpdated' }));
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
                {employees.map((emp) => (
                  <option key={emp.id} value={emp.id}>
                    {emp.fullName}
                  </option>
                ))}
              </select>
            </div>
            <div className="form-row">
              <label htmlFor="thr-dnm">
                {formatMessage({
                  id: 'admin.evaluatorSettings.thresholdDoesNotMeet',
                })}
              </label>
              <input
                id="thr-dnm"
                type="number"
                step="0.01"
                value={form.thresholdDoesNotMeet}
                onChange={(e) =>
                  setField('thresholdDoesNotMeet', e.target.value)
                }
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="thr-meets">
                {formatMessage({
                  id: 'admin.evaluatorSettings.thresholdMeets',
                })}
              </label>
              <input
                id="thr-meets"
                type="number"
                step="0.01"
                value={form.thresholdMeets}
                onChange={(e) => setField('thresholdMeets', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="thr-good">
                {formatMessage({ id: 'admin.evaluatorSettings.thresholdGood' })}
              </label>
              <input
                id="thr-good"
                type="number"
                step="0.01"
                value={form.thresholdGood}
                onChange={(e) => setField('thresholdGood', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="thr-exceeds">
                {formatMessage({
                  id: 'admin.evaluatorSettings.thresholdExceeds',
                })}
              </label>
              <input
                id="thr-exceeds"
                type="number"
                step="0.01"
                value={form.thresholdExceeds}
                onChange={(e) => setField('thresholdExceeds', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="pct-dnm">
                {formatMessage({
                  id: 'admin.evaluatorSettings.percentDoesNotMeet',
                })}
              </label>
              <input
                id="pct-dnm"
                type="number"
                step="1"
                min="0"
                value={form.percentDoesNotMeet}
                onChange={(e) => setField('percentDoesNotMeet', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="pct-meets">
                {formatMessage({ id: 'admin.evaluatorSettings.percentMeets' })}
              </label>
              <input
                id="pct-meets"
                type="number"
                step="1"
                min="0"
                value={form.percentMeets}
                onChange={(e) => setField('percentMeets', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="pct-good">
                {formatMessage({ id: 'admin.evaluatorSettings.percentGood' })}
              </label>
              <input
                id="pct-good"
                type="number"
                step="1"
                min="0"
                value={form.percentGood}
                onChange={(e) => setField('percentGood', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="pct-exceeds">
                {formatMessage({
                  id: 'admin.evaluatorSettings.percentExceeds',
                })}
              </label>
              <input
                id="pct-exceeds"
                type="number"
                step="1"
                min="0"
                value={form.percentExceeds}
                onChange={(e) => setField('percentExceeds', e.target.value)}
                required
              />
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
                  <th className="col-num">
                    {formatMessage({
                      id: 'admin.evaluatorSettings.thresholds',
                    })}
                  </th>
                  <th className="col-num">
                    {formatMessage({
                      id: 'admin.evaluatorSettings.compensationPercent',
                    })}
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
                    <td colSpan={5} className="empty">
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
                      <td className="col-text">{item.controllerFullName}</td>
                      <td className="col-num admin-settings-thresholds">
                        {item.thresholdDoesNotMeet} / {item.thresholdMeets} /{' '}
                        {item.thresholdGood} / {item.thresholdExceeds}
                      </td>
                      <td className="col-num admin-settings-thresholds">
                        {item.percentDoesNotMeet}% / {item.percentMeets}% /{' '}
                        {item.percentGood}% / {item.percentExceeds}%
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
