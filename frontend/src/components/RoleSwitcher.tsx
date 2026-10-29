import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react';

/** Linijske ikonice uloga, u istom stilu kao ikonice menija. */
const roleIconPaths: Record<string, string[]> = {
  // Ocenjivač piše ocenu (notes sa olovkom), kontrolor proverava (štit)
  EVALUATOR: [
    'M13.4 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-7.4',
    'M2 6h4',
    'M2 10h4',
    'M2 14h4',
    'M2 18h4',
    'M21.378 5.626a1 1 0 1 0-3.004-3.004l-5.01 5.012a2 2 0 0 0-.506.854l-.837 2.87a.5.5 0 0 0 .62.62l2.87-.837a2 2 0 0 0 .854-.506z',
  ],
  CONTROLLER: [
    'M12 3l7 4v5c0 4.4-3 8.5-7 9-4-0.5-7-4.6-7-9V7l7-4z',
    'm9 12 2 2 4-4',
  ],
  EMPLOYEE: [
    'M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2',
    'M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
  ],
  ADMIN: [
    'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z',
    'M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z',
  ],
  PAYROLL: [
    'M19 7V5a2 2 0 0 0-2-2H5a2 2 0 0 0 0 4h14a1 1 0 0 1 1 1v4h-3a2 2 0 0 0 0 4h3v3a1 1 0 0 1-1 1H5a2 2 0 0 1-2-2V5',
    'M20 12v4h-3a2 2 0 0 1 0-4z',
  ],
};

export function RoleIcon({ role, size = 16 }: { role: string; size?: number }) {
  const paths = roleIconPaths[role];
  if (!paths) return null;
  return (
    <svg
      className="role-icon"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      {paths.map((d) => (
        <path key={d} d={d} />
      ))}
    </svg>
  );
}

interface RoleSwitcherProps {
  roles: string[];
  activeRole: string;
  label: string;
  roleLabel: (role: string) => string;
  onChange: (role: string) => void;
}

/** Izbor aktivne uloge: dugme sa ikonicom i padajuća lista uloga. */
export function RoleSwitcher({
  roles,
  activeRole,
  label,
  roleLabel,
  onChange,
}: RoleSwitcherProps) {
  const [open, setOpen] = useState(false);
  const [focusIndex, setFocusIndex] = useState(0);
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const optionRefs = useRef<(HTMLLIElement | null)[]>([]);
  const listId = useId();

  useEffect(() => {
    if (!open) return;
    function onPointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    }
    document.addEventListener('pointerdown', onPointerDown);
    return () => document.removeEventListener('pointerdown', onPointerDown);
  }, [open]);

  useEffect(() => {
    if (open) optionRefs.current[focusIndex]?.focus();
  }, [open, focusIndex]);

  function openList() {
    setFocusIndex(Math.max(0, roles.indexOf(activeRole)));
    setOpen(true);
  }

  function close() {
    setOpen(false);
    triggerRef.current?.focus();
  }

  function choose(role: string) {
    close();
    if (role !== activeRole) onChange(role);
  }

  function onTriggerKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      openList();
    }
  }

  function onListKeyDown(event: KeyboardEvent<HTMLUListElement>) {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setFocusIndex((i) => (i + 1) % roles.length);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      setFocusIndex((i) => (i - 1 + roles.length) % roles.length);
    } else if (event.key === 'Home') {
      event.preventDefault();
      setFocusIndex(0);
    } else if (event.key === 'End') {
      event.preventDefault();
      setFocusIndex(roles.length - 1);
    } else if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      choose(roles[focusIndex]);
    } else if (event.key === 'Escape' || event.key === 'Tab') {
      event.preventDefault();
      close();
    }
  }

  return (
    <div className="role-switch" ref={rootRef}>
      <button
        ref={triggerRef}
        type="button"
        className="role-switch__trigger"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? listId : undefined}
        aria-label={`${label}: ${roleLabel(activeRole)}`}
        title={label}
        onClick={() => (open ? close() : openList())}
        onKeyDown={onTriggerKeyDown}
      >
        <RoleIcon role={activeRole} />
        <span className="role-switch__name">{roleLabel(activeRole)}</span>
        <svg
          className="role-switch__chevron"
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden
        >
          <path d="m6 9 6 6 6-6" />
        </svg>
      </button>

      {open && (
        <ul
          id={listId}
          className="role-switch__menu"
          role="listbox"
          aria-label={label}
          onKeyDown={onListKeyDown}
        >
          {roles.map((role, index) => {
            const selected = role === activeRole;
            return (
              <li
                key={role}
                ref={(el) => {
                  optionRefs.current[index] = el;
                }}
                role="option"
                aria-selected={selected}
                tabIndex={index === focusIndex ? 0 : -1}
                className={`role-switch__option${selected ? ' is-selected' : ''}`}
                onClick={() => choose(role)}
                onMouseEnter={() => setFocusIndex(index)}
              >
                <span className="role-switch__option-icon">
                  <RoleIcon role={role} size={18} />
                </span>
                <span className="role-switch__option-name">
                  {roleLabel(role)}
                </span>
                {selected && (
                  <svg
                    className="role-switch__check"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.25"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    aria-hidden
                  >
                    <path d="M20 6 9 17l-5-5" />
                  </svg>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
