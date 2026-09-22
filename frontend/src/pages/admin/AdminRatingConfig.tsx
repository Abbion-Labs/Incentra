import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from '../../api/client';
import type { DescriptiveRating } from '../../api/types';
import { useIntl } from '../../i18n';
import { formatDescriptiveRatingLabel } from '../../utils/descriptiveRating';
import { useToast } from '../../hooks';
import { AdminPageHeader } from './components/AdminPageHeader';

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
    setForm(emptyForm());
  }

  function startEdit(rating: DescriptiveRating) {
    setEditingId(rating.id);
    setForm(ratingToForm(rating));
  }

  function setField<K extends keyof RatingFormValues>(
    key: K,
    value: RatingFormValues[K],
  ) {
    setForm((prev) => ({ ...prev, [key]: value }));
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
        await api.put(`/api/descriptive-ratings/${editingId}`, payload);
        toast.success(formatMessage({ id: 'alerts.descriptiveRatingUpdated' }));
      } else {
        await api.post('/api/descriptive-ratings', payload);
        toast.success(formatMessage({ id: 'alerts.descriptiveRatingAdded' }));
        startCreate();
      }
      await load();
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

  return (
    <div className="admin-page">
      <AdminPageHeader
        actions={
          editingId ? (
            <button
              type="button"
              className="btn btn-secondary"
              onClick={startCreate}
            >
              {formatMessage({ id: 'admin.ratingConfig.newDescriptiveRating' })}
            </button>
          ) : null
        }
      />

      <form
        className="card admin-form admin-rating-form"
        onSubmit={handleSubmit}
      >
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
                <option value="1">{formatMessage({ id: 'common.yes' })}</option>
                <option value="0">{formatMessage({ id: 'common.no' })}</option>
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
          {editingId && (
            <button
              type="button"
              className="btn btn-secondary"
              onClick={startCreate}
            >
              {formatMessage({ id: 'buttons.cancel' })}
            </button>
          )}
        </div>
      </form>

      <div className="card">
        <div className="admin-rating-summary">
          <p className="card__hint">
            {formatMessage({ id: 'admin.ratingConfig.totalRecommendedActive' })}{' '}
            <strong
              className={
                Math.abs(totalRecommendedPercent - 100) > 0.5
                  ? 'text-warning'
                  : ''
              }
            >
              {totalRecommendedPercent.toFixed(1)}%
            </strong>
            {Math.abs(totalRecommendedPercent - 100) > 0.5 &&
              formatMessage({ id: 'admin.ratingConfig.idealShareHint' })}
          </p>
        </div>
        {loading ? (
          <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
        ) : (
          <div className="table-wrap">
            <table className="table table--hover">
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
                    <td className="col-text">
                      {formatDescriptiveRatingLabel(formatMessage, {
                        code: rating.code,
                        name: rating.name,
                      })}
                    </td>
                    <td className="col-meta">{rating.code}</td>
                    <td className="col-num">
                      {rating.minAverage?.toFixed(2)} –{' '}
                      {rating.maxAverage?.toFixed(2)}
                    </td>
                    <td className="col-num">
                      {(rating.recommendedShare * 100).toFixed(1)}%
                    </td>
                    <td className="col-num">{rating.sortOrder}</td>
                    <td className="col-meta table-col--compact">
                      {rating.isActive
                        ? formatMessage({ id: 'common.yes' })
                        : formatMessage({ id: 'common.no' })}
                    </td>
                    <td className="col-actions">
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => startEdit(rating)}
                      >
                        {formatMessage({ id: 'buttons.edit' })}
                      </button>
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
