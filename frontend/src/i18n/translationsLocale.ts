import enErrors from './errors/en.json';
import srErrors from './errors/sr.json';
import enMessages from './messages/en.json';
import srMessages from './messages/sr.json';
import type { IntlMessage } from './i18n.types';
import type { AppLocale } from '../localization';

type JsonObject = Record<string, unknown>;

function isJsonObject(value: unknown): value is JsonObject {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

// Deep merge: messages/*.json and errors/*.json both define an `errors`
// section, and a shallow spread would drop one of them entirely.
function mergeJson(target: JsonObject, source: JsonObject): JsonObject {
  const result: JsonObject = { ...target };
  for (const [key, value] of Object.entries(source)) {
    const existing = result[key];
    result[key] =
      isJsonObject(existing) && isJsonObject(value)
        ? mergeJson(existing, value)
        : value;
  }
  return result;
}

function concatJsonLocalizations(localizations: IntlMessage[]): IntlMessage {
  return localizations.reduce<JsonObject>(
    (result, json) => mergeJson(result, json as JsonObject),
    {},
  ) as IntlMessage;
}

export function translationsLocale(locale: AppLocale): IntlMessage {
  if (locale === 'en') {
    return concatJsonLocalizations([enMessages, enErrors] as IntlMessage[]);
  }

  return concatJsonLocalizations([srMessages, srErrors] as IntlMessage[]);
}
