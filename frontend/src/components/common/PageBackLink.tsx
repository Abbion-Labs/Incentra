import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

interface PageBackLinkProps {
  to: string;
  label: string;
  children?: ReactNode;
}

export function PageBackLink({ to, label, children }: PageBackLinkProps) {
  return (
    <p className="page-back-link">
      <Link to={to} className="page-back-link__anchor">
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden
        >
          <path d="m15 18-6-6 6-6" />
        </svg>
        {label}
      </Link>
      {children}
    </p>
  );
}
