import type { ReactNode } from 'react';

interface PlanItem {
  id: number;
  description: string;
}

interface PlanItemListProps<T extends PlanItem> {
  items: T[];
  /** Dodatak desno od stavke (npr. ponder cilja). */
  renderAside?: (item: T) => ReactNode;
}

/** Stavke plana (ciljevi, uslovi, kriterijumi), numerisane kao merila. */
export function PlanItemList<T extends PlanItem>({
  items,
  renderAside,
}: PlanItemListProps<T>) {
  return (
    <ol className="plan-item-list">
      {items.map((item, index) => (
        <li key={item.id}>
          <span className="plan-item-list__index" aria-hidden>
            {index + 1}
          </span>
          <span className="plan-item-list__text">{item.description}</span>
          {renderAside?.(item)}
        </li>
      ))}
    </ol>
  );
}
