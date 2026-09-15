interface TableSkeletonProps {
  rows?: number;
  columns?: number;
}

export function TableSkeleton({ rows = 5, columns = 4 }: TableSkeletonProps) {
  return (
    <div className="table-wrap" aria-hidden>
      <div className="skeleton-table">
        <div className="skeleton-row skeleton-row--head">
          {Array.from({ length: columns }).map((_, i) => (
            <div key={i} className="skeleton-cell skeleton-cell--short" />
          ))}
        </div>
        {Array.from({ length: rows }).map((_, row) => (
          <div key={row} className="skeleton-row">
            {Array.from({ length: columns }).map((_, col) => (
              <div
                key={col}
                className={`skeleton-cell ${col === 0 ? 'skeleton-cell--long' : 'skeleton-cell--medium'}`}
              />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

export function PageHeaderSkeleton() {
  return (
    <div className="skeleton-block page-header" aria-hidden>
      <div className="skeleton-cell skeleton-cell--title" />
      <div className="skeleton-cell skeleton-cell--subtitle" />
    </div>
  );
}

export function CardSkeleton({ lines = 3 }: { lines?: number }) {
  return (
    <div className="card skeleton-block" aria-hidden>
      <div className="skeleton-cell skeleton-cell--short" style={{ width: '40%', marginBottom: '1rem' }} />
      {Array.from({ length: lines }).map((_, i) => (
        <div key={i} className="skeleton-cell skeleton-cell--long" style={{ marginBottom: '0.5rem' }} />
      ))}
    </div>
  );
}
