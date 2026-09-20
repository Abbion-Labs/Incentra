import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from '../../api/client';
import type {
  CompensationCalculationStatus,
  CompensationParameters,
  OrganizationUnit,
} from '../../api/types';
import { currentYear } from '../../utils/status';
import {
  COMPENSATION_FIELD_HINT_KEYS,
  DEFAULT_COMPENSATION_PARAMS,
  DEFAULT_PREVIEW_PROFILE,
} from './compensationFormDefaults';
import { previewCompensationByRating } from './compensationPreview';
import { CompensationRatingPreviewChart } from './components/CompensationRatingPreviewChart';
import { useToast } from '../../hooks';
import { useIntl } from '../../i18n';
import { FormLabelWithHint } from './components/FormLabelWithHint';

interface CompensationParamsForm {
  monetaryPool: string;
  currency: string;
  acceptablePerformanceRating: string;
  dependencyWeight: string;
  exponent: string;
  allowNegativeVariable: boolean;
}

interface PreviewProfileForm {
  referencePoints: string;
  referenceSalaryPerPoint: string;
}

const UPPER_LIMIT_COEFFICIENT = 0.25;

function paramsToForm(params: CompensationParameters): CompensationParamsForm {
  return {
    monetaryPool: String(params.monetaryPool),
    currency: params.currency,
    acceptablePerformanceRating: String(params.acceptablePerformanceRating),
    dependencyWeight: String(params.dependencyWeight),
    exponent: String(params.exponent),
    allowNegativeVariable: params.allowNegativeVariable,
  };
}

