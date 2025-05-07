// 定义与 C# 模型匹配的 TypeScript 接口

/**
 * 应用程序设置
 */
export interface ApplicationSettings {
    pluginScanDirectories: string[];
    logFilePath: string;
    logLevel: string;
    ipcApiPort: number;
}

/**
 * 插件定义
 */
export interface PluginDefinition {
    id: string;
    name: string;
    version: string;
    description: string;
    author: string;
    plugin_type: string; // e.g., "executable", "dotnet_library"
    executable?: string;
    args_template?: string;
    assembly_path?: string;
    main_class?: string;
    config_schema?: any; // JSON Schema for configuration, using 'any' for flexibility
    communication: string; // Default to "stdio"
    icon?: string; // Path to icon file or base64 string
}

// 扩展 window 对象以包含 lightboxBridge
declare global {
    interface Window {
        lightboxBridge?: {
            getApplicationSettings: () => Promise<string>;
            saveApplicationSettings: (settingsJson: string) => Promise<void>;
            getAllPluginDefinitions: () => Promise<string>;
        };
    }
}

/**
 * 检查 lightboxBridge 是否可用
 */
function getBridge() {
    if (!window.lightboxBridge) {
        console.error("Lightbox JSBridge is not available.");
        throw new Error("Lightbox JSBridge is not available.");
    }
    return window.lightboxBridge;
}

/**
 * 获取应用程序设置
 * @returns Promise<ApplicationSettings>
 */
export async function getApplicationSettings(): Promise<ApplicationSettings> {
    const bridge = getBridge();
    try {
        const settingsJson = await bridge.getApplicationSettings();
        const settings = JSON.parse(settingsJson);
        if (settings.error) {
            console.error("Error from backend (getApplicationSettings):", settings.error);
            throw new Error(settings.error);
        }
        return settings as ApplicationSettings;
    } catch (error) {
        console.error("Error calling getApplicationSettings or parsing response:", error);
        throw error;
    }
}

/**
 * 保存应用程序设置
 * @param settings - ApplicationSettings 对象
 * @returns Promise<void>
 */
export async function saveApplicationSettings(settings: ApplicationSettings): Promise<void> {
    const bridge = getBridge();
    try {
        const settingsJson = JSON.stringify(settings);
        await bridge.saveApplicationSettings(settingsJson);
    } catch (error) {
        console.error("Error calling saveApplicationSettings:", error);
        throw error;
    }
}

/**
 * 获取所有插件定义
 * @returns Promise<PluginDefinition[]>
 */
export async function getAllPluginDefinitions(): Promise<PluginDefinition[]> {
    const bridge = getBridge();
    try {
        const pluginsJson = await bridge.getAllPluginDefinitions();
        const plugins = JSON.parse(pluginsJson);
        if (plugins.error) {
            console.error("Error from backend (getAllPluginDefinitions):", plugins.error);
            throw new Error(plugins.error);
        }
        return plugins as PluginDefinition[];
    } catch (error) {
        console.error("Error calling getAllPluginDefinitions or parsing response:", error);
        throw error;
    }
}