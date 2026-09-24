import { useId } from 'react';

import { currentQuarter, currentYear } from '../utils/status';

import { useIntl } from '../i18n';

interface Props {
  year: number;

  quarter: number | null;

  onYearChange: (year: number) => void;

  onQuarterChange: (quarter: number | null) => void;

  showAllQuartersOption?: boolean;

  search?: string;

  onSearchChange?: (value: string) => void;

  searchLabel?: string;

  searchPlaceholder?: string;
}

export function PeriodFilters({
  year,

  quarter,

  onYearChange,

  onQuarterChange,

  showAllQuartersOption = false,

  search,

  onSearchChange,

  searchLabel,

  searchPlaceholder,
}: Props) {
  const { formatMessage } = useIntl();

  const fieldId = useId();

  const resolvedSearchLabel =
    searchLabel ?? formatMessage({ id: 'evaluation.searchEmployees' });

  const resolvedSearchPlaceholder =
    searchPlaceholder ?? formatMessage({ id: 'admin.searchNamePlaceholder' });

  return (
    <div className="filter-bar">
      {onSearchChange !== undefined && (
        <div className="form-row filter-bar-search">
          <label htmlFor={`${fieldId}-search`}>{resolvedSearchLabel}</label>

          <input
            id={`${fieldId}-search`}
            type="search"

            placeholder={resolvedSearchPlaceholder}

            value={search ?? ''}

            onChange={(e) => onSearchChange(e.target.value)}
          />
        </div>
      )}

      <div className="filter-bar-period">
        <div className="form-row">
          <label htmlFor={`${fieldId}-year`}>
            {formatMessage({ id: 'common.year' })}
          </label>

          <input
            id={`${fieldId}-year`}
            type="number"
            value={year}
            onChange={(e) => onYearChange(Number(e.target.value))}
          />
        </div>

        <div className="form-row">
          <label htmlFor={`${fieldId}-quarter`}>
            {formatMessage({ id: 'evaluation.quarter' })}
          </label>

          <select
            id={`${fieldId}-quarter`}
            className={
              showAllQuartersOption
                ? 'period-filter-quarter period-filter-quarter--wide'
                : 'period-filter-quarter'
            }
            value={quarter ?? ''}
            onChange={(e) => {
              const value = e.target.value;

              onQuarterChange(
                value === '' ? null : (Number(value) as 1 | 2 | 3 | 4),
              );
            }}
          >
            {showAllQuartersOption && (
              <option value="">
                {formatMessage({ id: 'common.allQuarters' })}
              </option>
            )}

            {[1, 2, 3, 4].map((q) => (
              <option key={q} value={q}>
                Q{q}
              </option>
            ))}
          </select>
        </div>
      </div>
    </div>
  );
}

export { useDebouncedSearch } from '../hooks/useDebouncedSearch';

export { currentYear, currentQuarter };
