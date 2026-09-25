import type { GoalsBucket } from '../../../utils/goalsBuckets';

import { goalsBucketLabels } from '../../../utils/goalsBuckets';

import { useIntl } from '../../../i18n';

import { StatusTabIcon } from '../../../components/evaluation/StatusTabIcon';

const tabs: GoalsBucket[] = ['pending', 'set'];

interface GoalsBucketTabsProps {
  activeTab: GoalsBucket;

  onTabChange: (tab: GoalsBucket) => void;

  counts?: Partial<Record<GoalsBucket, number>>;
}

export function GoalsBucketTabs({
  activeTab,
  onTabChange,
  counts,
}: GoalsBucketTabsProps) {
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
            <StatusTabIcon status={tab === 'pending' ? 'planning' : 'set'} />

            <span className="tab__label">
              {formatMessage({ id: goalsBucketLabels[tab] as never })}
            </span>

            {count !== undefined && <span className="tab-count">{count}</span>}
          </button>
        );
      })}
    </div>
  );
}
