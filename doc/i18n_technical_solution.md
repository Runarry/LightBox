# 多语言 (i18n) 功能技术方案 for LightBox.WebViewUI

## 1. 推荐的 i18n 库

我推荐使用 **`i18next`** 结合 **`react-i18next`** (通过 Preact 的兼容层或直接使用其核心逻辑)。

**选择理由：**

*   **功能完善：** `i18next` 是一个功能非常强大且成熟的 i18n 框架，支持插值、复数、上下文、格式化、命名空间等高级功能。
*   **生态系统丰富：** 拥有庞大的社区和丰富的插件生态系统，例如：
    *   `i18next-browser-languagedetector`: 自动检测用户浏览器语言。
    *   `i18next-http-backend`: 从服务器或 CDN 异步加载语言文件。
    *   各种用于不同框架的集成库（如 `react-i18next`）。
*   **TypeScript 支持良好：** `i18next` 和 `react-i18next` 都提供优秀的 TypeScript 支持，有助于在开发过程中进行类型检查，减少错误。
*   **框架无关性：** `i18next` 本身是框架无关的，`react-i18next` 提供了与 React 生态（包括 Preact）的良好集成。我们可以利用 Preact 的 `preact/compat` 或者直接使用 `react-i18next` 的 Hooks 和高阶组件。
*   **灵活性高：** 支持多种语言文件格式和加载策略。
*   **社区活跃：** 遇到问题时更容易找到解决方案和支持。

虽然也有其他如 `typesafe-i18n` 这样专门为 TypeScript 设计的库，但 `i18next` 的成熟度和生态系统使其成为一个更稳健和灵活的选择，特别是对于一个需要逐步完善和可能扩展功能的项目。

## 2. 语言文件的组织和格式

*   **存储位置：** 建议在 `LightBox.WebViewUI/src/` 目录下创建一个专门的 `locales` 目录来存放所有的语言文件。
    ```
    LightBox.WebViewUI/
    └── src/
        ├── locales/
        │   ├── en/
        │   │   └── translation.json
        │   ├── zh/
        │   │   └── translation.json
        │   └── ... (其他语言)
        ├── components/
        ├── pages/
        ├── stores/
        ├── main.tsx
        └── app.tsx
    ```
*   **文件格式：** 推荐使用 **JSON** 格式。
    *   **理由：** JSON 格式简洁、易于阅读和编辑，被广泛支持，且易于程序解析。Vite 也内置了对 JSON 文件的良好支持，可以直接导入。
*   **组织方式：**
    *   **按语言组织：** 每个语言一个单独的目录（例如 `en`, `zh`）。
    *   **按命名空间/模块组织（可选但推荐）：** 在每个语言目录下，可以进一步按功能模块或组件将翻译内容分割成不同的 JSON 文件（例如 `common.json`, `settings.json`, `plugins.json`）。`i18next` 支持命名空间 (namespaces) 的概念，这有助于管理大型项目的翻译，并允许按需加载。
        *   **初始阶段：** 可以先从一个单一的 `translation.json` 文件开始，随着项目规模的增长，再考虑引入命名空间。
        *   **示例 (`translation.json`)：**
            ```json
            // src/locales/en/translation.json
            {
              "appTitle": "LightBox",
              "viewMode_card": "Card View",
              "viewMode_list": "List View",
              "openLogs": "Open Logs",
              "addPlugin": "Add Plugin",
              "actionsFor": "Actions for {{pluginName}}",
              "buttons": {
                "save": "Save",
                "cancel": "Cancel"
              }
            }
            ```
            ```json
            // src/locales/zh/translation.json
            {
              "appTitle": "光盒",
              "viewMode_card": "卡片视图",
              "viewMode_list": "列表视图",
              "openLogs": "打开日志",
              "addPlugin": "添加插件",
              "actionsFor": "针对 {{pluginName}} 的操作",
              "buttons": {
                "save": "保存",
                "cancel": "取消"
              }
            }
            ```

## 3. 语言初始化和加载机制

*   **初始化位置：** 在应用的入口文件 `LightBox.WebViewUI/src/main.tsx` 中进行 i18n 实例的初始化，确保在渲染任何组件之前完成。
*   **初始化步骤：**
    1.  导入 `i18next` 和相关插件。
    2.  配置 `i18next`：
        *   `fallbackLng`: 设置默认回退语言（例如 `'en'`）。
        *   `detection`: 配置语言检测器（例如 `i18next-browser-languagedetector`），可以从 `localStorage`、`navigator` 等检测语言。
        *   `backend`: 配置后端插件（例如 `i18next-http-backend`）来加载语言文件。
            *   `loadPath`: 指定语言文件的加载路径，例如 `/locales/{{lng}}/{{ns}}.json`。Vite 在构建时会将 `public` 目录或通过 `import` 引入的资源打包，我们需要确保这些 `locales` 文件能被正确访问。一种常见做法是将 `locales` 目录放在 `public` 目录下，或者通过 Vite 的 `import.meta.glob` 来动态导入。对于 Vite 项目，更推荐使用 `import.meta.glob` 来实现按需加载和代码分割。
        *   `interpolation`: 配置插值 `{ escapeValue: false }`，因为 Preact/React 已经处理了 XSS。
        *   `debug`: 开发环境下可以开启 `true`，方便调试。
    3.  调用 `i18next.init()`。
    4.  将 `i18next` 实例通过 `I18nextProvider` (来自 `react-i18next`) 包裹根组件 `<App />`。

