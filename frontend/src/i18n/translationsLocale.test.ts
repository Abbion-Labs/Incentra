import { describe, expect, it } from 'vitest';
import enErrors from './errors/en.json';
import srErrors from './errors/sr.json';
import enMessages from './messages/en.json';
import srMessages from './messages/sr.json';
import { flattenMessages } from './flattenMessages';
import { translationsLocale } from './translationsLocale';

describe('translationsLocale', () => {
  it.each([
    ['sr', srMessages, srErrors],
    ['en', enMessages, enErrors],
  ] as const)(
    'keeps every key from messages and errors for %s',
    (locale, messages, errors) => {
      const merged = flattenMessages(translationsLocale(locale));

      expect(merged).toMatchObject(flattenMessages(messages));
      expect(merged).toMatchObject(flattenMessages(errors));
    },
  );

  it('resolves client-side validation errors alongside API error codes', () => {
    const merged = flattenMessages(translationsLocale('sr'));

    expect(merged['errors.revisionCommentRequired']).toBe(
      'Komentar za doradu je obavezan.',
    );
    expect(merged['errors.vn-0001']).toBe('Ime i prezime su obavezni.');
  });
});
