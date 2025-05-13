import { create } from 'zustand';
import { getApplicationSettings, saveApplicationSettings } from '../services/lightboxApi';

// interface SettingsState { ... } // Removed TS interface

const useSettingsStore = create((set, get) => ({
    settings: null,
    isLoading: false,
    error: null,
    loadSettings: async () => {
        console.log("settingsStore: loadSettings called"); // Added log
        set({ isLoading: true, error: null });
        try {
            const loadedSettings = await getApplicationSettings();
            console.log("settingsStore: loadSettings received from getApplicationSettings:", loadedSettings); // Added log

            if (loadedSettings && typeof loadedSettings.pluginScanDirectories === 'undefined') {
                console.warn("settingsStore: 'pluginScanDirectories' is undefined in loadedSettings. Defaulting to empty array for the store.", loadedSettings);
                set({ settings: { ...loadedSettings, pluginScanDirectories: [] }, isLoading: false });
            } else if (loadedSettings && !Array.isArray(loadedSettings.pluginScanDirectories)) {
                 console.warn("settingsStore: 'pluginScanDirectories' is not an array in loadedSettings. Defaulting to empty array. Value:", loadedSettings.pluginScanDirectories);
                set({ settings: { ...loadedSettings, pluginScanDirectories: [] }, isLoading: false });
            }
            else {
                set({ settings: loadedSettings, isLoading: false });
            }
        } catch (err) {
            const error = err instanceof Error ? err.message : 'Failed to load settings';
            console.error("settingsStore: Error in loadSettings:", error, err); // Log original error too
            set({ error, isLoading: false });
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