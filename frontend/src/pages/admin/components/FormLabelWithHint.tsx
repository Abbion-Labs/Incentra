import { useCallback, useRef, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

interface FormLabelWithHintProps {
  htmlFor: string;
  hint: string;
  children: ReactNode;
}

const TOOLTIP_MAX_WIDTH = 256;
const VIEWPORT_PADDING = 8;

export function FormLabelWithHint({
  htmlFor,
  hint,
  children,
}: FormLabelWithHintProps) {
  const hintRef = useRef<HTMLSpanElement>(null);
  const [tooltip, setTooltip] = useState<{ top: number; left: number } | null>(
    null,
  );

  const showTooltip = useCallback(() => {
    const el = hintRef.current;
    if (!el) return;

    const rect = el.getBoundingClientRect();
    const left = Math.min(
      Math.max(VIEWPORT_PADDING, rect.left),
      window.innerWidth - TOOLTIP_MAX_WIDTH - VIEWPORT_PADDING,
    );

    setTooltip({
      top: rect.bottom + 6,
      left,
    });
  }, []);

  const hideTooltip = useCallback(() => {
    setTooltip(null);
  }, []);

  return (
    <label htmlFor={htmlFor} className="form-label-with-hint">
      <span className="form-label-with-hint__text">{children}</span>
      <span
        ref={hintRef}
        className="form-field-hint"
        tabIndex={0}
        aria-label={hint}
        onMouseEnter={showTooltip}
        onMouseLeave={hideTooltip}
        onFocus={showTooltip}
        onBlur={hideTooltip}
      >
        <span className="form-field-hint__icon" aria-hidden="true">
          i
        </span>
      </span>
      {tooltip &&
        createPortal(
          <span
            className="form-field-hint__tooltip form-field-hint__tooltip--fixed"
            style={{ top: tooltip.top, left: tooltip.left }}
            role="tooltip"
          >
            {hint}
          </span>,
          document.body,
        )}
    </label>
  );
}
