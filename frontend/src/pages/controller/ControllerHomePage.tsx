import { useEffect } from 'react';
import type { Employee } from '../../api/types';
import { InfiniteScrollSentinel } from '../../components/common/InfiniteScrollSentinel';
import { AppLayout } from '../../components/AppLayout';
import { ToolbarSearch } from '../../components/common/ToolbarSearch';
import { useDebouncedSearch, usePagedList, useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import { roleListPath } from '../../utils/evaluationApi';
import { EmployeeListTable } from '../evaluator/components/EmployeeListTable';

export function ControllerHomePage() {
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
      return `${roleListPath('employees', 'controller')}?${params}`;
    },
  });

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  return (
    <AppLayout title={formatMessage({ id: 'controller.myEmployeesTitle' })}>
      <div className="card card--flush data-panel">
        <div className="data-panel__toolbar">
          <ToolbarSearch
            id="controller-employee-search"
            label={formatMessage({ id: 'evaluation.searchEmployees' })}
            placeholder={formatMessage({
              id: 'evaluation.employeeSearchPlaceholder',
            })}
            value={searchInput}
            onChange={setSearchInput}
          />
        </div>
        <EmployeeListTable
          employees={employees}
          loading={loading && employees.length === 0}
          search={search}
          profilePath={(id) => `/controller/employees/${id}`}
          emptyDescriptionKey="controller.noEmployeesAssigned"
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
