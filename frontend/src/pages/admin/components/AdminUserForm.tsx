import { useMemo, useState } from 'react';
import type { AdminUser, Employee } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { roleLabel } from '../../../utils/status';
import { NO_CONTROLLER } from '../evaluatorController';
import { TEXT_LIMITS } from '../../../utils/textLimits';

const ROLE_ORDER = [
  'EMPLOYEE',
  'EVALUATOR',
  'CONTROLLER',
  'PAYROLL',
  'ADMIN',
] as const;

export { ROLE_ORDER };

/** Uloge koje rade kao određeni zaposleni, pa nalogu sa njima treba zaposleni. */
const EMPLOYEE_LINKED_ROLES = ['EMPLOYEE', 'EVALUATOR', 'CONTROLLER'];

function needsEmployee(roleCodes: string[]): boolean {
  return roleCodes.some((code) => EMPLOYEE_LINKED_ROLES.includes(code));
}

export interface UserFormValues {
  email: string;
  password: string;
  roleCodes: string[];
  isActive: boolean;
  controllerEmployeeId: string;
  /** Zaposleni kome novi nalog pripada; bira se samo pri pravljenju naloga. */
  employeeId: string;
}

export const emptyUserForm = (): UserFormValues => ({
  email: '',
  password: '',
  // Not EVALUATOR: that role needs an employee link, which a new account
  // does not have yet.
  roleCodes: ['EMPLOYEE'],
  isActive: true,
  controllerEmployeeId: '',
  employeeId: '',
});

export function userToForm(user: AdminUser): UserFormValues {
  return {
    email: user.email,
    password: '',
    roleCodes:
      user.roles.length > 0
        ? ROLE_ORDER.filter((code) => user.roles.includes(code))
        : ['EMPLOYEE'],
    isActive: user.isActive,
    controllerEmployeeId: '',
    employeeId: '',
  };
}

interface AdminUserFormProps {
  values: UserFormValues;
  editingUser: AdminUser | null;
  saving: boolean;
  controllerOptions: Employee[];
  /** Aktivni zaposleni koji još nemaju nalog. */
  employeeOptions: Employee[];
  alreadyConfiguredEvaluator: boolean;
  onChange: (values: UserFormValues) => void;
  onSubmit: () => void;
  onCancel: () => void;
}

