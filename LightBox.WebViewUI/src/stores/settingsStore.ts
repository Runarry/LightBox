import { create } from 'zustand';
import { type ApplicationSettings, getApplicationSettings, saveApplicationSettings } from '../services/lightboxApi';

interface SettingsState {
    settings: ApplicationSettings | null;
    isLoading: boolean;
    error: string | null;
    loadSettings: () => Promise<void>;
    updateSettings: (partialSettings: Partial<ApplicationSettings>) => void;
    saveSettings: () => Promise<void>;
    addPluginScanDirectory: (directory: string) => void;
    removePluginScanDirectory: (directory: string) => void;
    setLogFilePath: (path: string) => void;
}

const useSettingsStore = create<SettingsState>((set, get) => ({
    settings: null,
    isLoading: false,
    error: null,
    loadSettings: async () => {
        set({ isLoading: true, error: null });
        try {
            const settings = await getApplicationSettings();
            set({ settings, isLoading: false });
        } catch (err) {
            const error = err instanceof Error ? err.message : 'Failed to load settings';
            set({ error, isLoading: false });
            console.error(error);
        }
    },
    updateSettings: (partialSettings) => {
        set((state) => ({
            settings: state.settings ? { ...state.settings, ...partialSettings } : null,
        }));
    },
    saveSettings: async () => {
        const currentSettings = get().settings;
        if (!currentSettings) {
            const error = 'No settings to save.';
            set({ error });
            console.error(error);
            return;
        }
        set({ isLoading: true, error: null });
        try {
            await saveApplicationSettings(currentSettings);
            set({ isLoading: false });
        } catch (err) {
            const error = err instanceof Error ? err.message : 'Failed to save settings';
            set({ error, isLoading: false });
            console.error(error);
        }
    },
    addPluginScanDirectory: (directory) => {
        set((state) => {
            if (!state.settings) return {};
            // Avoid duplicates
            if (state.settings.pluginScanDirectories.includes(directory)) {
                return {};
            }
            return {
                settings: {
                    ...state.settings,
                    pluginScanDirectories: [...state.settings.pluginScanDirectories, directory],
                },
            };
        });
    },
    removePluginScanDirectory: (directory) => {
        set((state) => {
            if (!state.settings) return {};
            return {
                settings: {
                    ...state.settings,
                    pluginScanDirectories: state.settings.pluginScanDirectories.filter(d => d !== directory),
                },
            };
        });
    },
    setLogFilePath: (path) => {
        set((state) => {
            if (!state.settings) return {};
            return {
                settings: {
                    ...state.settings,
                    logFilePath: path,
                },
            };
        });
    }
}));

export default useSettingsStore;