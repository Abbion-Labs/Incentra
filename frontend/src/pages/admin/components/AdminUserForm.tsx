import type { AdminUser } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { roleLabel } from '../../../utils/status';

const ROLE_ORDER = ['EMPLOYEE', 'EVALUATOR', 'CONTROLLER', 'PAYROLL', 'ADMIN'] as const;

export { ROLE_ORDER };

export interface UserFormValues {
  email: string;
  password: string;
  roleCodes: string[];
  isActive: boolean;
}

export const emptyUserForm = (): UserFormValues => ({
  email: '',
  password: '',
  roleCodes: ['EVALUATOR'],
  isActive: true,
});

export function userToForm(user: AdminUser): UserFormValues {
  return {
    email: user.email,
    password: '',
    roleCodes: user.roles.length > 0
      ? ROLE_ORDER.filter((code) => user.roles.includes(code))
      : ['EVALUATOR'],
    isActive: user.isActive,
  };
}

interface AdminUserFormProps {
  values: UserFormValues;
  editingUser: AdminUser | null;
  saving: boolean;
  onChange: (values: UserFormValues) => void;
  onSubmit: () => void;
  onCancel: () => void;
}

export function AdminUserForm({
  values,
  editingUser,
  saving,
  onChange,
  onSubmit,
  onCancel,
}: AdminUserFormProps) {
  const { formatMessage } = useIntl();
  const isEditing = editingUser != null;

  function setField<K extends keyof UserFormValues>(key: K, value: UserFormValues[K]) {
    onChange({ ...values, [key]: value });
  }

  function toggleRole(role: string) {
    const selected = new Set(values.roleCodes);
    if (selected.has(role)) {
      selected.delete(role);
    } else {
      selected.add(role);
    }
    setField('roleCodes', ROLE_ORDER.filter((code) => selected.has(code)));
  }

  return (
    <form
      className="card admin-form"
      onSubmit={(e) => {
        e.preventDefault();
        onSubmit();
      }}
    >
      <div className="form-grid admin-form__grid admin-form__grid--three-cols">
        <div className="form-row">
          <label htmlFor="user-email">{formatMessage({ id: 'common.email' })}</label>
          <input
            id="user-email"
            type="email"
            value={values.email}
            onChange={(e) => setField('email', e.target.value)}
            required
          />
        </div>
        <div className="form-row">
          <label htmlFor="user-password">
            {isEditing ? formatMessage({ id: 'common.newPassword' }) : formatMessage({ id: 'common.password' })}
          </label>
          <input
            id="user-password"
            type="password"
            value={values.password}
            onChange={(e) => setField('password', e.target.value)}
            minLength={isEditing ? 8 : undefined}
            required={!isEditing}
            placeholder={isEditing ? formatMessage({ id: 'admin.users.passwordOptionalPlaceholder' }) : undefined}
          />
        </div>
        {isEditing ? (
          <div className="form-row">
            <label htmlFor="user-active">{formatMessage({ id: 'admin.active' })}</label>
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
        <span className="admin-form__roles-label">{formatMessage({ id: 'admin.roles' })}</span>
        <ul className="admin-form__roles-list" role="group" aria-label={formatMessage({ id: 'admin.roles' })}>
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
                  <span className="admin-form__role-label">{roleLabel(role, formatMessage)}</span>
                </label>
              </li>
            );
          })}
        </ul>
      </div>

      <div className="actions">
        <button type="submit" className="btn btn-primary" disabled={saving || values.roleCodes.length === 0}>
          {saving
            ? formatMessage({ id: 'buttons.saving' })
            : isEditing
              ? formatMessage({ id: 'buttons.saveChanges' })
              : formatMessage({ id: 'admin.users.createUser' })}
        </button>
        {isEditing && (
          <button type="button" className="btn btn-secondary" onClick={onCancel}>
            {formatMessage({ id: 'buttons.cancel' })}
          </button>
        )}
      </div>
    </form>
  );
}
