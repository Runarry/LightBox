import { useEffect } from 'preact/hooks';
import usePluginStore from '../stores/pluginStore';
import { type PluginDefinition } from '../services/lightboxApi'; // Import type

export function PluginsPage() {
    const { plugins, isLoading, error, loadPlugins } = usePluginStore();

    useEffect(() => {
        if (plugins.length === 0 && !isLoading) {
            loadPlugins();
        }
    }, [plugins, isLoading, loadPlugins]);

    if (isLoading) {
        return <div>Loading plugins...</div>;
    }

    if (error) {
        return <div>Error loading plugins: {error}</div>;
    }

    if (plugins.length === 0) {
        return <div>No plugins found.</div>;
    }

    return (
        <div>
            <h2>Available Plugins</h2>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '16px' }}>
                {plugins.map((plugin: PluginDefinition) => (
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
                ))}
            </div>
        </div>
    );
}