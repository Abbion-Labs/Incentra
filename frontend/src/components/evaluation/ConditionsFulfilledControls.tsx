import { useIntl } from '../../i18n';

interface ConditionsFulfilledToggleProps {
  checked: boolean;
  editable: boolean;
  onChange: (value: boolean) => void;
}

export function ConditionsFulfilledToggle({
  checked,
  editable,
  onChange,
}: ConditionsFulfilledToggleProps) {
  const { formatMessage } = useIntl();

  return (
    <label className="conditions-fulfilled-toggle">
      <input
        type="checkbox"
        checked={checked}
        disabled={!editable}
        onChange={(e) => onChange(e.target.checked)}
      />
      <span>
        {formatMessage({ id: 'evaluation.conditionsFulfilledLabel' })}
      </span>
    </label>
  );
}

interface ConditionsNotMetCommentFieldProps {
  value: string;
  editable: boolean;
  onChange: (value: string) => void;
}

export function ConditionsNotMetCommentField({
  value,
  editable,
  onChange,
}: ConditionsNotMetCommentFieldProps) {
  const { formatMessage } = useIntl();

  return (
    <div className="form-row">
      <label htmlFor="evaluation-conditions-comment">
        {formatMessage({ id: 'evaluation.conditionsNotMetCommentLabel' })}
      </label>
      <textarea
        id="evaluation-conditions-comment"
        rows={3}
        value={value}
        disabled={!editable}
        onChange={(e) => onChange(e.target.value)}
        placeholder={formatMessage({
          id: 'evaluation.conditionsNotMetCommentPlaceholder',
        })}
      />
    </div>
  );
}
