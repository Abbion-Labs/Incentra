import { useIntl } from '../../../i18n';

interface CopyFromPreviousQuarterButtonProps {
  copying: boolean;
  saving: boolean;
  lookupsLoading: boolean;
  onClick: () => void;
}

export function CopyFromPreviousQuarterButton({
  copying,
  saving,
  lookupsLoading,
  onClick,
}: CopyFromPreviousQuarterButtonProps) {
  const { formatMessage } = useIntl();
  return (
    <button
      type="button"
      className="btn btn-secondary btn-sm"
      onClick={onClick}
      disabled={copying || saving || lookupsLoading}
    >
      {copying
        ? formatMessage({ id: 'common.loading' })
        : formatMessage({ id: 'evaluation.copyFromPreviousQuarter' })}
    </button>
  );
}
