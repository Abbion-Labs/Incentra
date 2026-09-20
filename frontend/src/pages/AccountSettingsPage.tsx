import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { syncUserEmployeeProfile } from '../auth/syncUserEmployeeProfile';
import type { Employee } from '../api/types';
import { AppLayout } from '../components/AppLayout';
import { EmployeeAvatarUpload } from '../components/employee/EmployeeAvatarUpload';
import { useToast } from '../hooks';
import { useIntl } from '../i18n';
import { roleLabel } from '../utils/status';

export function AccountSettingsPage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const { user, updateEmployeeProfile, updateNotificationPreferences } = useAuth();
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [loading, setLoading] = useState(true);

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [changingPassword, setChangingPassword] = useState(false);
  const [savingNotifications, setSavingNotifications] = useState(false);

  const loadEmployee = useCallback(async () => {
    if (!user?.employeeId) {
      setEmployee(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const data = await api.get<Employee>(`/api/employees/${user.employeeId}`);
      setEmployee(data);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'account.loadProfileError' }));
    } finally {
      setLoading(false);
    }
  }, [user?.employeeId, formatMessage, toast]);

  useEffect(() => {
    loadEmployee();
  }, [loadEmployee]);

  async function handlePasswordChange(e: React.FormEvent) {
    e.preventDefault();

    if (newPassword !== confirmPassword) {
      toast.error(formatMessage({ id: 'account.passwordMismatch' }));
      return;
    }

    setChangingPassword(true);
    try {
      await api.put('/api/auth/change-password', {
        currentPassword,
        newPassword,
      });
      toast.success(formatMessage({ id: 'account.passwordChanged' }));
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (e) {
      toast.error(e instanceof Error ? e.message : formatMessage({ id: 'account.passwordChangeFailed' }));
    } finally {
      setChangingPassword(false);
    }
  }

  async function handleNotificationToggle(enabled: boolean) {
    setSavingNotifications(true);
    try {
      await updateNotificationPreferences(enabled);
      toast.success(formatMessage({ id: 'account.emailNotificationsUpdated' }));
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'account.emailNotificationsUpdateFailed' }),
      );
    } finally {
      setSavingNotifications(false);
    }
  }

  const primaryRole = user?.roles[0];

  return (
    <AppLayout title={formatMessage({ id: 'account.title' })}>
      <div className="account-settings">
        <div className="card account-settings__profile">
          <h2 className="account-settings__section-title">{formatMessage({ id: 'account.profile' })}</h2>
          {loading ? (
            <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
          ) : employee ? (
            <>
              <div className="account-settings__avatar-row">
                <EmployeeAvatarUpload
                  employee={employee}
                  size="lg"
                  onUpdated={(updated) => {
                    setEmployee(updated);
                    syncUserEmployeeProfile(updated, updateEmployeeProfile);
                  }}
                />
                <div className="account-settings__identity">
                  <p className="account-settings__name">{employee.fullName}</p>
                  <p className="account-settings__meta">{user?.email}</p>
                </div>
              </div>
              <dl className="account-settings__details">
                <div>
                  <dt>{formatMessage({ id: 'account.organizationUnit' })}</dt>
                  <dd>{employee.organizationUnitName}</dd>
                </div>
                <div>
                  <dt>{formatMessage({ id: 'account.jobPosition' })}</dt>
                  <dd>{employee.jobPositionName}</dd>
                </div>
              </dl>
            </>
          ) : (
            <div className="account-settings__no-employee">
              <p className="account-settings__name">{user?.email}</p>
              {primaryRole && (
                <span className="badge account-settings__role">{roleLabel(primaryRole, formatMessage)}</span>
              )}
              <p className="card__hint">
                {formatMessage({ id: 'account.unlinkedInfo' })}
              </p>
            </div>
          )}
        </div>

        <div className="card account-settings__notifications">
          <h2 className="account-settings__section-title">{formatMessage({ id: 'account.notifications' })}</h2>
          <label className="account-settings__toggle-row">
            <span className="account-settings__toggle-copy">
              <span className="account-settings__toggle-label">{formatMessage({ id: 'account.emailNotifications' })}</span>
              <span className="account-settings__toggle-hint">{formatMessage({ id: 'account.emailNotificationsDescription' })}</span>
            </span>
            <input
              type="checkbox"
              className="toggle-switch"
              role="switch"
              checked={user?.emailNotificationsEnabled ?? false}
              disabled={savingNotifications || !user}
              onChange={(e) => void handleNotificationToggle(e.target.checked)}
              aria-label={formatMessage({ id: 'account.emailNotifications' })}
            />
          </label>
        </div>

        <div className="card account-settings__password">
          <h2 className="account-settings__section-title">{formatMessage({ id: 'account.passwordChange' })}</h2>
          <form className="form-grid account-settings__password-form" onSubmit={handlePasswordChange}>
            <div className="form-row">
              <label htmlFor="current-password">{formatMessage({ id: 'common.currentPassword' })}</label>
              <input
                id="current-password"
                type="password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
            </div>
            <div className="form-row">
              <label htmlFor="new-password">{formatMessage({ id: 'common.newPassword' })}</label>
              <input
                id="new-password"
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                required
                minLength={8}
                autoComplete="new-password"
              />
            </div>
            <div className="form-row">
              <label htmlFor="confirm-password">{formatMessage({ id: 'common.confirmPassword' })}</label>
              <input
                id="confirm-password"
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
                minLength={8}
                autoComplete="new-password"
              />
            </div>
            <div className="actions">
              <button type="submit" className="btn btn-primary" disabled={changingPassword}>
                {changingPassword ? formatMessage({ id: 'buttons.saving' }) : formatMessage({ id: 'buttons.changePassword' })}
              </button>
            </div>
          </form>
        </div>
      </div>
    </AppLayout>
  );
}
