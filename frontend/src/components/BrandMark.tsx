interface BrandMarkProps {
  size?: number;
}

export function BrandMark({ size = 22 }: BrandMarkProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d="M4 17l5.25-5.25 3.5 3.5L18.75 9.25" />
      <circle cx="18.75" cy="9.25" r="2.25" fill="currentColor" stroke="none" />
    </svg>
  );
}
