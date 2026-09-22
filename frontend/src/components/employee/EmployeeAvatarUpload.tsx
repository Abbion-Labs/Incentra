import { useEffect, useRef, useState } from 'react';
import { api, ApiError } from '../../api/client';
import type { Employee } from '../../api/types';
import { useIntl } from '../../i18n';
import { EmployeeAvatar } from './EmployeeAvatar';

interface EmployeeAvatarUploadProps {
  employee: Employee;
  onUpdated: (employee: Employee) => void;
  size?: 'sm' | 'md' | 'lg';
}

export function EmployeeAvatarUpload({
  employee,
  onUpdated,
  size = 'lg',
}: EmployeeAvatarUploadProps) {
  const { formatMessage } = useIntl();
  const rootRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const [menuOpen, setMenuOpen] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!menuOpen) return;

    function handlePointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setMenuOpen(false);
      }
    }

    document.addEventListener('mousedown', handlePointerDown);
    return () => document.removeEventListener('mousedown', handlePointerDown);
  }, [menuOpen]);

  async function handleFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      setError(formatMessage({ id: 'account.selectImageType' }));
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      setError(formatMessage({ id: 'account.imageTooLarge' }));
      return;
    }

    setUploading(true);
    setError('');
    setMenuOpen(false);
    try {
      const formData = new FormData();
      formData.append('file', file);
      const updated = await api.postForm<Employee>(
        `/api/employees/${employee.id}/avatar`,
        formData,
      );
      onUpdated(updated);
    } catch (e) {
      setError(
        e instanceof ApiError
          ? e.message
          : formatMessage({ id: 'account.uploadFailed' }),
      );
    } finally {
      setUploading(false);
    }
  }

  async function handleRemove() {
    if (!employee.avatarUrl) return;
    setUploading(true);
    setError('');
    setMenuOpen(false);
    try {
      const updated = await api.delete<Employee>(
        `/api/employees/${employee.id}/avatar`,
      );
      onUpdated(updated);
    } catch (e) {
      setError(
        e instanceof ApiError
          ? e.message
          : formatMessage({ id: 'account.removeFailed' }),
      );
    } finally {
      setUploading(false);
    }
  }

  return (
    <div className="employee-avatar-upload" ref={rootRef}>
      <button
        type="button"
        className="employee-avatar-upload__trigger"
        onClick={() => setMenuOpen((open) => !open)}
        disabled={uploading}
        title={formatMessage({ id: 'account.avatarTitle' })}
        aria-expanded={menuOpen}
        aria-haspopup="menu"
      >
        <EmployeeAvatar employee={employee} size={size} />
        <span className="employee-avatar-upload__overlay">
          {uploading ? '...' : formatMessage({ id: 'buttons.edit' })}
        </span>
      </button>

      {menuOpen && (
        <div className="employee-avatar-upload__menu" role="menu">
          <button
            type="button"
            role="menuitem"
            className="employee-avatar-upload__menu-item"
            onClick={() => inputRef.current?.click()}
            disabled={uploading}
          >
            {formatMessage({ id: 'account.uploadNewImage' })}
          </button>
          {employee.avatarUrl && (
            <button
              type="button"
              role="menuitem"
              className="employee-avatar-upload__menu-item employee-avatar-upload__menu-item--danger"
              onClick={handleRemove}
              disabled={uploading}
            >
              {formatMessage({ id: 'account.removeImage' })}
            </button>
          )}
        </div>
      )}

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="sr-only"
        onChange={handleFileChange}
      />
      {error && <p className="employee-avatar-upload__error">{error}</p>}
    </div>
  );
}
