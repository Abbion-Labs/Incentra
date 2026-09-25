type TableIconName = 'edit' | 'history';

const paths: Record<TableIconName, React.ReactNode> = {
  edit: (
    <>
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z" />
    </>
  ),
  history: (
    <>
      <path d="M3 12a9 9 0 1 0 3-6.7L3 8" />
      <path d="M3 3v5h5" />
      <path d="M12 7v5l3 2" />
    </>
  ),
};

interface TableIconButtonProps {
  icon: TableIconName;
  /** Naziv akcije: tooltip i naziv za čitače ekrana. */
  label: string;
  onClick: () => void;
  active?: boolean;
}

/** Akcija u redu tabele kao mala ikonica, da red ostane uzak. */
export function TableIconButton({
  icon,
  label,
  onClick,
  active = false,
}: TableIconButtonProps) {
  return (
    <button
      type="button"
      className={`table-icon-button${active ? ' is-active' : ''}`}
      aria-label={label}
      aria-pressed={icon === 'history' ? active : undefined}
      title={label}
      onClick={(event) => {
        // Red tabele može biti klikabilan; akcija ne sme da otvori i njega.
        event.stopPropagation();
        onClick();
      }}
    >
      <svg
        viewBox="0 0 24 24"
        width="16"
        height="16"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.9"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden
      >
        {paths[icon]}
      </svg>
    </button>
  );
}
