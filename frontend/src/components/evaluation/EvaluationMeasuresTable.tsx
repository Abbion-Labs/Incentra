import type { EvaluationMeasure } from '../../api/types';

import { useIntl } from '../../i18n';

import { formatLocalizedRatingDisplay } from '../../utils/ratingLevelLabels';
import { formatMeasureTypeName, getMeasureTypeDescription } from '../../utils/measureRatingDefaults';



interface EvaluationMeasuresTableProps {

  measures: EvaluationMeasure[];

  variant?: 'full' | 'simple';

}



export function EvaluationMeasuresTable({ measures, variant = 'full' }: EvaluationMeasuresTableProps) {

  const { formatMessage } = useIntl();

  function measureDescriptionText(measure: EvaluationMeasure): string {
    return measure.measureTypeCode ? getMeasureTypeDescription(measure.measureTypeCode, formatMessage) : '';
  }



  if (measures.length === 0) {

    return null;

  }



  if (variant === 'simple') {

    return (

      <div className="card">

        <h2>{formatMessage({ id: 'evaluation.measuresTitle' })}</h2>

        <table className="table">

          <thead>

            <tr>

              <th className="col-text">{formatMessage({ id: 'evaluation.measureFallback' })}</th>

              <th className="col-meta">{formatMessage({ id: 'evaluation.rating' })}</th>

            </tr>

          </thead>

          <tbody>

            {measures.map((m) => (

              <tr key={m.id}>

                <td className="col-text">{formatMeasureTypeName(formatMessage, { code: m.measureTypeCode, name: m.measureTypeName })}</td>

                <td className="col-meta">{formatLocalizedRatingDisplay(formatMessage, m.ratingLevelValue, m.ratingLevelLabel)}</td>

              </tr>

            ))}

          </tbody>

        </table>

      </div>

    );

  }



  return (

    <div className="card">

      <h2>{formatMessage({ id: 'evaluation.measuresTitle' })}</h2>

      <table className="table">

        <thead>

          <tr>

            <th className="col-text">{formatMessage({ id: 'evaluation.measureFallback' })}</th>

            <th className="col-text">{formatMessage({ id: 'common.description' })}</th>

            <th className="col-meta">{formatMessage({ id: 'evaluation.rating' })}</th>

            <th className="col-text">{formatMessage({ id: 'common.comment' })}</th>

          </tr>

        </thead>

        <tbody>

          {measures.map((m) => (

            <tr key={m.id}>

              <td className="col-text">{formatMeasureTypeName(formatMessage, { code: m.measureTypeCode, name: m.measureTypeName })}</td>

              <td className="col-text">{measureDescriptionText(m) || formatMessage({ id: 'common.emptyValue' })}</td>

              <td className="col-meta">{formatLocalizedRatingDisplay(formatMessage, m.ratingLevelValue, m.ratingLevelLabel)}</td>

              <td className="col-text">{m.ratingComment ?? formatMessage({ id: 'common.emptyValue' })}</td>

            </tr>

          ))}

        </tbody>

      </table>

    </div>

  );

}

