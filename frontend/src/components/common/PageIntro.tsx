import type { ReactNode } from 'react';

interface PageIntroProps {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
}

/** Naslov stranice sa kratkim opisom i opcionim akcijama desno. */
export function PageIntro({ title, subtitle, actions }: PageIntroProps) {
  return (
    <div className="page-intro">
      <div className="page-intro__text">
        <h1 className="page-intro__title">{title}</h1>
        {subtitle && <p className="page-intro__subtitle">{subtitle}</p>}
      </div>
      {actions && <div className="page-intro__actions">{actions}</div>}
    </div>
  );
}