*   **语言文件加载：**
    *   **默认语言：** 应用启动时，`i18next` 会根据配置（例如语言检测器的结果或 `lng` 初始值）加载默认语言的翻译文件。
    *   **后续切换：** 当用户切换语言时，`i18next` 会异步加载对应语言的翻译文件。
    *   **加载策略：**
        *   **Vite 的 `import.meta.glob`：** 这是 Vite 项目中推荐的方式，可以实现真正的按需加载和代码分割。
            ```typescript
            // i18n.ts (或在 main.tsx 中)
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
                // 如果使用命名空间，可以在这里配置
                // ns: ['translation', 'common', 'settings'],
                // defaultNS: 'translation',
              });

            export default i18n;
            ```
            然后在 `LightBox.WebViewUI/src/main.tsx` 中：
            ```typescript
            // main.tsx
            import { render } from 'preact';
            import './index.css';
            import { App } from './app.tsx';
            import './i18n'; // 初始化 i18next
            import { I18nextProvider } from 'react-i18next';
            import i18n from './i18n'; // 导入配置好的 i18n 实例

            render(
              <I18nextProvider i18n={i18n}>
                <App />
              </I18nextProvider>,
              document.getElementById('app')!
            );
            ```
        *   **`i18next-http-backend` (备选方案)：** 如果语言文件需要从服务器动态获取，或者文件非常多不适合一次性打包，可以使用此插件。需要将 `locales` 目录放在 `public` 目录下，Vite 会将其复制到构建输出的根目录。

## 4. 语言切换机制

*   **UI 选项：**
    *   建议在应用的某个固定位置（例如 `TopBar` 或 `SettingsPage`）提供一个**下拉菜单**或一组**按钮**来让用户选择语言。
    *   下拉菜单可以显示支持的语言列表（例如 "English", "中文"）。
*   **Zustand Store 管理：**
    *   在现有的 `LightBox.WebViewUI/src/stores/uiStore.ts` (或者创建一个新的 `i18nStore.ts`) 中管理当前语言状态。
        ```typescript
        // src/stores/uiStore.ts (或 i18nStore.ts)
        import { create } from 'zustand';
        import i18n from '../i18n'; // 导入 i18next 实例

        interface I18nState {
          currentLanguage: string;
          supportedLanguages: { code: string; name: string }[];
          setLanguage: (language: string) => void;
        }

        export const useI18nStore = create<I18nState>((set) => ({
          currentLanguage: i18n.language || 'en', // 从 i18next 获取初始语言
          supportedLanguages: [ // 可以从配置或 API 获取
            { code: 'en', name: 'English' },
            { code: 'zh', name: '中文' },
          ],
          setLanguage: (language: string) => {
            i18n.changeLanguage(language).then(() => {
              set({ currentLanguage: language });
              // 可以选择将语言偏好保存到 localStorage
              localStorage.setItem('i18nextLng', language);
            });
          },
        }));

        // 监听 i18next 的 languageChanged 事件，确保 store 与 i18next 状态同步
        // 这在通过浏览器检测器或其他方式改变语言时尤其重要
        i18n.on('languageChanged', (lng) => {
          useI18nStore.setState({ currentLanguage: lng });
        });
        ```
*   **触发 UI 更新：**
    *   当 `setLanguage` action 被调用时：
        1.  调用 `i18n.changeLanguage(newLanguage)`。
        2.  `i18next` 会异步加载新的语言文件（如果尚未加载）。
        3.  加载完成后，`i18next` 会触发事件。
        4.  `react-i18next` 提供的 Hooks (如 `useTranslation`) 和 HOCs (如 `withTranslation`) 会自动感知到语言变化，并重新渲染使用了翻译文本的组件。
        5.  同时，Zustand store 中的 `currentLanguage` 状态也会更新，订阅了该状态的组件也会重新渲染（如果需要根据语言状态做其他逻辑处理）。

## 5. 在 Preact 组件中使用翻译

`react-i18next` 提供了多种方式在组件中使用翻译：

