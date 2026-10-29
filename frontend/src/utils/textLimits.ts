/**
 * Najveće dužine tekstualnih polja, iste kao ograničenja kolona u bazi
 * (Infrastructure/Persistence/Configurations). Duži unos baza odbija.
 */
export const TEXT_LIMITS = {
  planItem: 500,
  personName: 100,
  email: 255,
  organizationUnitName: 150,
  lookupName: 100,
  code: 50,
} as const;
