import { useState } from 'react';
import { api } from '../../../api/client';
import type {
  EducationLevel,
  JobPosition,
  OrganizationUnit,
} from '../../../api/types';
import { useToast } from '../../../hooks';
import { useIntl } from '../../../i18n';
import { isEditConflict } from '../../../utils/editConflict';
import { TEXT_LIMITS } from '../../../utils/textLimits';

type LookupKind = 'org' | 'position' | 'education';
type LookupItem = OrganizationUnit | JobPosition | EducationLevel;

interface LookupCrudPanelProps {
  kind: LookupKind;
  orgUnits: OrganizationUnit[];
  positions: JobPosition[];
  educationLevels: EducationLevel[];
  onReload: () => Promise<void>;
}

const CONFIG = {
  org: {
    titleKey: 'admin.lookups.orgUnits',
    endpoint: '/api/organization-units',
    maxNameLength: TEXT_LIMITS.organizationUnitName,
  },
  position: {
    titleKey: 'admin.lookups.jobPositions',
    endpoint: '/api/job-positions',
    maxNameLength: TEXT_LIMITS.lookupName,
  },
  education: {
    titleKey: 'admin.lookups.educationLevels',
    endpoint: '/api/education-levels',
    maxNameLength: TEXT_LIMITS.lookupName,
  },
} as const;

function nextSortOrder(items: { sortOrder: number }[]): number {
  if (items.length === 0) return 0;
  return Math.max(...items.map((item) => item.sortOrder)) + 1;
}

export function LookupCrudPanel({
  kind,
  orgUnits,
  positions,
  educationLevels,
  onReload,
}: LookupCrudPanelProps) {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const config = CONFIG[kind];
  const [editingId, setEditingId] = useState<number | null>(null);
  // Verzija stavke sa kojom je forma otvorena; šalje se uz izmenu.
  const [editingVersion, setEditingVersion] = useState<number | null>(null);
  const [name, setName] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [saving, setSaving] = useState(false);

  const rows: LookupItem[] =
    kind === 'org'
      ? orgUnits
      : kind === 'position'
        ? positions
        : educationLevels;

  function resetForm() {
    setEditingId(null);
    setEditingVersion(null);
    setName('');
    setIsActive(true);
  }

  function startEdit(item: LookupItem) {
    setEditingId(item.id);
    setEditingVersion(item.version);
    setName(item.name);
    setIsActive(item.isActive);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      const trimmedName = name.trim();

      if (editingId) {
        if (kind === 'org') {
          const existing = rows.find(
            (item) => item.id === editingId,
          ) as OrganizationUnit;
          await api.put(`${config.endpoint}/${editingId}`, {
            name: trimmedName,
            // Šifra se ovde ne uređuje, pa ostaje kakva jeste.
            code: existing.code,
            isActive,
            version: editingVersion,
          });
        } else {
          const existing = rows.find((item) => item.id === editingId) as
            JobPosition | EducationLevel;
          await api.put(`${config.endpoint}/${editingId}`, {
            name: trimmedName,
            sortOrder: existing.sortOrder,
            isActive,
            version: editingVersion,
          });
        }
        toast.success(formatMessage({ id: 'alerts.changesSaved' }));
      } else if (kind === 'org') {
        await api.post(config.endpoint, { name: trimmedName, code: null });
        toast.success(formatMessage({ id: 'alerts.itemAdded' }));
      } else {
        const sortedItems = rows as (JobPosition | EducationLevel)[];
        await api.post(config.endpoint, {
          name: trimmedName,
          sortOrder: nextSortOrder(sortedItems),
        });
        toast.success(formatMessage({ id: 'alerts.itemAdded' }));
      }

      resetForm();
      await onReload();
    } catch (err) {
      if (isEditConflict(err)) {
        toast.warning(formatMessage({ id: 'errors.recordChangedMeanwhile' }));
        resetForm();
        await onReload();
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
    <div className="card admin-lookup-panel">
      <form className="admin-lookup-form" onSubmit={handleSubmit}>
        <div className="form-row">
          <label htmlFor={`${kind}-name`}>
            {formatMessage({ id: 'common.name' })}
          </label>
          <input
            id={`${kind}-name`}
            value={name}
            maxLength={config.maxNameLength}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>
        {editingId && (
          <div className="form-row">
            <label htmlFor={`${kind}-active`}>
              {formatMessage({ id: 'common.active' })}
            </label>
            <select
              id={`${kind}-active`}
              value={isActive ? '1' : '0'}
              onChange={(e) => setIsActive(e.target.value === '1')}
            >
              <option value="1">{formatMessage({ id: 'common.yes' })}</option>
              <option value="0">{formatMessage({ id: 'common.no' })}</option>
            </select>
          </div>
        )}
        <div className="actions admin-lookup-form__actions">
          <button
            type="submit"
            className="btn btn-primary btn-sm"
            disabled={saving}
          >
            {saving
              ? formatMessage({ id: 'buttons.saving' })
              : editingId
                ? formatMessage({ id: 'buttons.save' })
                : formatMessage({ id: 'buttons.add' })}
          </button>
          {editingId && (
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={resetForm}
            >
              {formatMessage({ id: 'buttons.cancel' })}
            </button>
          )}
        </div>
      </form>
      <div className="table-wrap">
        <table className="table table--hover admin-lookup-table">
          <thead>
            <tr>
              <th className="col-text">
                {formatMessage({ id: 'common.name' })}
              </th>
              <th className="col-meta admin-lookup-table__status">
                {formatMessage({ id: 'common.active' })}
              </th>
              <th
                className="col-actions"
                aria-label={formatMessage({ id: 'admin.actions' })}
              />
            </tr>
          </thead>
          <tbody>
            {rows.map((item) => (
              <tr key={item.id}>
                <td className="col-text">{item.name}</td>
                <td className="col-meta admin-lookup-table__status">
                  {item.isActive
                    ? formatMessage({ id: 'common.yes' })
                    : formatMessage({ id: 'common.no' })}
                </td>
                <td className="col-actions">
                  <button
                    type="button"
                    className="btn btn-secondary btn-sm"
                    onClick={() => startEdit(item)}
                  >
                    {formatMessage({ id: 'buttons.edit' })}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
