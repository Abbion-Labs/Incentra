import type {
  EducationLevel,
  Employee,
  JobPosition,
  OrganizationUnit,
} from '../../../api/types';
import { useIntl } from '../../../i18n';
import { TEXT_LIMITS } from '../../../utils/textLimits';

export interface EmployeeFormValues {
  firstName: string;
  lastName: string;
  organizationUnitId: string;
  jobPositionId: string;
  educationLevelId: string;
  evaluatorEmployeeId: string;
  hiredAt: string;
  isActive: boolean;
}

export const emptyEmployeeForm = (): EmployeeFormValues => ({
  firstName: '',
  lastName: '',
  organizationUnitId: '',
  jobPositionId: '',
  educationLevelId: '',
  evaluatorEmployeeId: '',
  hiredAt: '',
  isActive: true,
});

export function employeeToForm(employee: Employee): EmployeeFormValues {
  return {
    firstName: employee.firstName,
    lastName: employee.lastName,
    organizationUnitId: String(employee.organizationUnitId),
    jobPositionId: String(employee.jobPositionId),
    educationLevelId: employee.educationLevelId
      ? String(employee.educationLevelId)
      : '',
    evaluatorEmployeeId: employee.evaluatorEmployeeId
      ? String(employee.evaluatorEmployeeId)
      : '',
    hiredAt: employee.hiredAt ?? '',
    isActive: employee.isActive,
  };
}

interface AdminEmployeeFormProps {
  values: EmployeeFormValues;
  orgUnits: OrganizationUnit[];
  positions: JobPosition[];
  educationLevels: EducationLevel[];
  evaluators: Employee[];
  users: { id: number; email: string; employeeId: number | null }[];
  linkedUserId: string;
  /** Nalog koji je zaposleni imao kad je forma otvorena. */
  savedUserId: number | null;
  editingId: number | null;
  saving: boolean;
  onChange: (values: EmployeeFormValues) => void;
  onLinkedUserChange: (userId: string) => void;
  onSubmit: () => void;
  onCancel: () => void;
}

