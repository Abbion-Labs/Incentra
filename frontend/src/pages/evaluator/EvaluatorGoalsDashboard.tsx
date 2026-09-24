import { useEffect, useMemo, useState } from 'react';

import { useNavigate, useSearchParams } from 'react-router-dom';

import { api } from '../../api/client';

import type { Employee, EvaluationSummary, PagedResult } from '../../api/types';

import { InfiniteScrollSentinel } from '../../components/common/InfiniteScrollSentinel';
import { TableSkeleton } from '../../components/common/LoadingSkeleton';

import { AppLayout } from '../../components/AppLayout';
import { PageIntro } from '../../components/common/PageIntro';

import {
  PeriodFilters,
  currentQuarter,
  currentYear,
} from '../../components/PeriodFilters';

import { useAuth } from '../../auth/AuthContext';
import { useDebouncedSearch, usePagedList, useToast } from '../../hooks';

import { useEvaluationBucketCounts } from '../../hooks/useEvaluationBucketCounts';
import { useIntl } from '../../i18n';

import { type GoalsBucket } from '../../utils/goalsBuckets';

import {
  buildEvaluationsPagePath,
  roleListPath,
} from '../../utils/evaluationApi';

import { GoalsBucketTabs } from './components/GoalsBucketTabs';

import { PlanningEmployeesTable } from './components/PlanningEmployeesTable';

import { SetGoalsTable } from './components/SetGoalsTable';

export function EvaluatorGoalsDashboard() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const navigate = useNavigate();

  const [searchParams, setSearchParams] = useSearchParams();

  const tabFromUrl = searchParams.get('tab') as GoalsBucket | null;

  const initialTab: GoalsBucket = tabFromUrl === 'set' ? 'set' : 'pending';

  const [activeTab, setActiveTab] = useState<GoalsBucket>(initialTab);

  const [creatingFor, setCreatingFor] = useState<number | null>(null);
  // Ocene pravi samo ocenjivač; admin ovde samo gleda.
  const { activeRole } = useAuth();
  const canPlan = activeRole === 'EVALUATOR';

  const [year, setYear] = useState(currentYear);

  const [quarter, setQuarter] = useState(currentQuarter);

  const {
    input: searchInput,
    debounced: search,
    setInput: setSearchInput,
  } = useDebouncedSearch();

  const countsQueryKey = `${year}|${quarter}|${search}`;

  const { counts } = useEvaluationBucketCounts(countsQueryKey, {
    scope: 'evaluator',
    year,
    quarter,
    search,
  });

  const employeesQueryKey = `${activeTab}|${year}|${quarter}|${search}`;

  const {
    items: pendingEmployees,

    loading: loadingEmployees,

    loadingMore: loadingMoreEmployees,

    hasMore: hasMoreEmployees,

    loadMore: loadMoreEmployees,
    error: employeesError,
  } = usePagedList<Employee>({
    queryKey: employeesQueryKey,

    enabled: activeTab === 'pending',

    fetchPage: (page, pageSize) => {
      const params = new URLSearchParams({
        page: String(page),

        pageSize: String(pageSize),

        isActive: 'true',

        goalsBucket: 'pending',

        goalsYear: String(year),

        goalsQuarter: String(quarter),
      });

      if (search.trim()) {
        params.set('search', search.trim());
      }

      return `${roleListPath('employees', 'evaluator')}?${params}`;
    },
  });

  const setGoalsQueryKey = `${year}|${quarter}|${search}|set`;

  const {
    items: setEvaluations,

    loading: loadingSet,

    loadingMore: loadingMoreSet,

    hasMore: hasMoreSet,

    loadMore: loadMoreSet,
    error: setErrorList,
  } = usePagedList<EvaluationSummary>({
    queryKey: setGoalsQueryKey,

    enabled: activeTab === 'set',

    fetchPage: (page, pageSize) =>
      buildEvaluationsPagePath(page, pageSize, {
        scope: 'evaluator',
        year,

        quarter,

        bucket: 'goalscomplete',

        search,
      }),
  });

  useEffect(() => {
    if (tabFromUrl === 'pending' || tabFromUrl === 'set') {
      setActiveTab(tabFromUrl);
    }
  }, [tabFromUrl]);

  useEffect(() => {
    if (employeesError) toast.error(employeesError);
  }, [employeesError, toast]);

  useEffect(() => {
    if (setErrorList) toast.error(setErrorList);
  }, [setErrorList, toast]);

  function handleTabChange(tab: GoalsBucket) {
    setActiveTab(tab);

    setSearchParams({ tab });
  }

  async function startPlanning(employeeId: number) {
    setCreatingFor(employeeId);

    try {
      const result = await api.get<PagedResult<EvaluationSummary>>(
        buildEvaluationsPagePath(1, 1, {
          scope: 'evaluator',
          year,
          quarter,
          employeeId,
        }),
      );

      const existing = result.items?.[0];

      if (existing) {
        navigate(`/evaluator/goals/evaluations/${existing.id}`);
        return;
      }

      const created = await api.post<EvaluationSummary>('/api/evaluations', {
        employeeId,
        year,
        quarter,
      });
      navigate(`/evaluator/goals/evaluations/${created.id}`);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.creationFailed' }),
      );
    } finally {
      setCreatingFor(null);
    }
  }

  const tabCounts = useMemo(
    () => ({
      pending: counts.goalsPending,

      set: counts.goalsComplete,
    }),
    [counts.goalsComplete, counts.goalsPending],
  );

  const loading = activeTab === 'pending' ? loadingEmployees : loadingSet;

  return (
    <AppLayout title={formatMessage({ id: 'evaluation.goalsTitle' })}>
      <PageIntro
        title={formatMessage({ id: 'navigation.evaluatorGoals' })}
        subtitle={formatMessage({ id: 'pageIntro.evaluatorGoalsSubtitle' })}
      />
      <div className="card card--filter">
        <PeriodFilters
          year={year}
          quarter={quarter}
          onYearChange={setYear}
          onQuarterChange={(q) => {
            if (q != null) setQuarter(q as 1 | 2 | 3 | 4);
          }}
          search={searchInput}
          onSearchChange={setSearchInput}
        />
      </div>

      <GoalsBucketTabs
        activeTab={activeTab}
        onTabChange={handleTabChange}
        counts={tabCounts}
      />

      {activeTab === 'pending' ? (
        <div className="card card--flush card--table-fill">
          {loading ? (
            <TableSkeleton rows={4} columns={4} />
          ) : (
            <div className="table-panel">
              <div className="table-wrap table-wrap--infinite">
                <PlanningEmployeesTable
                  employees={pendingEmployees}
                  creatingFor={creatingFor}
                  search={search}
                  onStartPlanning={canPlan ? startPlanning : undefined}
                  evaluationLabel={() =>
                    formatMessage({ id: 'evaluation.setGoals' })
                  }
                />

                <InfiniteScrollSentinel
                  hasMore={hasMoreEmployees}
                  isLoading={loadingMoreEmployees}
                  onLoadMore={loadMoreEmployees}
                />
              </div>
            </div>
          )}
        </div>
      ) : (
        <div className="card card--flush card--table-fill">
          {loading ? (
            <TableSkeleton rows={4} columns={4} />
          ) : (
            <div className="table-panel">
              <div className="table-wrap table-wrap--infinite">
                <SetGoalsTable evaluations={setEvaluations} search={search} />

                <InfiniteScrollSentinel
                  hasMore={hasMoreSet}
                  isLoading={loadingMoreSet}
                  onLoadMore={loadMoreSet}
                />
              </div>
            </div>
          )}
        </div>
      )}
    </AppLayout>
  );
}
