/** Linijske ikonice za tabove statusa, u istom stilu kao ikonice menija. */
const statusIconPaths: Record<string, string[]> = {
  // Neocenjene: ocena čeka na ocenjivača
  unrated: ['M12 20h9', 'M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z'],
  // Vraćene: kontrolor je vratio ocenu na doradu
  returned: ['M9 14 4 9l5-5', 'M4 9h10.5a5.5 5.5 0 0 1 0 11H11'],
  // Poslate: čekaju odluku kontrolora
  submitted: ['m22 2-7 20-4-9-9-4Z', 'M22 2 11 13'],
  // Kod kontrolora: ocene koje čekaju pregled
  pending: ['M12 6v6l4 2', 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z'],
  // Odobrene
  approved: ['M22 11.08V12a10 10 0 1 1-5.93-9.14', 'M22 4 12 14.01l-3-3'],
  rejected: [
    'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z',
    'm15 9-6 6',
    'm9 9 6 6',
  ],
  // Ciljevi: zaposleni kojima ciljevi tek treba da se postave
  planning: [
    'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z',
    'M12 18a6 6 0 1 0 0-12 6 6 0 0 0 0 12z',
    'M12 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4z',
  ],
  // Ciljevi: postavljeni
  set: [
    'M9 11l3 3L22 4',
    'M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11',
  ],
};

export function StatusTabIcon({ status }: { status: string }) {
  const paths = statusIconPaths[status];
  if (!paths) return null;
  return (
    <svg
      className="tab__icon"
      width="16"
      height="16"
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
