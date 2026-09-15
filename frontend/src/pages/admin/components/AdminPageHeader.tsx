import type { ReactNode } from 'react';
import { AlertMessages } from '../../../components/common/AlertMessages';

interface AdminPageHeaderProps {
  error?: string;
  message?: string;
  actions?: ReactNode;
}

export function AdminPageHeader({ error, message, actions }: AdminPageHeaderProps) {
  if (!actions && !error && !message) {
    return null;
  }

  return (
    <div className="admin-page-header">
      {actions}
      <AlertMessages error={error} info={message} />
    </div>
  );
}
