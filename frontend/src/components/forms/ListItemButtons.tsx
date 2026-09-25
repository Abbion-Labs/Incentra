import { useIntl } from '../../i18n';

/** Dodavanje stavke u listu: isprekidano polje preko cele širine. */
export function AddItemButton({
  label,
  onClick,
}: {
  label: string;
  onClick: () => void;
}) {
  return (
    <button type="button" className="list-add-button" onClick={onClick}>
      <svg
        viewBox="0 0 24 24"
        width="16"
        height="16"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        aria-hidden
      >
        <path d="M12 5v14M5 12h14" />
      </svg>
      {label}
    </button>
  );
}

/** Uklanjanje stavke: mala ikonica pored polja. */
export function RemoveItemButton({ onClick }: { onClick: () => void }) {
  const { formatMessage } = useIntl();
  const label = formatMessage({ id: 'common.removeItem' });
  return (
    <button
      type="button"
      className="list-remove-button"
      onClick={onClick}
      aria-label={label}
      title={label}
    >
      <svg
        viewBox="0 0 24 24"
        width="16"
        height="16"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        aria-hidden
      >
        <path d="M18 6 6 18M6 6l12 12" />
      </svg>
    </button>
  );
}
