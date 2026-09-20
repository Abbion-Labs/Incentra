import { useCallback, useEffect, useState } from 'react';
import { useLocation, useParams, useSearchParams } from 'react-router-dom';
import { api } from '../../api/client';
import type {
  EmployeeEvaluationBenchmarks,
  EvaluationDetail,
} from '../../api/types';
import { CardSkeleton } from '../../components/common/LoadingSkeleton';
import { PageBackLink } from '../../components/common/PageBackLink';
import { AppLayout } from '../../components/AppLayout';
import { EmployeeAvatarUpload } from '../../components/employee/EmployeeAvatarUpload';
import { useToast } from '../../hooks';
import { EvaluationBenchmarkChart } from './components/EvaluationBenchmarkChart';
import { EmployeeQuarterList } from './components/EmployeeQuarterList';
import { SelectedEvaluationPanel } from './components/SelectedEvaluationPanel';
import type { PageBackState } from '../admin/adminNavigation';
import { useIntl } from '../../i18n';
import { formatDate } from '../../utils/formatLocale';

export function EvaluatorEmployeePage() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const { employeeId } = useParams<{ employeeId: string }>();
  const location = useLocation();
  const backState = location.state as PageBackState | null;
  const [searchParams, setSearchParams] = useSearchParams();
  const [benchmarks, setBenchmarks] =
    useState<EmployeeEvaluationBenchmarks | null>(null);
  const [selectedEvaluation, setSelectedEvaluation] =
    useState<EvaluationDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);

  const selectedEvaluationId = searchParams.get('evaluation')
    ? Number(searchParams.get('evaluation'))
    : null;

  const loadBenchmarks = useCallback(async () => {
    if (!employeeId) return;
    setLoading(true);
    try {
      const data = await api.get<EmployeeEvaluationBenchmarks>(
        `/api/employees/${employeeId}/evaluation-benchmarks`,
      );
      setBenchmarks(data);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.loadFailed' }),
      );
    } finally {
      setLoading(false);
    }
  }, [employeeId, formatMessage, toast]);

  useEffect(() => {
    loadBenchmarks();
  }, [loadBenchmarks]);

  useEffect(() => {
    if (!benchmarks || selectedEvaluationId) return;
    if (benchmarks.quarters.length > 0) {
      setSearchParams(
        { evaluation: String(benchmarks.quarters[0].evaluationId) },
        { replace: true },
      );
    }
  }, [benchmarks, selectedEvaluationId, setSearchParams]);

  useEffect(() => {
    if (!selectedEvaluationId) {
      setSelectedEvaluation(null);
      return;
    }

    let cancelled = false;
    setDetailLoading(true);
    api
      .get<EvaluationDetail>(`/api/evaluations/${selectedEvaluationId}`)
      .then((detail) => {
        if (!cancelled) setSelectedEvaluation(detail);
      })
      .catch((e) => {
        if (!cancelled) {
          toast.error(
            e instanceof Error
              ? e.message
              : formatMessage({ id: 'errors.evaluationLoadFailed' }),
          );
          setSelectedEvaluation(null);
        }
      })
      .finally(() => {
        if (!cancelled) setDetailLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [selectedEvaluationId, formatMessage, toast]);

  function handleSelectEvaluation(id: number) {
    setSearchParams({ evaluation: String(id) });
  }

  const selectedQuarter = benchmarks?.quarters.find(
    (q) => q.evaluationId === selectedEvaluationId,
  );

  if (loading && !benchmarks) {
    return (
      <AppLayout title={formatMessage({ id: 'admin.employees' })}>
        <CardSkeleton lines={2} />
        <CardSkeleton lines={8} />
      </AppLayout>
    );
  }

  if (!benchmarks) {
    return (
      <AppLayout title={formatMessage({ id: 'admin.employees' })}>
        <PageBackLink
          to="/evaluator"
          label={formatMessage({ id: 'buttons.backToList' })}
        />
      </AppLayout>
    );
  }

  const { employee, quarters } = benchmarks;

  return (
    <AppLayout title={employee.fullName}>
      {backState?.backTo && (
        <PageBackLink
          to={backState.backTo}
          label={formatMessage({ id: backState.backLabelKey as never })}
        />
      )}

      <div className="employee-profile-layout">
        <aside className="employee-profile-layout__sidebar">
          <div className="card employee-card">
            <div className="employee-card__header">
              <EmployeeAvatarUpload
                employee={employee}
                size="lg"
                onUpdated={(updated) => {
                  setBenchmarks((current) =>
                    current ? { ...current, employee: updated } : current,
                  );
                }}
              />
              <h2 className="employee-card__name">{employee.fullName}</h2>
            </div>
            <dl className="employee-card__meta">
              <div>
                <dt>{formatMessage({ id: 'evaluation.orgUnitShort' })}</dt>
                <dd>{employee.organizationUnitName}</dd>
              </div>
              <div>
                <dt>{formatMessage({ id: 'evaluation.jobPosition' })}</dt>
                <dd>{employee.jobPositionName}</dd>
              </div>
              {employee.educationLevelName && (
                <div>
                  <dt>{formatMessage({ id: 'common.education' })}</dt>
                  <dd>{employee.educationLevelName}</dd>
                </div>
              )}
              {employee.hiredAt && (
                <div>
                  <dt>{formatMessage({ id: 'common.hiredFrom' })}</dt>
                  <dd>{formatDate(employee.hiredAt)}</dd>
                </div>
              )}
            </dl>
          </div>

          <div className="card">
            <h3 className="form-section__title">
              {formatMessage({ id: 'evaluation.quarterlyEvaluations' })}
            </h3>
            <EmployeeQuarterList
              quarters={quarters}
              selectedEvaluationId={selectedEvaluationId}
              onSelect={handleSelectEvaluation}
            />
          </div>
        </aside>

        <div className="employee-profile-layout__main">
          <div className="card card--chart">
            <EvaluationBenchmarkChart
              quarters={quarters}
              selectedYear={selectedQuarter?.year}
              selectedQuarter={selectedQuarter?.quarter}
            />
          </div>

          <SelectedEvaluationPanel
            evaluation={selectedEvaluation}
            loading={detailLoading}
          />
        </div>
      </div>
    </AppLayout>
  );
}
