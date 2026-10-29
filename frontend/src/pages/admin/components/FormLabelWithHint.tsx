import type { ReactNode } from 'react';
import { FieldHint } from '../../../components/common/FieldHint';

interface FormLabelWithHintProps {
  htmlFor: string;
  hint: string;
  children: ReactNode;
}

export function FormLabelWithHint({
  htmlFor,
  hint,
  children,
}: FormLabelWithHintProps) {
  return (
    <label htmlFor={htmlFor} className="form-label-with-hint">
      <span className="form-label-with-hint__text">{children}</span>
      <FieldHint hint={hint} />
    </label>
  );
}
