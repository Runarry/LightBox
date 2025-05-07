import { create } from 'zustand';
import { type PluginDefinition, getAllPluginDefinitions } from '../services/lightboxApi';

interface PluginState {
    plugins: PluginDefinition[];
    isLoading: boolean;
    error: string | null;
    loadPlugins: () => Promise<void>;
}

const usePluginStore = create<PluginState>((set) => ({
    plugins: [],
    isLoading: false,
    error: null,
    loadPlugins: async () => {
        set({ isLoading: true, error: null });
        try {
            const plugins = await getAllPluginDefinitions();
            set({ plugins, isLoading: false });
        } catch (err) {
            const error = err instanceof Error ? err.message : 'Failed to load plugins';
            set({ error, isLoading: false });
            console.error(error);
        }
    },
}));

export default usePluginStore;