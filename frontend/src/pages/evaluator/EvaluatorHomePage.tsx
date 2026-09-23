import { useEffect } from 'react';
import type { Employee } from '../../api/types';
import { InfiniteScrollSentinel } from '../../components/common/InfiniteScrollSentinel';
import { AppLayout } from '../../components/AppLayout';
import { useDebouncedSearch, usePagedList, useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import { roleListPath } from '../../utils/evaluationApi';
import { EmployeeListTable } from './components/EmployeeListTable';

export function EvaluatorHomePage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const {
    input: searchInput,
    debounced: search,
    setInput: setSearchInput,
  } = useDebouncedSearch();

  const {
    items: employees,
    loading,
    loadingMore,
    hasMore,
    loadMore,
    error,
  } = usePagedList<Employee>({
    queryKey: search,
    fetchPage: (page, pageSize) => {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
        isActive: 'true',
      });
      if (search.trim()) {
        params.set('search', search.trim());
      }
      return `${roleListPath('employees', 'evaluator')}?${params}`;
    },
  });

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  return (
    <AppLayout title={formatMessage({ id: 'admin.employees' })}>
      <div className="card card--filter">
        <div className="form-row filter-bar-search">
          <label htmlFor="employee-search">
            {formatMessage({ id: 'evaluation.searchEmployees' })}
          </label>
          <input
            id="employee-search"
            type="search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder={formatMessage({
              id: 'evaluation.employeeSearchPlaceholder',
            })}
          />
        </div>
      </div>

      <div className="card card--flush">
        <EmployeeListTable
          employees={employees}
          loading={loading && employees.length === 0}
          search={search}
        />
        <InfiniteScrollSentinel
          hasMore={hasMore}
          isLoading={loadingMore}
          onLoadMore={loadMore}
        />
      </div>
    </AppLayout>
  );
}
