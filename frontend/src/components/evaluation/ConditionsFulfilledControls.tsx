import { useIntl } from '../../i18n';

interface ConditionsFulfilledToggleProps {
  checked: boolean;
  editable: boolean;
  onChange: (value: boolean) => void;
}

/**
 * Da li su uslovi ispunjeni: izbor Da/Ne u stilu ocene. Ako nisu, ciljevi se
 * ne ocenjuju nego se upisuje obrazloženje.
 */
export function ConditionsFulfilledToggle({
  checked,
  editable,
  onChange,
}: ConditionsFulfilledToggleProps) {
  const { formatMessage } = useIntl();
  const label = formatMessage({ id: 'evaluation.conditionsFulfilledLabel' });
  const options = [
    { value: true, labelKey: 'common.yes' },
    { value: false, labelKey: 'common.no' },
  ] as const;

  return (
    <div className="conditions-choice">
      <span className="conditions-choice__label" id="conditions-choice-label">
        {formatMessage({ id: 'evaluation.conditionsFulfilledShort' })}
      </span>
      <div
        className={`rating-scale conditions-choice__options${editable ? '' : ' rating-scale--readonly'}`}
        role="radiogroup"
        aria-labelledby="conditions-choice-label"
        title={label}
      >
        {options.map((option) => {
          const selected = checked === option.value;
          return (
            <button
              key={String(option.value)}
              type="button"
              role="radio"
              aria-checked={selected}
              className={`rating-scale__option conditions-choice__option${selected ? ' is-selected' : ''}${option.value ? '' : ' is-negative'}`}
              disabled={!editable}
              onClick={() => onChange(option.value)}
            >
              {formatMessage({ id: option.labelKey })}
            </button>
          );
        })}
      </div>
    </div>
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
