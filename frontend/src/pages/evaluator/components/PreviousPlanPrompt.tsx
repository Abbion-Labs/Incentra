import type { EvaluationDetail } from '../../../api/types';
import { useIntl } from '../../../i18n';

export type PreviousPlan =
  | { state: 'loading' }
  | { state: 'failed' }
  | { state: 'ready'; detail: EvaluationDetail | null; hasPlan: boolean };

const countDescribed = (items: { description: string }[]) =>
  items.filter((item) => item.description.trim()).length;

interface PreviousPlanBannerProps {
  quarter: number;
  year: number;
  detail: EvaluationDetail;
  copying: boolean;
  disabled: boolean;
  onCopy: () => void;
}

/** Ponuda da se prazna forma popuni planom iz prethodnog kvartala. */
export function PreviousPlanBanner({
  quarter,
  year,
  detail,
  copying,
  disabled,
  onCopy,
}: PreviousPlanBannerProps) {
  const { formatMessage } = useIntl();

  return (
    <div className="goals-previous-banner">
      <HistoryIcon />
      <span className="goals-previous-banner__text">
        {formatMessage(
          { id: 'evaluation.planning.previousPlanBanner' },
          {
            quarter,
            year,
            goals: countDescribed(detail.goals),
            conditions: countDescribed(detail.conditions),
            criteria: countDescribed(detail.criteria),
          },
        )}
      </span>
      <button
        type="button"
        className="btn btn-secondary btn-sm"
        onClick={onCopy}
        disabled={copying || disabled}
      >
        {copying
          ? formatMessage({ id: 'common.loading' })
          : formatMessage({ id: 'evaluation.planning.copyPreviousPlan' })}
      </button>
    </div>
  );
}

interface CopyPreviousPlanLinkProps {
  quarter: number;
  year: number;
  copying: boolean;
  disabled: boolean;
  onCopy: () => void;
}

/** Preuzimanje prethodnog plana kad forma već ima sadržaj: tihi link uz naslov. */
export function CopyPreviousPlanLink({
  quarter,
  year,
  copying,
  disabled,
  onCopy,
}: CopyPreviousPlanLinkProps) {
  const { formatMessage } = useIntl();

  return (
    <button
      type="button"
      className="goals-copy-link"
      onClick={onCopy}
      disabled={copying || disabled}
    >
      <HistoryIcon />
      {copying
        ? formatMessage({ id: 'common.loading' })
        : formatMessage(
            { id: 'evaluation.planning.copyPreviousPlanLink' },
            { quarter, year },
          )}
    </button>
  );
}

function HistoryIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="16"
      height="16"
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
