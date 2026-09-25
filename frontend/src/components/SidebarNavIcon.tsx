interface SidebarNavIconProps {
  name: SidebarNavIconName;
}

export type SidebarNavIconName =
  | 'employees'
  | 'goals'
  | 'evaluation'
  | 'analytics'
  | 'evaluators'
  | 'controller'
  | 'my-evaluations'
  | 'admin'
  | 'users'
  | 'id-card'
  | 'hierarchy'
  | 'lookups'
  | 'rating-scale'
  | 'salaries'
  | 'parameters'
  | 'results';

const iconProps = {
  width: 18,
  height: 18,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.75,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  'aria-hidden': true,
};

export function SidebarNavIcon({ name }: SidebarNavIconProps) {
  switch (name) {
    case 'employees':
      return (
        <svg {...iconProps}>
          <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
          <path d="M16 3.13a4 4 0 0 1 0 7.75" />
        </svg>
      );
    case 'goals':
      return (
        <svg {...iconProps}>
          <circle cx="12" cy="12" r="8" />
          <circle cx="12" cy="12" r="4" />
          <path d="M12 2v2" />
          <path d="M12 20v2" />
          <path d="M2 12h2" />
          <path d="M20 12h2" />
        </svg>
      );
    case 'evaluation':
      return (
        <svg {...iconProps}>
          <path d="M9 11l3 3L22 4" />
          <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
        </svg>
      );
    case 'analytics':
      return (
        <svg {...iconProps}>
          <path d="M3 3v18h18" />
          <path d="M7 16v-5" />
          <path d="M12 16V8" />
          <path d="M17 16v-3" />
        </svg>
      );
    case 'evaluators':
      return (
        <svg {...iconProps}>
          <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M19 8v6" />
          <path d="M22 11h-6" />
        </svg>
      );
    case 'controller':
      return (
        <svg {...iconProps}>
          <path d="M12 3l7 4v5c0 4.4-3 8.5-7 9-4-0.5-7-4.6-7-9V7l7-4z" />
          <path d="M9 12l2 2 4-4" />
        </svg>
      );
    case 'my-evaluations':
      return (
        <svg {...iconProps}>
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
          <path d="M14 2v6h6" />
          <path d="M8 13h8" />
          <path d="M8 17h5" />
        </svg>
      );
    case 'admin':
      return (
        <svg {...iconProps}>
          <path d="M12 3l7 3v5c0 4.5-3 8.3-7 10-4-1.7-7-5.5-7-10V6l7-3z" />
          <circle cx="12" cy="11" r="2.5" />
          <path d="M8.5 16.5a4 4 0 0 1 7 0" />
        </svg>
      );
    case 'id-card':
      return (
        <svg {...iconProps}>
          <rect x="3" y="5" width="18" height="14" rx="2" />
          <circle cx="9" cy="11" r="2" />
          <path d="M6.5 16a3 3 0 0 1 5 0" />
          <path d="M14 10h4" />
          <path d="M14 14h3" />
        </svg>
      );
    case 'hierarchy':
      return (
        <svg {...iconProps}>
          <rect x="9" y="3" width="6" height="5" rx="1" />
          <rect x="3" y="16" width="6" height="5" rx="1" />
          <rect x="15" y="16" width="6" height="5" rx="1" />
          <path d="M12 8v4" />
          <path d="M6 16v-2h12v2" />
        </svg>
      );
    case 'users':
      return (
        <svg {...iconProps}>
          <circle cx="12" cy="8" r="4" />
          <path d="M4 21v-1a6 6 0 0 1 9-5.2" />
          <circle cx="18" cy="17" r="2.5" />
          <path d="M18 13.5v1" />
          <path d="M18 19.5v1" />
          <path d="M21.5 17h-1" />
          <path d="M15.5 17h-1" />
        </svg>
      );
    case 'lookups':
      return (
        <svg {...iconProps}>
          <path d="M8 6h13" />
          <path d="M8 12h13" />
          <path d="M8 18h13" />
          <path d="M3 6h.01" />
          <path d="M3 12h.01" />
          <path d="M3 18h.01" />
        </svg>
      );
    case 'rating-scale':
      return (
        <svg {...iconProps}>
          <path d="M12 3l2.6 5.3 5.9.9-4.2 4.1 1 5.8L12 16.4l-5.3 2.7 1-5.8-4.2-4.1 5.9-.9z" />
        </svg>
      );
    case 'salaries':
      return (
        <svg {...iconProps}>
          <rect x="2" y="6" width="20" height="12" rx="2" />
          <circle cx="12" cy="12" r="2.5" />
          <path d="M6 12h.01" />
          <path d="M18 12h.01" />
        </svg>
      );
    case 'parameters':
      return (
        <svg {...iconProps}>
          <path d="M4 21v-7" />
          <path d="M4 10V3" />
          <path d="M12 21v-9" />
          <path d="M12 8V3" />
          <path d="M20 21v-5" />
          <path d="M20 12V3" />
          <path d="M1 14h6" />
          <path d="M9 8h6" />
          <path d="M17 16h6" />
        </svg>
      );
    case 'results':
      return (
        <svg {...iconProps}>
          <rect x="3" y="3" width="18" height="18" rx="2" />
          <path d="M3 9h18" />
          <path d="M3 15h18" />
          <path d="M9 9v12" />
        </svg>
      );
    default:
      return null;
  }
}
