import { useCallback, useEffect, useId, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { EvaluationDetail } from '../../../api/types';
import { useIntl } from '../../../i18n';
import { formatGoalWeight } from '../../../utils/goalsPlanning';

export type PreviousPlan =
  | { state: 'loading' }
  | { state: 'failed' }
  | { state: 'ready'; detail: EvaluationDetail | null; hasPlan: boolean };

const PREVIEW_WIDTH = 352;
const VIEWPORT_PADDING = 8;

interface CopyPreviousPlanButtonProps {
  quarter: number;
  year: number;
  /** Učitan plan za pregled na prelazak mišem; dok se učitava, pregleda nema. */
  detail: EvaluationDetail | null;
  copying: boolean;
  disabled: boolean;
  onCopy: () => void;
}

/**
 * Preuzimanje plana iz prethodnog kvartala. Popunjava ciljeve, uslove i
 * kriterijume, pa stoji uz zaposlenog i period, a ne u jednoj sekciji.
 * Na prelazak mišem ili fokus pokazuje šta će biti preuzeto.
 */
export function CopyPreviousPlanButton({
  quarter,
  year,
  detail,
  copying,
  disabled,
  onCopy,
}: CopyPreviousPlanButtonProps) {
  const { formatMessage } = useIntl();
  const buttonRef = useRef<HTMLButtonElement>(null);
  const previewId = useId();
  const [preview, setPreview] = useState<{ top: number; left: number } | null>(
    null,
  );

  const showPreview = useCallback(() => {
    const button = buttonRef.current;
    if (!button || !detail) return;
    const rect = button.getBoundingClientRect();
    const width = Math.min(
      PREVIEW_WIDTH,
      window.innerWidth - VIEWPORT_PADDING * 2,
    );
    // Poravnato sa desnom ivicom dugmeta, koje stoji na desnoj strani zaglavlja.
    const left = Math.min(
      Math.max(VIEWPORT_PADDING, rect.right - width),
      window.innerWidth - width - VIEWPORT_PADDING,
    );
    setPreview({ top: rect.bottom + 6, left });
  }, [detail]);

  const hidePreview = useCallback(() => setPreview(null), []);

  // Pregled je fiksiran na ekranu, pa se pri skrolovanju sklanja.
  useEffect(() => {
    if (!preview) return;
    window.addEventListener('scroll', hidePreview, true);
    return () => window.removeEventListener('scroll', hidePreview, true);
  }, [preview, hidePreview]);

  return (
    <>
      <button
        ref={buttonRef}
        type="button"
        className="btn btn-secondary btn-sm goals-copy-button"
        onClick={() => {
          hidePreview();
          onCopy();
        }}
        onMouseEnter={showPreview}
        onMouseLeave={hidePreview}
        onFocus={showPreview}
        onBlur={hidePreview}
        disabled={copying || disabled}
        aria-describedby={preview ? previewId : undefined}
      >
        <HistoryIcon />
        {copying
          ? formatMessage({ id: 'common.loading' })
          : formatMessage(
              { id: 'evaluation.planning.copyPreviousPlanLink' },
              { quarter, year },
            )}
      </button>
      {preview &&
        detail &&
        createPortal(
          <div
            id={previewId}
            className="previous-plan-preview"
            style={{ top: preview.top, left: preview.left }}
            role="tooltip"
          >
            <p className="previous-plan-preview__title">
              {formatMessage(
                { id: 'evaluation.planning.previousPlanTitle' },
                { quarter, year },
              )}
            </p>
            <PreviewSection
              label={formatMessage({ id: 'evaluation.goalsTitle' })}
              empty={formatMessage({ id: 'evaluation.goalsNotDefined' })}
              items={detail.goals.map((goal) => ({
                id: goal.id,
                description: goal.description,
                weight: formatGoalWeight(goal.weight),
              }))}
            />
            <PreviewSection
              label={formatMessage({ id: 'evaluation.conditionsTitle' })}
              empty={formatMessage({ id: 'evaluation.noConditions' })}
              items={detail.conditions}
            />
            <PreviewSection
              label={formatMessage({ id: 'evaluation.criteriaTitle' })}
              empty={formatMessage({ id: 'evaluation.noCriteria' })}
              items={detail.criteria}
            />
          </div>,
          document.body,
        )}
    </>
  );
}

function PreviewSection({
  label,
  empty,
  items,
}: {
  label: string;
  empty: string;
  items: { id: number; description: string; weight?: string | null }[];
}) {
  const described = items.filter((item) => item.description.trim());

  return (
    <div className="previous-plan-preview__section">
      <p className="previous-plan-preview__label">{label}</p>
      {described.length > 0 ? (
        <ol>
          {described.map((item) => (
            <li key={item.id}>
              {item.description}
              {item.weight && (
                <span className="previous-plan-preview__weight">
                  {item.weight}
                </span>
              )}
            </li>
          ))}
        </ol>
      ) : (
        <p className="previous-plan-preview__empty">{empty}</p>
      )}
    </div>
  );
}

function HistoryIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="15"
      height="15"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d="M3 12a9 9 0 1 0 3-6.7L3 8" />
      <path d="M3 3v5h5" />
      <path d="M12 7v5l3 2" />
    </svg>
  );
}
