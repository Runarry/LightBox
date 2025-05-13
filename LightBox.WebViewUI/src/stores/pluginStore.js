import { create } from 'zustand';
import { getAllPluginDefinitions } from '../services/lightboxApi';

// interface PluginState { ... } // Removed TS interface

const usePluginStore = create((set) => ({
    plugins: [],
    isLoading: false,
    error: null,
    loadPlugins: async () => {
        set({ isLoading: true, error: null });
        try {
            const pluginsResult = await getAllPluginDefinitions();
            console.info("Plugins result received in pluginStore:", pluginsResult); // Added log
            // Ensure plugins is always an array, even if API returns null/undefined unexpectedly
            const pluginsToSet = Array.isArray(pluginsResult) ? pluginsResult : [];
            if (!Array.isArray(pluginsResult)) {
                console.warn("getAllPluginDefinitions did not return an array. Defaulting to empty array. Received:", pluginsResult);
            }
            set({ plugins: pluginsToSet, isLoading: false });
        } catch (err) {
            const error = err instanceof Error ? err.message : 'Failed to load plugins';
            set({ error, isLoading: false });
            console.error(error);
        }
    },
}));

export default usePluginStore;