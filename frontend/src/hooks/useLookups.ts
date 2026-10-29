import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { DescriptiveRating, MeasureType, RatingLevel } from '../api/types';

interface Lookups {
  ratingLevels: RatingLevel[];
  measureTypes: MeasureType[];
  descriptiveRatings: DescriptiveRating[];
}

// Šifarnici se retko menjaju, pa se učitavaju jednom po otvaranju aplikacije, a ne na svakoj stranici.
// Admin koji izmeni opisne ocene poziva invalidateLookups, pa sledeća stranica dobija nove.
let loaded: Lookups | null = null;
let request: Promise<Lookups> | null = null;

function loadLookups(): Promise<Lookups> {
  if (!request) {
    request = Promise.all([
      api.get<RatingLevel[]>('/api/lookups/rating-levels'),
      api.get<MeasureType[]>('/api/lookups/measure-types'),
      api.get<DescriptiveRating[]>('/api/lookups/descriptive-ratings'),
    ]).then(([ratingLevels, measureTypes, descriptiveRatings]) => {
      loaded = { ratingLevels, measureTypes, descriptiveRatings };
      return loaded;
    });
    // Neuspeo zahtev se ne pamti: sledeća stranica pokušava ponovo.
    request.catch(() => {
      request = null;
    });
  }
  return request;
}

export function invalidateLookups(): void {
  loaded = null;
  request = null;
}

export function useLookups() {
  const [lookups, setLookups] = useState<Lookups | null>(loaded);

  useEffect(() => {
    if (lookups) return;
    let cancelled = false;
    loadLookups()
      .then((result) => {
        if (!cancelled) setLookups(result);
      })
      .catch(() => {
        // Stranica radi i bez šifarnika; prikazuje prazne skale kao i ranije.
        if (!cancelled)
          setLookups({
            ratingLevels: [],
            measureTypes: [],
            descriptiveRatings: [],
          });
      });
    return () => {
      cancelled = true;
    };
  }, [lookups]);

  return {
    ratingLevels: lookups?.ratingLevels ?? EMPTY_RATING_LEVELS,
    measureTypes: lookups?.measureTypes ?? EMPTY_MEASURE_TYPES,
    descriptiveRatings:
      lookups?.descriptiveRatings ?? EMPTY_DESCRIPTIVE_RATINGS,
    loading: lookups === null,
  };
}

// Stabilne prazne liste, da efekti koji zavise od šifarnika ne bi radili na svaki render dok se učitavaju.
const EMPTY_RATING_LEVELS: RatingLevel[] = [];
const EMPTY_MEASURE_TYPES: MeasureType[] = [];
const EMPTY_DESCRIPTIVE_RATINGS: DescriptiveRating[] = [];
