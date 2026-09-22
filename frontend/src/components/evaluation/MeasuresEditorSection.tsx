import type { MeasureType, RatingLevel } from '../../api/types';
import { useIntl } from '../../i18n';
import { FormSection } from '../forms/FormSection';
import { SectionAverageFooter } from './SectionAverageFooter';
import { RatingValueSelect } from './RatingValueSelect';
import {
  calculateComponentAverage,
  findRatingLevel,
  formatComponentAverage,
  isNotRated,
  ratingValueOptionLabel,
} from '../../utils/scoring';
import {
  getMeasureRatingComment,
  getMeasureTypeDescription,
  formatMeasureTypeName,
  type MeasureFormatMessage,
} from '../../utils/measureRatingDefaults';
export interface MeasureDraft {
  measureTypeId: number;
  ratingComment: string;
  ratingLevelId: number;
  sortOrder: number;
}

interface MeasuresEditorSectionProps {
  measures: MeasureDraft[];
  measureTypes: MeasureType[];
  ratingLevels: RatingLevel[];
  editable: boolean;
  onMeasuresChange: (measures: MeasureDraft[]) => void;
}

function measuresIncomplete(
  measures: MeasureDraft[],
  ratingLevels: RatingLevel[],
): boolean {
  if (measures.length === 0) return true;
  return measures.some((measure) => {
    const level = findRatingLevel(ratingLevels, measure.ratingLevelId);
    return !level || isNotRated(level);
  });
}
function applyRatingToMeasure(
  measure: MeasureDraft,
  measureType: MeasureType | undefined,
  ratingLevel: RatingLevel | undefined,
  formatMessage: MeasureFormatMessage,
): MeasureDraft {
  if (!ratingLevel || !measureType) {
    return {
      ...measure,
      ratingLevelId: ratingLevel?.id ?? measure.ratingLevelId,
    };
  }

  const comment = isNotRated(ratingLevel)
    ? ''
    : getMeasureRatingComment(
        measureType.code,
        ratingLevel.value,
        formatMessage,
      );

  return {
    ...measure,
    ratingLevelId: ratingLevel.id,
    ratingComment: comment,
  };
}

export function MeasuresEditorSection({
  measures,
  measureTypes,
  ratingLevels,
  editable,
  onMeasuresChange,
}: MeasuresEditorSectionProps) {
  const { formatMessage } = useIntl();
  const measuresAverage = calculateComponentAverage(
    measures.map((measure) => ({ ratingLevelId: measure.ratingLevelId })),
    ratingLevels,
  );
  const averageText = formatComponentAverage(
    measuresIncomplete(measures, ratingLevels),
    measuresAverage,
    measures.length > 0,
  );

  return (
    <FormSection
      title={formatMessage({ id: 'evaluation.measuresTitle' })}
      hint={formatMessage({ id: 'evaluation.measuresHint' })}
    >
      <div className="measure-list">
        {measures.map((m, idx) => {
          const mt = measureTypes.find((t) => t.id === m.measureTypeId);
          const selectedLevel = findRatingLevel(ratingLevels, m.ratingLevelId);
          const description = mt
            ? getMeasureTypeDescription(mt.code, formatMessage, mt.description)
            : '';

          return (
            <article key={m.measureTypeId} className="measure-card">
              <div className="measure-card__header">
                <span className="measure-card__index">{idx + 1}</span>
                <h3 className="measure-card__title">
                  {mt
                    ? formatMeasureTypeName(formatMessage, {
                        code: mt.code,
                        name: mt.name,
                      })
                    : formatMessage({ id: 'evaluation.measureFallback' })}
                </h3>
              </div>

              {description && (
                <p className="measure-card__description">{description}</p>
              )}

              <div className="measure-card__rating-row">
                <div className="measure-card__rating-control">
                  <label>{formatMessage({ id: 'evaluation.rating' })}</label>
                  {editable ? (
                    <RatingValueSelect
                      ratingLevels={ratingLevels}
                      value={m.ratingLevelId}
                      onChange={(ratingLevelId) => {
                        const level = findRatingLevel(
                          ratingLevels,
                          ratingLevelId,
                        );
                        const next = [...measures];
                        next[idx] = applyRatingToMeasure(
                          m,
                          mt,
                          level,
                          formatMessage,
                        );
                        onMeasuresChange(next);
                      }}
                    />
                  ) : (
                    <span className="measure-card__rating-value">
                      {selectedLevel
                        ? ratingValueOptionLabel(selectedLevel)
                        : '—'}
                    </span>
                  )}
                </div>
                {m.ratingComment && (
                  <p className="measure-card__comment-inline">
                    {m.ratingComment}
                  </p>
                )}
              </div>
            </article>
          );
        })}
      </div>
      <SectionAverageFooter
        label={formatMessage({ id: 'evaluation.measuresAverage' })}
        value={averageText}
      />
    </FormSection>
  );
}
