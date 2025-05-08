import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

// 动态导入所有 locales/*/translation.json 文件
const resources = Object.entries(
  import.meta.glob<{ default: Record<string, string> }>('./locales/*/translation.json', { eager: true })
).reduce((acc, [path, mod]) => {
  const lang = path.match(/\.\/locales\/(.*)\/translation\.json/)![1];
  acc[lang] = { translation: mod.default };
  return acc;
}, {} as Record<string, { translation: Record<string, string> }>);

i18n
  .use(LanguageDetector) // 检测用户语言
  .use(initReactI18next) // 将 i18n 实例传递给 react-i18next
  .init({
    resources, // 直接提供所有语言资源
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false, // React 已经做了 XSS 防护
    },
    detection: {
      order: ['localStorage', 'navigator'], // 检测顺序
      caches: ['localStorage'], // 缓存检测到的语言
    },
    react: {
      useSuspense: true, // 明确启用 Suspense
    },
    // 如果使用命名空间，可以在这里配置
    // ns: ['translation', 'common', 'settings'],
    // defaultNS: 'translation',
  });

export default i18n;