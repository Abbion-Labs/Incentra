import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

interface PageBackLinkProps {
  to: string;
  label: string;
  children?: ReactNode;
}

export function PageBackLink({ to, label, children }: PageBackLinkProps) {
  return (
    <p>
      <Link to={to}>← {label}</Link>
      {children}
    </p>
  );
}