*   **`useTranslation` Hook (推荐)：** 这是在函数组件中最现代和简洁的方式。
    ```typescript
    // src/components/MyComponent.tsx
    import { h } from 'preact';
    import { useTranslation } from 'react-i18next';

    const MyComponent = () => {
      const { t, i18n } = useTranslation(); // 默认使用 'translation' 命名空间

      // 如果使用了其他命名空间，可以这样：
      // const { t } = useTranslation('myNamespace');

      const handleChangeLanguage = (lang: string) => {
        i18n.changeLanguage(lang);
        // 或者通过 Zustand store 来改变语言
        // useI18nStore.getState().setLanguage(lang);
      };

      return (
        <div>
          <h1>{t('appTitle')}</h1>
          <p>{t('welcomeMessage', { name: 'User' })}</p> {/* 带插值的翻译 */}
          <button onClick={() => handleChangeLanguage('en')}>English</button>
          <button onClick={() => handleChangeLanguage('zh')}>中文</button>
        </div>
      );
    };

    export default MyComponent;
    ```
*   **`withTranslation` HOC (高阶组件)：** 适用于类组件或需要将 `t` 函数作为 prop 传递的场景。
    ```typescript
    // src/components/MyClassComponent.tsx
    import { h, Component } from 'preact';
    import { withTranslation, WithTranslation } from 'react-i18next';

    interface MyClassComponentProps extends WithTranslation {}

    class MyClassComponent extends Component<MyClassComponentProps> {
      render() {
        const { t } = this.props;
        return <p>{t('someKey')}</p>;
      }
    }

    export default withTranslation()(MyClassComponent); // 默认使用 'translation' 命名空间
    // export default withTranslation('myNamespace')(MyClassComponent); // 指定命名空间
    ```
*   **`Trans` 组件：** 用于翻译包含 HTML 元素或 Preact 组件的复杂文本。
    ```typescript
    import { Trans } from 'react-i18next';

    // translation.json: "termsLink": "Please read our <1>terms and conditions</1>."
    <p>
      <Trans i18nKey="termsLink">
        Please read our <a href="/terms">terms and conditions</a>.
      </Trans>
    </p>
    ```

## 6. 集成到现有结构

*   **组件化开发 (`doc/架构说明文档_大纲.md`)：**
    *   `useTranslation` Hook 非常适合组件化开发。每个需要翻译文本的组件都可以独立调用此 Hook 来获取 `t` 函数。
    *   如果采用按命名空间组织语言文件，可以将特定组件或页面的翻译放在其专属的命名空间中，并在使用 `useTranslation` 时指定该命名空间，例如 `useTranslation('settingsPage')`。
*   **Zustand 状态管理 (`doc/架构说明文档_大纲.md`)：**
    *   如第 4 点所述，创建一个 `useI18nStore` (或在 `uiStore` 中扩展) 来管理当前选择的语言 (`currentLanguage`) 和支持的语言列表 (`supportedLanguages`)。
    *   语言切换组件 (例如下拉菜单) 会调用 `useI18nStore` 中的 `setLanguage` action。
    *   `setLanguage` action 内部会调用 `i18n.changeLanguage()` 来实际触发 `i18next` 的语言切换逻辑。
    *   `i18next` 的 `languageChanged` 事件可以用来同步更新 `useI18nStore` 中的 `currentLanguage`，确保状态的一致性，特别是当语言是通过 `i18next-browser-languagedetector` 自动检测或外部方式改变时。
*   **Vite 集成：**
    *   使用 `import.meta.glob` 动态导入 `src/locales` 下的 JSON 文件，可以充分利用 Vite 的代码分割和按需加载能力。
    *   确保 Vite 配置 (`vite.config.ts`) 不需要特殊修改即可支持 JSON 导入和 `import.meta.glob`。
*   **TypeScript 集成：**
    *   `i18next` 和 `react-i18next` 提供了良好的类型定义。
    *   可以考虑使用 `i18next-typescript` 或类似的工具根据 JSON 语言文件自动生成类型定义，从而在调用 `t` 函数时获得键名的类型提示和检查，进一步提升开发体验和代码健壮性。例如，可以定义一个类型来约束 `t` 函数的第一个参数。
        ```typescript
        // types/i18next.d.ts
        import 'i18next';
        // 导入你的默认命名空间的翻译类型
        import translation from '../src/locales/en/translation.json';

        declare module 'i18next' {
          interface CustomTypeOptions {
            defaultNS: 'translation';
            resources: {
              translation: typeof translation;
              // 如果有其他命名空间，也在这里声明
              // common: typeof import('../src/locales/en/common.json').default;
            };
          }
        }
        ```
        这需要 JSON 文件是静态可分析的，或者通过构建步骤生成类型。

## 总结与后续步骤

该方案提供了一个基于 `i18next` 的完整 i18n 实现路径。

**建议的后续步骤：**

1.  **安装依赖：**
    ```bash
    npm install i18next react-i18next i18next-browser-languagedetector
    # 或者 yarn add i18next react-i18next i18next-browser-languagedetector
    ```
2.  **创建 `locales` 目录和初始语言文件。**
3.  **在 `LightBox.WebViewUI/src/main.tsx` (或单独的 `i18n.ts`) 中初始化 `i18next`。**
4.  **在 Zustand store 中添加语言状态管理。**
5.  **创建语言切换 UI 组件。**
6.  **在需要翻译的组件中使用 `useTranslation` Hook。**
7.  **（可选）配置 TypeScript 类型以增强类型安全。**