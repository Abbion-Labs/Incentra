import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  badge?: ReactNode;
  meta?: ReactNode;
  children?: ReactNode;
}

export function PageHeader({
  title,
  subtitle,
  badge,
  meta,
  children,
}: PageHeaderProps) {
  return (
    <header className="page-header">
      <div className="page-header__main">
        <div className="page-header__title-row">
          <h2 className="page-header__title">{title}</h2>
          {badge}
        </div>
        {subtitle && <p className="page-header__subtitle">{subtitle}</p>}
        {meta && <div className="page-header__meta">{meta}</div>}
      </div>
      {children && <div className="page-header__aside">{children}</div>}
    </header>
  );
}

export function PeriodPill({
  quarter,
  year,
}: {
  quarter: number;
  year: number;
}) {
  return (
    <span className="period-pill">
      Q{quarter}/{year}
    </span>
  );
}
