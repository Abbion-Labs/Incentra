import type { ReactNode } from 'react';

interface FormSectionProps {
  title: string;
  hint?: string;
  actions?: ReactNode;
  /** Sažetak uz naslov (npr. napredak i prosek sekcije). */
  meta?: ReactNode;
  children: ReactNode;
  variant?: 'default' | 'secondary';
}

export function FormSection({
  title,
  hint,
  actions,
  meta,
  children,
  variant = 'default',
}: FormSectionProps) {
  return (
    <section
      className={`form-section card ${variant === 'secondary' ? 'form-section--secondary' : ''}`}
    >
      <div className="form-section__header">
        <div className="form-section__heading">
          <h2 className="form-section__title">{title}</h2>
          {hint && <p className="form-section__hint">{hint}</p>}
        </div>
        {(meta || actions) && (
          <div className="form-section__actions">
            {actions}
            {/* Sažetak uvek na desnoj ivici, isto u svakoj sekciji. */}
            {meta}
          </div>
        )}
      </div>
      <div className="form-section__body">{children}</div>
    </section>
  );
}