export function AdminCompensation() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [orgUnits, setOrgUnits] = useState<OrganizationUnit[]>([]);
  const [organizationUnitId, setOrganizationUnitId] = useState('');
  const [year, setYear] = useState(String(currentYear));
  const [form, setForm] = useState<CompensationParamsForm>({
    ...DEFAULT_COMPENSATION_PARAMS,
  });
  const [previewProfile, setPreviewProfile] = useState<PreviewProfileForm>({
    ...DEFAULT_PREVIEW_PROFILE,
  });
  const [existingId, setExistingId] = useState<number | null>(null);
  const [calculationStatus, setCalculationStatus] =
    useState<CompensationCalculationStatus | null>(null);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [calculating, setCalculating] = useState(false);
  const [calculateAllowNegative, setCalculateAllowNegative] = useState(false);

  const loadOrgUnits = useCallback(async () => {
    const units = await api.get<OrganizationUnit[]>('/api/organization-units');
    setOrgUnits(units);
    if (units.length > 0) {
      setOrganizationUnitId((current) => current || String(units[0].id));
    }
  }, []);

  const loadCalculationStatus = useCallback(async (parametersId: number) => {
    try {
      const status = await api.get<CompensationCalculationStatus>(
        `/api/compensation-parameters/${parametersId}/calculation-status`,
      );
      setCalculationStatus(status);
    } catch {
      setCalculationStatus(null);
    }
  }, []);

  const loadParameters = useCallback(
    async (orgId: string, selectedYear: string) => {
      if (!orgId || !selectedYear) return;

      setLoading(true);
      try {
        const params = new URLSearchParams({
          organizationUnitId: orgId,
          year: selectedYear,
        });
        const data = await api.get<CompensationParameters[]>(
          `/api/compensation-parameters?${params}`,
        );
        if (data.length > 0) {
          setExistingId(data[0].id);
          setForm(paramsToForm(data[0]));
          setCalculateAllowNegative(data[0].allowNegativeVariable);
          await loadCalculationStatus(data[0].id);
        } else {
          setExistingId(null);
          setCalculationStatus(null);
          setForm({ ...DEFAULT_COMPENSATION_PARAMS });
          setCalculateAllowNegative(
            DEFAULT_COMPENSATION_PARAMS.allowNegativeVariable,
          );
        }
      } catch (e) {
        toast.error(
          e instanceof Error
            ? e.message
            : formatMessage({ id: 'errors.compensationParamsLoadFailed' }),
        );
      } finally {
        setLoading(false);
      }
    },
    [loadCalculationStatus, formatMessage, toast],
  );

  useEffect(() => {
    loadOrgUnits().catch((e) => {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.generic' }),
      );
      setLoading(false);
    });
  }, [loadOrgUnits, formatMessage, toast]);

  useEffect(() => {
    if (organizationUnitId) {
      loadParameters(organizationUnitId, year);
    }
  }, [organizationUnitId, year, loadParameters]);

  const previewPoints = useMemo(() => {
    const acceptablePerformanceRating = Number(
      form.acceptablePerformanceRating,
    );
    const dependencyWeight = Number(form.dependencyWeight);
    const exponent = Number(form.exponent);
    const referencePoints = Number(previewProfile.referencePoints);
    const referenceSalaryPerPoint = Number(
      previewProfile.referenceSalaryPerPoint,
    );

    if (
      !Number.isFinite(acceptablePerformanceRating) ||
      !Number.isFinite(dependencyWeight) ||
      dependencyWeight <= 0 ||
      !Number.isFinite(exponent) ||
      exponent <= 0 ||
      !Number.isFinite(referencePoints) ||
      referencePoints <= 0 ||
      !Number.isFinite(referenceSalaryPerPoint) ||
      referenceSalaryPerPoint <= 0
    ) {
      return [];
    }

    return previewCompensationByRating({
      acceptablePerformanceRating,
      dependencyWeight,
      exponent,
      allowNegativeVariable: form.allowNegativeVariable,
      referencePoints,
      referenceSalaryPerPoint,
    });
  }, [form, previewProfile]);

  function updateField<K extends keyof CompensationParamsForm>(
    key: K,
    value: CompensationParamsForm[K],
  ) {
    setForm((current) => {
      const next = { ...current, [key]: value };
      if (key === 'allowNegativeVariable') {
        setCalculateAllowNegative(value as boolean);
      }
      return next;
    });
  }

  function updatePreviewField<K extends keyof PreviewProfileForm>(
    key: K,
    value: PreviewProfileForm[K],
  ) {
    setPreviewProfile((current) => ({ ...current, [key]: value }));
  }

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    if (!organizationUnitId) {
      toast.warning(formatMessage({ id: 'errors.selectOrganizationUnit' }));
      return;
    }

    setSaving(true);
    try {
      const payload = {
        monetaryPool: Number(form.monetaryPool),
        currency: form.currency.trim().toUpperCase(),
        acceptablePerformanceRating: Number(form.acceptablePerformanceRating),
        upperLimitCoefficient: UPPER_LIMIT_COEFFICIENT,
        dependencyWeight: Number(form.dependencyWeight),
        exponent: Number(form.exponent),
        allowNegativeVariable: form.allowNegativeVariable,
      };

      if (existingId) {
        await api.put(`/api/compensation-parameters/${existingId}`, {
          ...payload,
          isActive: true,
        });
        toast.success(formatMessage({ id: 'alerts.parametersSaved' }));
      } else {
        const created = await api.post<CompensationParameters>(
          '/api/compensation-parameters',
          {
            organizationUnitId: Number(organizationUnitId),
            year: Number(year),
            ...payload,
          },
        );
        setExistingId(created.id);
        toast.success(formatMessage({ id: 'alerts.parametersCreated' }));
      }

      await loadParameters(organizationUnitId, year);
    } catch (err) {
      toast.error(
        err instanceof Error
          ? err.message
          : formatMessage({ id: 'errors.saveFailed' }),
      );
    } finally {
      setSaving(false);
    }
  }

  async function handleCalculate() {
    if (!existingId) {
      toast.warning(
        formatMessage({ id: 'errors.saveParametersBeforeCalculation' }),
      );
      return;
    }

    setCalculating(true);
    try {
      const response = await api.post<{
        employeesCalculated: number;
        employeesSkipped: number;
        warnings: string[];
      }>(`/api/compensation-parameters/${existingId}/calculate`, {
        isFinal: false,
        requireAllQuarters: false,
        allowNegativeVariable: calculateAllowNegative,
      });
      const count = response.employeesCalculated;
      toast.success(
        formatMessage(
          {
            id:
              count > 0
                ? 'alerts.calculationCompletedWithResults'
                : 'alerts.calculationCompleted',
          },
          { count },
        ),
      );
      if (existingId) {
        await loadCalculationStatus(existingId);
      }
    } catch (err) {
      toast.error(
        err instanceof Error
          ? err.message
          : formatMessage({ id: 'errors.calculationFailed' }),
      );
    } finally {
      setCalculating(false);
    }
  }

  const isFinalized = calculationStatus?.isFinalized ?? false;

  const yearOptions = useMemo(() => {
    const base = currentYear;
    return [base - 1, base, base + 1];
  }, []);

  return (
    <div className="card compensation-params">
      <div className="compensation-params__layout">
        <form
          className="admin-form compensation-params__form"
          onSubmit={handleSave}
        >
          <div className="form-grid admin-form__grid compensation-params__grid">
            <div className="form-row">
              <label htmlFor="comp-org">
                {formatMessage({ id: 'common.organizationUnit' })}
              </label>
              <select
                id="comp-org"
                value={organizationUnitId}
                onChange={(e) => setOrganizationUnitId(e.target.value)}
                required
              >
                {orgUnits.map((unit) => (
                  <option key={unit.id} value={unit.id}>
                    {unit.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-row">
              <label htmlFor="comp-year">
                {formatMessage({ id: 'common.year' })}
              </label>
              <select
                id="comp-year"
                value={year}
                onChange={(e) => setYear(e.target.value)}
                required
              >
                {yearOptions.map((y) => (
                  <option key={y} value={y}>
                    {y}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-row">
              <FormLabelWithHint
                htmlFor="comp-pool"
                hint={formatMessage({
                  id: COMPENSATION_FIELD_HINT_KEYS.monetaryPool as never,
                })}
              >
                {formatMessage({ id: 'admin.compensation.monetaryPool' })}
              </FormLabelWithHint>
              <input
                id="comp-pool"
                type="number"
                min="1"
                step="1"
                value={form.monetaryPool}
                onChange={(e) => updateField('monetaryPool', e.target.value)}
                required
              />
            </div>

            <div className="form-row">
              <label htmlFor="comp-currency">
                {formatMessage({ id: 'common.currency' })}
              </label>
              <input
                id="comp-currency"
                type="text"
                maxLength={3}
                value={form.currency}
                onChange={(e) =>
                  updateField('currency', e.target.value.toUpperCase())
                }
                required
              />
            </div>

            <div className="form-row">
              <FormLabelWithHint
                htmlFor="comp-threshold"
                hint={formatMessage({
                  id: COMPENSATION_FIELD_HINT_KEYS.acceptablePerformanceRating as never,
                })}
              >
                {formatMessage({
                  id: 'admin.compensation.acceptablePerformanceThreshold',
                })}
              </FormLabelWithHint>
              <input
                id="comp-threshold"
                type="number"
                min="1"
                max="5"
                step="0.1"
                value={form.acceptablePerformanceRating}
                onChange={(e) =>
                  updateField('acceptablePerformanceRating', e.target.value)
                }
                required
              />
            </div>

            <div className="form-row">
              <FormLabelWithHint
                htmlFor="comp-exponent"
                hint={formatMessage({
                  id: COMPENSATION_FIELD_HINT_KEYS.exponent as never,
                })}
              >
                Eksponent
              </FormLabelWithHint>
              <input
                id="comp-exponent"
                type="number"
                min="0.1"
                step="0.1"
                value={form.exponent}
                onChange={(e) => updateField('exponent', e.target.value)}
                required
              />
            </div>

            <div className="form-row">
              <FormLabelWithHint
                htmlFor="comp-dependency"
                hint={formatMessage({
                  id: COMPENSATION_FIELD_HINT_KEYS.dependencyWeight as never,
                })}
              >
                Ponder zavisnosti (bodovi)
              </FormLabelWithHint>
              <input
                id="comp-dependency"
                type="number"
                min="0.01"
                step="0.01"
                value={form.dependencyWeight}
                onChange={(e) =>
                  updateField('dependencyWeight', e.target.value)
                }
                required
              />
            </div>

            <div className="form-row">
              <FormLabelWithHint
                htmlFor="comp-negative"
                hint={formatMessage({
                  id: COMPENSATION_FIELD_HINT_KEYS.allowNegativeVariable as never,
                })}
              >
                Negativna varijabila
              </FormLabelWithHint>
              <select
                id="comp-negative"
                value={form.allowNegativeVariable ? '1' : '0'}
                onChange={(e) =>
                  updateField('allowNegativeVariable', e.target.value === '1')
                }
              >
                <option value="0">{formatMessage({ id: 'common.no' })}</option>
                <option value="1">{formatMessage({ id: 'common.yes' })}</option>
              </select>
            </div>
          </div>

          <div className="form-actions compensation-params__actions">
            <button
              type="submit"
              className="btn btn-primary"
              disabled={saving || loading || isFinalized}
            >
              {saving
                ? formatMessage({ id: 'buttons.saving' })
                : existingId
                  ? formatMessage({ id: 'buttons.saveChanges' })
                  : formatMessage({
                      id: 'admin.compensation.createParameters',
                    })}
            </button>
            {existingId ? (
              <div className="compensation-params__actions-calc">
                <div
                  className="form-row compensation-params__calc-row"
                  style={{ margin: 0 }}
                >
                  <FormLabelWithHint
                    htmlFor="calc-negative"
                    hint={formatMessage({
                      id: COMPENSATION_FIELD_HINT_KEYS.calculateAllowNegative as never,
                    })}
                  >
                    Negativna varijabila pri kalkulaciji
                  </FormLabelWithHint>
                  <select
                    id="calc-negative"
                    value={calculateAllowNegative ? '1' : '0'}
                    onChange={(e) =>
                      setCalculateAllowNegative(e.target.value === '1')
                    }
                    disabled={calculating || isFinalized}
                  >
                    <option value="0">
                      {formatMessage({ id: 'common.no' })}
                    </option>
                    <option value="1">
                      {formatMessage({ id: 'common.yes' })}
                    </option>
                  </select>
                </div>
                <button
                  type="button"
                  className="btn btn-accent"
                  disabled={calculating || isFinalized}
                  onClick={handleCalculate}
                >
                  {calculating
                    ? formatMessage({ id: 'buttons.calculating' })
                    : formatMessage({
                        id: 'admin.compensation.calculateCompensation',
                      })}
                </button>
              </div>
            ) : null}
          </div>
        </form>

        <section className="compensation-params__chart card card--chart">
          <div className="compensation-preview-settings">
            <div className="compensation-preview-settings__grid">
              <div className="form-row">
                <FormLabelWithHint
                  htmlFor="comp-reference-points"
                  hint={formatMessage({
                    id: COMPENSATION_FIELD_HINT_KEYS.referencePoints as never,
                  })}
                >
                  Referentni bodovi
                </FormLabelWithHint>
                <input
                  id="comp-reference-points"
                  type="number"
                  min="1"
                  max="1000"
                  step="1"
                  value={previewProfile.referencePoints}
                  onChange={(e) =>
                    updatePreviewField('referencePoints', e.target.value)
                  }
                />
              </div>

              <div className="form-row">
                <FormLabelWithHint
                  htmlFor="comp-reference-salary"
                  hint={formatMessage({
                    id: COMPENSATION_FIELD_HINT_KEYS.referenceSalaryPerPoint as never,
                  })}
                >
                  Zarada po bodu
                </FormLabelWithHint>
                <input
                  id="comp-reference-salary"
                  type="number"
                  min="1"
                  step="1"
                  value={previewProfile.referenceSalaryPerPoint}
                  onChange={(e) =>
                    updatePreviewField(
                      'referenceSalaryPerPoint',
                      e.target.value,
                    )
                  }
                />
              </div>
            </div>
          </div>

          {loading ? (
            <div className="empty">
              {formatMessage({ id: 'common.loading' })}
            </div>
          ) : previewPoints.length === 0 ? (
            <div className="analytics-chart analytics-chart--empty">
              <p>
                {formatMessage({
                  id: 'admin.compensation.enterValidParamsForChart',
                })}
              </p>
            </div>
          ) : (
            <CompensationRatingPreviewChart
              points={previewPoints}
              currency={form.currency || 'RSD'}
              acceptablePerformanceRating={
                Number(form.acceptablePerformanceRating) || 2.5
              }
              variant="panel"
              hideFooter
            />
          )}
        </section>
      </div>
    </div>
  );
}