export function AdminUserForm({
  values,
  editingUser,
  saving,
  controllerOptions,
  employeeOptions,
  alreadyConfiguredEvaluator,
  onChange,
  onSubmit,
  onCancel,
}: AdminUserFormProps) {
  const { formatMessage } = useIntl();
  const isEditing = editingUser != null;

  const [employeeSearch, setEmployeeSearch] = useState('');

  const wantsEvaluator = values.roleCodes.includes('EVALUATOR');
  // Nov nalog sa ulogom zaposlenog, ocenjivača ili kontrolora odmah dobija
  // zaposlenog. Postojećem nepovezanom nalogu se te uloge ne mogu dodati;
  // uloge koje već ima (od ranije) ostaju.
  const pickEmployee = !isEditing && needsEmployee(values.roleCodes);
  const missingEmployeeLink =
    isEditing &&
    editingUser.employeeId == null &&
    needsEmployee(
      values.roleCodes.filter((code) => !editingUser.roles.includes(code)),
    );
  // Postojeći ocenjivač zadržava dodeljenog kontrolora, pa ga bira samo novi.
  const needsController =
    wantsEvaluator && !missingEmployeeLink && !alreadyConfiguredEvaluator;
  const blocked = missingEmployeeLink || (pickEmployee && !values.employeeId);

  const filteredEmployees = useMemo(() => {
    const term = employeeSearch.trim().toLowerCase();
    return term
      ? employeeOptions.filter((e) => e.fullName.toLowerCase().includes(term))
      : employeeOptions;
  }, [employeeOptions, employeeSearch]);

  // Ocenjivač ne može biti sam sebi kontrolor.
  const availableControllers = controllerOptions.filter(
    (e) => String(e.id) !== values.employeeId,
  );

  function setField<K extends keyof UserFormValues>(
    key: K,
    value: UserFormValues[K],
  ) {
    onChange({ ...values, [key]: value });
  }

  function toggleRole(role: string) {
    const selected = new Set(values.roleCodes);
    if (selected.has(role)) {
      selected.delete(role);
    } else {
      selected.add(role);
    }
    setField(
      'roleCodes',
      ROLE_ORDER.filter((code) => selected.has(code)),
    );
  }

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
          id: isEditing ? 'admin.users.editUser' : 'admin.users.newUser',
        })}
      </h2>
      <div className="form-grid admin-form__grid admin-form__grid--three-cols">
        <div className="form-row">
          <label htmlFor="user-email">
            {formatMessage({ id: 'common.email' })}
          </label>
          <input
            id="user-email"
            type="email"
            value={values.email}
            maxLength={TEXT_LIMITS.email}
            onChange={(e) => setField('email', e.target.value)}
            required
          />
        </div>
        <div className="form-row">
          <label htmlFor="user-password">
            {isEditing
              ? formatMessage({ id: 'common.newPassword' })
              : formatMessage({ id: 'common.password' })}
          </label>
          <input
            id="user-password"
            type="password"
            value={values.password}
            onChange={(e) => setField('password', e.target.value)}
            minLength={isEditing ? 8 : undefined}
            required={!isEditing}
            placeholder={
              isEditing
                ? formatMessage({
                    id: 'admin.users.passwordOptionalPlaceholder',
                  })
                : undefined
            }
          />
        </div>
        {isEditing ? (
          <div className="form-row">
            <label htmlFor="user-active">
              {formatMessage({ id: 'admin.active' })}
            </label>
            <select
              id="user-active"
              value={values.isActive ? '1' : '0'}
              onChange={(e) => setField('isActive', e.target.value === '1')}
            >
              <option value="1">{formatMessage({ id: 'common.yes' })}</option>
              <option value="0">{formatMessage({ id: 'common.no' })}</option>
            </select>
          </div>
        ) : null}
      </div>

      <div className="admin-form__roles">
        <span className="admin-form__roles-label">
          {formatMessage({ id: 'admin.roles' })}
        </span>
        <ul
          className="admin-form__roles-list"
          role="group"
          aria-label={formatMessage({ id: 'admin.roles' })}
        >
          {ROLE_ORDER.map((role) => {
            const selected = values.roleCodes.includes(role);
            return (
              <li key={role}>
                <label
                  className={`admin-form__role-item${selected ? ' is-selected' : ''}`}
                >
                  <input
                    type="checkbox"
                    className="admin-form__role-checkbox"
                    checked={selected}
                    onChange={() => toggleRole(role)}
                  />
                  <span className="admin-form__role-label">
                    {roleLabel(role, formatMessage)}
                  </span>
                </label>
              </li>
            );
          })}
        </ul>
      </div>

      {pickEmployee && (
        <div className="form-row">
          <label htmlFor="user-employee">
            {formatMessage({ id: 'admin.users.employee' })}
          </label>
          <input
            type="search"
            value={employeeSearch}
            onChange={(e) => setEmployeeSearch(e.target.value)}
            placeholder={formatMessage({
              id: 'admin.users.employeeSearchPlaceholder',
            })}
            aria-label={formatMessage({
              id: 'admin.users.employeeSearchPlaceholder',
            })}
          />
          <select
            id="user-employee"
            value={values.employeeId}
            onChange={(e) => setField('employeeId', e.target.value)}
            required
          >
            <option value="">--</option>
            {filteredEmployees.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.fullName} · {employee.organizationUnitName}
              </option>
            ))}
          </select>
          <p className="form-hint">
            {employeeOptions.length === 0
              ? formatMessage({ id: 'admin.users.noEmployeesWithoutAccount' })
              : formatMessage({ id: 'admin.users.employeeHint' })}
          </p>
        </div>
      )}

      {missingEmployeeLink && (
        <p className="alert alert-warning">
          {formatMessage({ id: 'admin.users.rolesNeedLinkedEmployee' })}
        </p>
      )}

      {needsController && (
        <div className="form-row">
          <label htmlFor="user-evaluator-controller">
            {formatMessage({ id: 'admin.users.evaluatorController' })}
          </label>
          <select
            id="user-evaluator-controller"
            value={values.controllerEmployeeId}
            onChange={(e) => setField('controllerEmployeeId', e.target.value)}
            required
          >
            <option value="">--</option>
            <option value={NO_CONTROLLER}>
              {formatMessage({ id: 'admin.evaluatorSettings.noController' })}
            </option>
            {availableControllers.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.fullName}
              </option>
            ))}
          </select>
          <p className="form-hint">
            {formatMessage({ id: 'admin.users.evaluatorControllerHint' })}
          </p>
        </div>
      )}

      <div className="actions">
        <button
          type="submit"
          className="btn btn-primary"
          disabled={
            saving ||
            values.roleCodes.length === 0 ||
            blocked ||
            (needsController && !values.controllerEmployeeId)
          }
        >
          {saving
            ? formatMessage({ id: 'buttons.saving' })
            : isEditing
              ? formatMessage({ id: 'buttons.saveChanges' })
              : formatMessage({ id: 'admin.users.createUser' })}
        </button>
        <button type="button" className="btn btn-secondary" onClick={onCancel}>
          {formatMessage({ id: 'buttons.cancel' })}
        </button>
      </div>
    </form>
  );
}
