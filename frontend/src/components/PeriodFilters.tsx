import { useId, useMemo } from 'react';

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

  /** Jedan red sa čipovima umesto polja sa labelama, za traku iznad tabele. */

  compact?: boolean;

  /** U kompaktnom režimu: vraća filtere na podrazumevane vrednosti. */

  onReset?: () => void;

  /** Da li su filteri promenjeni, pa ima smisla ponuditi poništavanje. */

  canReset?: boolean;
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

  compact = false,

  onReset,

  canReset = false,
}: Props) {
  const { formatMessage } = useIntl();

  const fieldId = useId();

  const yearOptions = useMemo(() => {
    const years = new Set<number>();
    for (let y = currentYear; y >= currentYear - 4; y -= 1) years.add(y);
    years.add(year);
    return [...years].sort((a, b) => b - a);
  }, [year]);

  const resolvedSearchLabel =
    searchLabel ?? formatMessage({ id: 'evaluation.searchEmployees' });

  const resolvedSearchPlaceholder =
    searchPlaceholder ?? formatMessage({ id: 'admin.searchNamePlaceholder' });

  function handleQuarterChange(value: string) {
    onQuarterChange(value === '' ? null : (Number(value) as 1 | 2 | 3 | 4));
  }

  if (compact) {
    return (
      <div className="filter-bar filter-bar--compact">
        {onSearchChange !== undefined && (
          <div className="form-row filter-bar-search">
            <label htmlFor={`${fieldId}-search`} className="sr-only">
              {resolvedSearchLabel}
            </label>
            <input
              id={`${fieldId}-search`}
              type="search"
              placeholder={resolvedSearchPlaceholder}
              value={search ?? ''}
              onChange={(e) => onSearchChange(e.target.value)}
            />
          </div>
        )}

        <label className="filter-chip">
          <span className="filter-chip__label">
            {formatMessage({ id: 'common.year' })}
          </span>
          <select
            value={year}
            onChange={(e) => onYearChange(Number(e.target.value))}
          >
            {yearOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>

        <label className="filter-chip">
          <span className="filter-chip__label">
            {formatMessage({ id: 'evaluation.quarter' })}
          </span>
          <select
            value={quarter ?? ''}
            onChange={(e) => handleQuarterChange(e.target.value)}
          >
            {showAllQuartersOption && (
              <option value="">
                {formatMessage({ id: 'common.allShort' })}
              </option>
            )}
            {[1, 2, 3, 4].map((q) => (
              <option key={q} value={q}>
                Q{q}
              </option>
            ))}
          </select>
        </label>

        {onReset && (
          // Dugme je uvek u rasporedu (samo sakriveno), pa se traka ne pomera
          // kada se filteri promene.
          <button
            type="button"
            className={`filter-reset${canReset ? '' : ' is-hidden'}`}
            onClick={onReset}
            aria-label={formatMessage({ id: 'common.resetFilters' })}
            title={formatMessage({ id: 'common.resetFilters' })}
            aria-hidden={!canReset || undefined}
            tabIndex={canReset ? undefined : -1}
          >
            <svg
              width="14"
              height="14"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.25"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden
            >
              <path d="M18 6 6 18M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>
    );
  }

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
