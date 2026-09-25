import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from '../../api/client';
import type { DescriptiveRating } from '../../api/types';
import { useIntl } from '../../i18n';
import { formatDescriptiveRatingLabel } from '../../utils/descriptiveRating';
import { useToast } from '../../hooks';
import { isEditConflict } from '../../utils/editConflict';
import { TableIconButton } from '../../components/common/TableIconButton';
import { TEXT_LIMITS } from '../../utils/textLimits';

interface RatingFormValues {
  code: string;
  name: string;
  minAverage: string;
  maxAverage: string;
  sortOrder: string;
  recommendedSharePercent: string;
  isActive: boolean;
}

const emptyForm = (): RatingFormValues => ({
  code: '',
  name: '',
  minAverage: '',
  maxAverage: '',
  sortOrder: '0',
  recommendedSharePercent: '0',
  isActive: true,
});

function ratingToForm(rating: DescriptiveRating): RatingFormValues {
  return {
    code: rating.code,
    name: rating.name,
    minAverage: rating.minAverage != null ? String(rating.minAverage) : '',
    maxAverage: rating.maxAverage != null ? String(rating.maxAverage) : '',
    sortOrder: String(rating.sortOrder),
    recommendedSharePercent: String(
      Math.round(rating.recommendedShare * 1000) / 10,
    ),
    isActive: rating.isActive,
  };
}

