// import 'i18next'; // Per instructions, this line might cause issues if i18next is not structured for this exact augmentation in all envs.
// It's often better to augment the specific module part if direct import augmentation fails.
// For now, we'll try the direct augmentation as suggested by many i18next TypeScript examples.

// Import the English translation file to use its structure as the source of truth for keys.
// Make sure the path is correct relative to this d.ts file, or adjust as needed.
// If your d.ts file is in src/types and locales is in src/locales, the path would be:
import type enTranslation from '../locales/en/translation.json';

declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: {
      translation: typeof enTranslation;
      // If you had other namespaces, you would add them here, for example:
      // common: typeof import('../locales/en/common.json').default;
    };
    // You can also specify return types, etc., if needed for advanced typing.
  }
}