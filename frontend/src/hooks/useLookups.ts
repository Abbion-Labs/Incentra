import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { DescriptiveRating, MeasureType, RatingLevel } from '../api/types';

export function useLookups() {
  const [ratingLevels, setRatingLevels] = useState<RatingLevel[]>([]);
  const [measureTypes, setMeasureTypes] = useState<MeasureType[]>([]);
  const [descriptiveRatings, setDescriptiveRatings] = useState<DescriptiveRating[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      api.get<RatingLevel[]>('/api/lookups/rating-levels'),
      api.get<MeasureType[]>('/api/lookups/measure-types'),
      api.get<DescriptiveRating[]>('/api/lookups/descriptive-ratings'),
    ])
      .then(([levels, types, descriptive]) => {
        setRatingLevels(levels);
        setMeasureTypes(types);
        setDescriptiveRatings(descriptive);
      })
      .finally(() => setLoading(false));
  }, []);

  return { ratingLevels, measureTypes, descriptiveRatings, loading };
}
