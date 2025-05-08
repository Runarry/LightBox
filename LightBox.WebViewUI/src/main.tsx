import { render } from 'preact';
import { Suspense } from 'preact/compat'; // Import Suspense
import './index.css';
import { App } from './app.tsx';
import './i18n'; // 初始化 i18next
import { I18nextProvider } from 'react-i18next';
import i18n from './i18n'; // 导入配置好的 i18n 实例

render(
  <Suspense fallback={<div>Loading translations...</div>}>
    <I18nextProvider i18n={i18n}>
      <App />
    </I18nextProvider>
  </Suspense>,
  document.getElementById('app')!
);
