import type { ReactNode } from 'react';

interface AdminPageHeaderProps {
  actions?: ReactNode;
}

export function AdminPageHeader({ actions }: AdminPageHeaderProps) {
  if (!actions) {
    return null;
  }

  return (
    <div className="admin-page-header">
      {actions}
    </div>
  );
}
