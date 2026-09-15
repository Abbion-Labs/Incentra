import type { IntlMessage } from './i18n.types';

export function flattenMessages(
  nestedMessages: Record<string, unknown> | IntlMessage,
  prefix = '',
): Record<string, string> {
  return Object.keys(nestedMessages).reduce<Record<string, string>>((messages, key) => {
    const value = nestedMessages[key as keyof typeof nestedMessages];
    const prefixedKey = prefix ? `${prefix}.${key}` : key;

    if (typeof value === 'string') {
      messages[prefixedKey] = value;
    } else if (value && typeof value === 'object') {
      Object.assign(messages, flattenMessages(value as Record<string, unknown>, prefixedKey));
    }

    return messages;
  }, {});
}
