import type { ReactNode } from 'react';

interface ToolbarSearchProps {
  id: string;
  /** Naziv polja za čitače ekrana; vizuelno ga zamenjuje placeholder. */
  label: string;
  placeholder: string;
  value: string;
  onChange: (value: string) => void;
  /** Dodatni filteri (npr. FilterChip) desno od pretrage. */
  children?: ReactNode;
}

/** Pretraga (i opcioni filteri) u traci na vrhu kartice sa tabelom. */
export function ToolbarSearch({
  id,
  label,
  placeholder,
  value,
  onChange,
  children,
}: ToolbarSearchProps) {
  return (
    <div className="filter-bar filter-bar--compact">
      <div className="form-row filter-bar-search">
        <label htmlFor={id} className="sr-only">
          {label}
        </label>
        <input
          id={id}
          type="search"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
        />
      </div>
      {children}
    </div>
  );
}

interface FilterChipProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  children: ReactNode;
  id?: string;
  disabled?: boolean;
}

/** Filter u obliku zaobljenog čipa: labela i izabrana vrednost. */
export function FilterChip({
  label,
  value,
  onChange,
  children,
  id,
  disabled = false,
}: FilterChipProps) {
  return (
    <label className={`filter-chip${disabled ? ' is-disabled' : ''}`}>
      <span className="filter-chip__label" aria-hidden>
        {label}
      </span>
      <select
        id={id}
        aria-label={label}
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
      >
        {children}
      </select>
    </label>
  );
}
