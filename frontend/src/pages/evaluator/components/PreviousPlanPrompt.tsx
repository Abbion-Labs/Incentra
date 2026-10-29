import type { EvaluationDetail } from '../../../api/types';
import { useIntl } from '../../../i18n';

export type PreviousPlan =
  | { state: 'loading' }
  | { state: 'failed' }
  | { state: 'ready'; detail: EvaluationDetail | null; hasPlan: boolean };

interface CopyPreviousPlanButtonProps {
  quarter: number;
  year: number;
  copying: boolean;
  disabled: boolean;
  onCopy: () => void;
}

/**
 * Preuzimanje plana iz prethodnog kvartala. Popunjava ciljeve, uslove i
 * kriterijume, pa stoji uz zaposlenog i period, a ne u jednoj sekciji.
 */
export function CopyPreviousPlanButton({
  quarter,
  year,
  copying,
  disabled,
  onCopy,
}: CopyPreviousPlanButtonProps) {
  const { formatMessage } = useIntl();

  return (
    <button
      type="button"
      className="btn btn-secondary btn-sm goals-copy-button"
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