export function AdminEmployeeForm({
  values,
  orgUnits,
  positions,
  educationLevels,
  evaluators,
  users,
  linkedUserId,
  savedUserId,
  editingId,
  saving,
  onChange,
  onLinkedUserChange,
  onSubmit,
  onCancel,
}: AdminEmployeeFormProps) {
  const { formatMessage } = useIntl();
  function setField<K extends keyof EmployeeFormValues>(
    key: K,
    value: EmployeeFormValues[K],
  ) {
    onChange({ ...values, [key]: value });
  }

  // Nalog se ne predaje drugom zaposlenom: postojeći se može samo odvezati
  // (radi ispravke greške), a zaposlenom bez naloga može se povezati slobodan.
  const availableUsers = users.filter((user) => !user.employeeId);
  const savedUser = users.find((user) => user.id === savedUserId);
  const unlinking = savedUserId != null && linkedUserId === '';

  return (
    <form
      className="card admin-form"
      onSubmit={(e) => {
        e.preventDefault();
        onSubmit();
      }}
    >
      <h2 className="admin-form__title">
        {formatMessage({
          id: editingId
            ? 'admin.employeeForm.editEmployee'
            : 'admin.employeeForm.newEmployee',
        })}
      </h2>
      <div className="form-grid admin-form__grid">
        <div className="form-row">
          <label htmlFor="emp-first-name">
            {formatMessage({ id: 'common.firstName' })}
          </label>
          <input
            id="emp-first-name"
            value={values.firstName}
            maxLength={TEXT_LIMITS.personName}
            onChange={(e) => setField('firstName', e.target.value)}
            required
          />
        </div>
        <div className="form-row">
          <label htmlFor="emp-last-name">
            {formatMessage({ id: 'common.lastName' })}
          </label>
          <input
            id="emp-last-name"
            value={values.lastName}
            maxLength={TEXT_LIMITS.personName}
            onChange={(e) => setField('lastName', e.target.value)}
            required
          />
        </div>
        <div className="form-row">
          <label htmlFor="emp-org">
            {formatMessage({ id: 'evaluation.orgUnitShort' })}
          </label>
          <select
            id="emp-org"
            value={values.organizationUnitId}
            onChange={(e) => setField('organizationUnitId', e.target.value)}
            required
          >
            <option value="">
              {formatMessage({ id: 'common.selectPlaceholder' })}
            </option>
            {orgUnits
              .filter((o) => o.isActive)
              .map((o) => (
                <option key={o.id} value={o.id}>
                  {o.name}
                </option>
              ))}
          </select>
        </div>
        <div className="form-row">
          <label htmlFor="emp-position">
            {formatMessage({ id: 'evaluation.jobPosition' })}
          </label>
          <select
            id="emp-position"
            value={values.jobPositionId}
            onChange={(e) => setField('jobPositionId', e.target.value)}
            required
          >
            <option value="">
              {formatMessage({ id: 'common.selectPlaceholder' })}
            </option>
            {positions
              .filter((p) => p.isActive)
              .map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
          </select>
        </div>
        <div className="form-row">
          <label htmlFor="emp-education">
            {formatMessage({ id: 'admin.employeeForm.educationLevel' })}
          </label>
          <select
            id="emp-education"
            value={values.educationLevelId}
            onChange={(e) => setField('educationLevelId', e.target.value)}
            required
          >
            <option value="">
              {formatMessage({ id: 'common.selectPlaceholder' })}
            </option>
            {educationLevels
              .filter((e) => e.isActive)
              .map((e) => (
                <option key={e.id} value={e.id}>
                  {e.name}
                </option>
              ))}
          </select>
        </div>
        <div className="form-row">
          <label htmlFor="emp-evaluator">
            {formatMessage({ id: 'admin.evaluators' })}
          </label>
          <select
            id="emp-evaluator"
            value={values.evaluatorEmployeeId}
            onChange={(e) => setField('evaluatorEmployeeId', e.target.value)}
          >
            <option value="">—</option>
            {evaluators.map((e) => (
              <option key={e.id} value={e.id}>
                {e.fullName}
              </option>
            ))}
          </select>
        </div>
        <div className="form-row">
          <label htmlFor="emp-hired">
            {formatMessage({ id: 'common.hiredFrom' })}
          </label>
          <input
            id="emp-hired"
            type="date"
            value={values.hiredAt}
            onChange={(e) => setField('hiredAt', e.target.value)}
          />
        </div>
        {editingId && (
          <div className="form-row">
            <label htmlFor="emp-active">
              {formatMessage({ id: 'evaluation.status' })}
            </label>
            <select
              id="emp-active"
              value={values.isActive ? '1' : '0'}
              onChange={(e) => setField('isActive', e.target.value === '1')}
            >
              <option value="1">
                {formatMessage({ id: 'common.active' })}
              </option>
              <option value="0">
                {formatMessage({ id: 'common.inactive' })}
              </option>
            </select>
          </div>
        )}
        {editingId && (
          <div className="form-row">
            <label htmlFor="emp-user">
              {formatMessage({ id: 'common.linkedAccount' })}
            </label>
            {savedUserId != null ? (
              <div className="admin-form__linked-account">
                <p className="admin-form__static-value" id="emp-user">
                  {unlinking ? (
                    <s>{savedUser?.email ?? '—'}</s>
                  ) : (
                    (savedUser?.email ?? '—')
                  )}
                </p>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() =>
                    onLinkedUserChange(unlinking ? String(savedUserId) : '')
                  }
                >
                  {unlinking
                    ? formatMessage({ id: 'buttons.cancel' })
                    : formatMessage({ id: 'admin.employeeForm.unlinkAccount' })}
                </button>
                <p className="form-hint">
                  {formatMessage({
                    id: unlinking
                      ? 'admin.employeeForm.unlinkOnSave'
                      : 'admin.employeeForm.unlinkHint',
                  })}
                </p>
              </div>
            ) : (
              <>
                <select
                  id="emp-user"
                  value={linkedUserId}
                  onChange={(e) => onLinkedUserChange(e.target.value)}
                >
                  <option value="">—</option>
                  {availableUsers.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.email}
                    </option>
                  ))}
                </select>
              </>
            )}
          </div>
        )}
      </div>
      <div className="actions">
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving
            ? formatMessage({ id: 'buttons.saving' })
            : editingId
              ? formatMessage({ id: 'buttons.saveChanges' })
              : formatMessage({ id: 'admin.employeeForm.addEmployee' })}
        </button>
        <button type="button" className="btn btn-secondary" onClick={onCancel}>
          {formatMessage({ id: 'buttons.cancel' })}
        </button>
      </div>
    </form>
  );
}