export function AdminRatingConfig() {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const [ratings, setRatings] = useState<DescriptiveRating[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  // Forma za unos je skrivena dok se ne izabere dodavanje ili izmena.
  const [formOpen, setFormOpen] = useState(false);
  // Verzija opisne ocene sa kojom je forma otvorena; šalje se uz izmenu.
  const [editingVersion, setEditingVersion] = useState<number | null>(null);
  const [form, setForm] = useState<RatingFormValues>(emptyForm());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const items = await api.get<DescriptiveRating[]>(
        '/api/descriptive-ratings',
      );
      setRatings(items);
    } catch (e) {
      toast.error(
        e instanceof Error
          ? e.message
          : formatMessage({ id: 'errors.loadFailed' }),
      );
    } finally {
      setLoading(false);
    }
  }, [formatMessage, toast]);

  useEffect(() => {
    load();
  }, [load]);

  const totalRecommendedPercent = useMemo(
    () =>
      ratings
        .filter((r) => r.isActive)
        .reduce((sum, r) => sum + r.recommendedShare * 100, 0),
    [ratings],
  );

  function startCreate() {
    setEditingId(null);
    setEditingVersion(null);
    setForm(emptyForm());
  }

  function openCreate() {
    startCreate();
    setFormOpen(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function closeForm() {
    startCreate();
    setFormOpen(false);
  }

  function startEdit(rating: DescriptiveRating) {
    setEditingId(rating.id);
    setEditingVersion(rating.version);
    setForm(ratingToForm(rating));
    setFormOpen(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function setField<K extends keyof RatingFormValues>(
    key: K,
    value: RatingFormValues[K],
  ) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  /** Opisna ocena je izmenjena posle otvaranja forme: forma se puni novim stanjem. */
  async function reopenAfterConflict(ratingId: number) {
    toast.warning(formatMessage({ id: 'errors.recordChangedMeanwhile' }));
    try {
      const fresh = (
        await api.get<DescriptiveRating[]>('/api/descriptive-ratings')
      ).find((r) => r.id === ratingId);
      if (fresh) {
        startEdit(fresh);
      } else {
        closeForm();
      }
    } catch {
      closeForm();
    }
    await load();
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    const payload = {
      code: form.code.trim(),
      name: form.name.trim(),
      minAverage: Number(form.minAverage),
      maxAverage: Number(form.maxAverage),
      sortOrder: Number(form.sortOrder),
      recommendedShare: Number(form.recommendedSharePercent) / 100,
      ...(editingId ? { isActive: form.isActive } : {}),
    };
    try {
      if (editingId) {
        const saved = await api.put<DescriptiveRating>(
          `/api/descriptive-ratings/${editingId}`,
          { ...payload, version: editingVersion },
        );
        setEditingVersion(saved.version);
        toast.success(formatMessage({ id: 'alerts.descriptiveRatingUpdated' }));
        closeForm();
      } else {
        await api.post('/api/descriptive-ratings', payload);
        toast.success(formatMessage({ id: 'alerts.descriptiveRatingAdded' }));
        closeForm();
      }
      await load();
    } catch (err) {
      if (editingId && isEditConflict(err)) {
        await reopenAfterConflict(editingId);
        return;
      }
      toast.error(
        err instanceof Error
          ? err.message
          : formatMessage({ id: 'errors.saveFailed' }),
      );
    } finally {
      setSaving(false);
    }
  }

  const sharesComplete = Math.abs(totalRecommendedPercent - 100) <= 0.5;

  return (
    <div className="admin-page">
      {formOpen && (
        <form
          className="card admin-form admin-rating-form"
          onSubmit={handleSubmit}
        >
          <h2 className="admin-form__title">
            {formatMessage({
              id: editingId
                ? 'admin.ratingConfig.editDescriptiveRating'
                : 'admin.ratingConfig.newDescriptiveRating',
            })}
          </h2>
          <div
            className={`admin-rating-form__grid${editingId ? ' admin-rating-form__grid--editing' : ''}`}
          >
            <div className="form-row">
              <label htmlFor="rating-code">
                {formatMessage({ id: 'admin.ratingConfig.code' })}
              </label>
              <input
                id="rating-code"
                value={form.code}
                maxLength={TEXT_LIMITS.code}
                onChange={(e) => setField('code', e.target.value.toUpperCase())}
                required
                disabled={!!editingId}
              />
            </div>
            <div className="form-row">
              <label htmlFor="rating-name">
                {formatMessage({ id: 'common.name' })}
              </label>
              <input
                id="rating-name"
                value={form.name}
                onChange={(e) => setField('name', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="rating-min">
                {formatMessage({ id: 'admin.ratingConfig.minAverage' })}
              </label>
              <input
                id="rating-min"
                type="number"
                step="0.01"
                min="0"
                max="5"
                value={form.minAverage}
                onChange={(e) => setField('minAverage', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="rating-max">
                {formatMessage({ id: 'admin.ratingConfig.maxAverage' })}
              </label>
              <input
                id="rating-max"
                type="number"
                step="0.01"
                min="0"
                max="5"
                value={form.maxAverage}
                onChange={(e) => setField('maxAverage', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="rating-sort">
                {formatMessage({ id: 'admin.ratingConfig.sortOrder' })}
              </label>
              <input
                id="rating-sort"
                type="number"
                value={form.sortOrder}
                onChange={(e) => setField('sortOrder', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <label htmlFor="rating-share">
                {formatMessage({ id: 'admin.ratingConfig.recommendedShare' })}
              </label>
              <input
                id="rating-share"
                type="number"
                step="0.1"
                min="0"
                max="100"
                value={form.recommendedSharePercent}
                onChange={(e) =>
                  setField('recommendedSharePercent', e.target.value)
                }
                required
              />
            </div>
            {editingId && (
              <div className="form-row">
                <label htmlFor="rating-active">
                  {formatMessage({ id: 'admin.active' })}
                </label>
                <select
                  id="rating-active"
                  value={form.isActive ? '1' : '0'}
                  onChange={(e) => setField('isActive', e.target.value === '1')}
                >
                  <option value="1">
                    {formatMessage({ id: 'common.yes' })}
                  </option>
                  <option value="0">
                    {formatMessage({ id: 'common.no' })}
                  </option>
                </select>
              </div>
            )}
          </div>
          <div className="actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving
                ? formatMessage({ id: 'buttons.saving' })
                : editingId
                  ? formatMessage({ id: 'buttons.saveChanges' })
                  : formatMessage({ id: 'buttons.add' })}
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={closeForm}
            >
              {formatMessage({ id: 'buttons.cancel' })}
            </button>
          </div>
        </form>
      )}

      <div className="card card--flush data-panel">
        <div className="data-panel__toolbar">
          {/* Zbir preporučenih udela aktivnih ocena: zeleno kad je 100%. */}
          <span
            className={`share-total ${sharesComplete ? 'is-complete' : 'is-invalid'}`}
            role="status"
          >
            {formatMessage({ id: 'admin.ratingConfig.totalRecommendedActive' })}{' '}
            {totalRecommendedPercent.toFixed(1)}%
            {!sharesComplete &&
              formatMessage({ id: 'admin.ratingConfig.idealShareHint' })}
          </span>
          {!formOpen && (
            <button
              type="button"
              className="btn btn-primary btn-sm"
              onClick={openCreate}
            >
              {formatMessage({ id: 'admin.ratingConfig.addDescriptiveRating' })}
            </button>
          )}
        </div>
        {loading ? (
          <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
        ) : (
          <div className="table-wrap">
            <table className="table table--hover table--stack">
              <thead>
                <tr>
                  <th className="col-text">
                    {formatMessage({ id: 'common.name' })}
                  </th>
                  <th className="col-meta">
                    {formatMessage({ id: 'admin.ratingConfig.code' })}
                  </th>
                  <th className="col-num">
                    {formatMessage({ id: 'admin.ratingConfig.averageRange' })}
                  </th>
                  <th className="col-num">
                    {formatMessage({
                      id: 'admin.ratingConfig.recommendedPercent',
                    })}
                  </th>
                  <th className="col-num">
                    {formatMessage({ id: 'admin.ratingConfig.sortOrder' })}
                  </th>
                  <th className="col-meta table-col--compact">
                    {formatMessage({ id: 'admin.active' })}
                  </th>
                  <th
                    className="col-actions"
                    aria-label={formatMessage({ id: 'admin.actions' })}
                  />
                </tr>
              </thead>
              <tbody>
                {ratings.map((rating) => (
                  <tr key={rating.id}>
                    <td className="col-text stack-title">
                      {formatDescriptiveRatingLabel(formatMessage, {
                        code: rating.code,
                        name: rating.name,
                      })}
                    </td>
                    <td
                      className="col-meta cell-code"
                      data-label={formatMessage({
                        id: 'admin.ratingConfig.code',
                      })}
                    >
                      {rating.code}
                    </td>
                    <td
                      className="col-num"
                      data-label={formatMessage({
                        id: 'admin.ratingConfig.averageRange',
                      })}
                    >
                      {rating.minAverage?.toFixed(2)} –{' '}
                      {rating.maxAverage?.toFixed(2)}
                    </td>
                    <td
                      className="col-num"
                      data-label={formatMessage({
                        id: 'admin.ratingConfig.recommendedPercent',
                      })}
                    >
                      {(rating.recommendedShare * 100).toFixed(1)}%
                    </td>
                    <td
                      className="col-num"
                      data-label={formatMessage({
                        id: 'admin.ratingConfig.sortOrder',
                      })}
                    >
                      {rating.sortOrder}
                    </td>
                    <td
                      className="col-meta table-col--compact"
                      data-label={formatMessage({ id: 'admin.active' })}
                    >
                      {rating.isActive
                        ? formatMessage({ id: 'common.yes' })
                        : formatMessage({ id: 'common.no' })}
                    </td>
                    <td className="col-actions">
                      <TableIconButton
                        icon="edit"
                        label={formatMessage({ id: 'buttons.edit' })}
                        onClick={() => startEdit(rating)}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
