import type { EvaluationBucket } from '../../utils/evaluationBuckets';
import { bucketTabLabelKeys } from '../../utils/evaluationBuckets';
import { useIntl } from '../../i18n';
import { StatusTabIcon } from './StatusTabIcon';

export const evaluationBucketTabs: EvaluationBucket[] = [
  'unrated',
  'returned',
  'submitted',
  'approved',
];

interface EvaluationBucketTabsProps {
  activeTab: string;
  onTabChange: (tab: string) => void;
  tabs?: string[];
  counts?: Partial<Record<string, number>>;
  tabLabels?: Record<string, string>;
}

export function EvaluationBucketTabs({
  activeTab,
  onTabChange,
  tabs = evaluationBucketTabs,
  counts,
  tabLabels = {},
}: EvaluationBucketTabsProps) {
  const { formatMessage } = useIntl();
  return (
    <div className="tabs" role="tablist">
      {tabs.map((tab) => {
        const count = counts?.[tab];
        return (
          <button
            key={tab}
            type="button"
            role="tab"
            aria-selected={activeTab === tab}
            className={`tab ${activeTab === tab ? 'active' : ''}`}
            onClick={() => onTabChange(tab)}
          >
            <StatusTabIcon status={tab} />
            <span className="tab__label">
              {formatMessage({
                id: (tabLabels[tab] ??
                  bucketTabLabelKeys[tab as EvaluationBucket] ??
                  tab) as never,
              })}
            </span>
            {count !== undefined && <span className="tab-count">{count}</span>}
          </button>
        );
      })}
    </div>
  );
}
