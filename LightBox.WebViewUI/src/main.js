import App from './App.svelte';
import { initLightboxApiService } from './services/lightboxApi';
import { mount } from 'svelte';

// 初始化与后端的通信服务
initLightboxApiService().then(initialized => {
    if (initialized) {
        console.log("Lightbox API service initialized successfully.");
        // 创建 Svelte 应用实例
        const app = mount(App, { target: document.getElementById("app") });
    } else {
        console.error("Failed to initialize Lightbox API service.");
        // 可以考虑在这里显示一个错误消息给用户
        document.getElementById('app').innerHTML = '<p style="color: red;">Failed to connect to Lightbox backend.</p>';
    }
}).catch(error => {
    console.error("Error during Lightbox API service initialization:", error);
    document.getElementById('app').innerHTML = `<p style="color: red;">Error connecting to Lightbox backend: ${error.message}</p>`;
});