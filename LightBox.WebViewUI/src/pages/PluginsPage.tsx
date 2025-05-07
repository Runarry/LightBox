import { useEffect } from 'preact/hooks';
import usePluginStore from '../stores/pluginStore';
import { type PluginDefinition } from '../services/lightboxApi'; // Import type

export function PluginsPage() {
    const { plugins, isLoading, error, loadPlugins } = usePluginStore();

    useEffect(() => {
        // Ensure plugins is an array before checking its length
        if (Array.isArray(plugins) && plugins.length === 0 && !isLoading) {
            console.log("PluginsPage useEffect: plugins is empty array and not loading, calling loadPlugins.");
            loadPlugins();
        } else if (!Array.isArray(plugins) && !isLoading) {
            // If plugins is not an array (e.g., undefined initially) and not loading, also try to load.
            console.warn("PluginsPage useEffect: plugins is not an array and not loading, calling loadPlugins. Current plugins:", plugins);
            loadPlugins();
        }
    }, [plugins, isLoading, loadPlugins]);

    if (isLoading) {
        return <div>Loading plugins...</div>;
    }

    if (error) {
        return <div>Error loading plugins: {error}</div>;
    }

    // Ensure plugins is an array before trying to access its length or map over it
    if (!Array.isArray(plugins)) {
        // This case should ideally be prevented by the store always providing an array,
        // but as a fallback, or if the store's initial state is somehow bypassed or delayed.
        console.warn("PluginsPage: 'plugins' is not an array. Current value:", plugins);
        return <div>Plugins data is currently unavailable or in an invalid format.</div>;
    }

    if (plugins.length === 0) {
        return <div>No plugins found.</div>;
    }

    return (
        <div>
            <h2>Available Plugins</h2>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '16px' }}>
                {(() => {
                    console.log("PluginsPage: About to map 'plugins'. Current value:", plugins, "Is Array:", Array.isArray(plugins));
                    if (!Array.isArray(plugins)) {
                        // This should have been caught by earlier checks, but as a last resort.
                        console.error("PluginsPage: CRITICAL - 'plugins' is not an array just before .map() call. This should not happen.", plugins);
                        return <p>Error: Plugin data is corrupted.</p>;
                    }
                    return plugins.map((plugin: PluginDefinition) => (
                        <div key={plugin.id} style={{ border: '1px solid #ccc', padding: '16px', borderRadius: '8px', width: '300px' }}>
                            <h3>{plugin.name} <small>({plugin.version})</small></h3>
                        <p><strong>ID:</strong> {plugin.id}</p>
                        <p><strong>Author:</strong> {plugin.author || 'N/A'}</p>
                        <p><strong>Description:</strong> {plugin.description || 'No description provided.'}</p>
                        <p><strong>Type:</strong> {plugin.plugin_type}</p>
                        {plugin.executable && <p><strong>Executable:</strong> {plugin.executable}</p>}
                        {plugin.assembly_path && <p><strong>Assembly:</strong> {plugin.assembly_path}</p>}
                        {plugin.main_class && <p><strong>Main Class:</strong> {plugin.main_class}</p>}
                        <p><strong>Communication:</strong> {plugin.communication}</p>
                        {/* Icon could be displayed here if available: <img src={plugin.icon} alt={plugin.name} /> */}
                    </div>
                    ));
                })()}
            </div>
        </div>
    );
}