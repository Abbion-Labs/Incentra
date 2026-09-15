import srMessages from './messages/sr.json';
import srErrors from './errors/sr.json';

export type IntlMessage = typeof srMessages & typeof srErrors;

export type NestedKeys<T> = T extends object
  ? {
      [K in keyof T]: K extends string
        ? T[K] extends string
          ? K
          : `${K}.${NestedKeys<T[K]>}`
        : never;
    }[keyof T]
  : never;
