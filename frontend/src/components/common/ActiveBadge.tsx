import { useIntl } from '../../i18n';

/** Status zapisa u tabelama: ista oznaka „Aktivan“/„Neaktivan“ svuda. */
export function ActiveBadge({ active }: { active: boolean }) {
  const { formatMessage } = useIntl();
  return (
    <span className={`badge ${active ? 'badge-approved' : 'badge-draft'}`}>
      {formatMessage({ id: active ? 'common.active' : 'common.inactive' })}
    </span>
  );
}
