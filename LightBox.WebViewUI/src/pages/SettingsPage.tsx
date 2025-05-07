import { useEffect, useState } from 'preact/hooks';
import useSettingsStore from '../stores/settingsStore';

export function SettingsPage() {
    const { settings, isLoading, error, loadSettings, updateSettings, saveSettings, addPluginScanDirectory, removePluginScanDirectory, setLogFilePath } = useSettingsStore();
    const [newScanDir, setNewScanDir] = useState('');

    useEffect(() => {
        if (!settings && !isLoading) {
            loadSettings();
        }
    }, [settings, isLoading, loadSettings]);

    const handleAddScanDir = () => {
        if (newScanDir.trim() !== '') {
            addPluginScanDirectory(newScanDir.trim());
            setNewScanDir('');
        }
    };

    if (isLoading && !settings) {
        return <div>Loading settings...</div>;
    }

    if (error) {
        return <div>Error loading settings: {error}</div>;
    }

    if (!settings) {
        return <div>No settings loaded.</div>;
    }

    return (
        <div>
            <h2>Application Settings</h2>

            <section>
                <h3>Plugin Scan Directories</h3>
                <ul>
                    {settings.pluginScanDirectories.map((dir) => (
                        <li key={dir}>
                            {dir}{' '}
                            <button onClick={() => removePluginScanDirectory(dir)}>Remove</button>
                        </li>
                    ))}
                </ul>
                <div>
                    <input
                        type="text"
                        value={newScanDir}
                        onInput={(e) => setNewScanDir((e.target as HTMLInputElement).value)}
                        placeholder="Add new scan directory"
                    />
                    <button onClick={handleAddScanDir}>Add Directory</button>
                </div>
            </section>

            <section>
                <h3>Log File Path</h3>
                <input
                    type="text"
                    value={settings.logFilePath}
                    onInput={(e) => setLogFilePath((e.target as HTMLInputElement).value)}
                />
            </section>
            
            <section>
                <h3>Log Level</h3>
                <select 
                    value={settings.logLevel} 
                    onChange={(e) => updateSettings({ logLevel: (e.target as HTMLSelectElement).value })}
                >
                    <option value="Verbose">Verbose</option>
                    <option value="Debug">Debug</option>
                    <option value="Information">Information</option>
                    <option value="Warning">Warning</option>
                    <option value="Error">Error</option>
                    <option value="Fatal">Fatal</option>
                </select>
            </section>

            <section>
                <h3>IPC API Port</h3>
                <input
                    type="number"
                    value={settings.ipcApiPort}
                    onInput={(e) => {
                        const port = parseInt((e.target as HTMLInputElement).value, 10);
                        updateSettings({ ipcApiPort: isNaN(port) ? 0 : port });
                    }}
                />
                <p><small>Set to 0 for a random available port.</small></p>
            </section>

            <button onClick={saveSettings} disabled={isLoading}>
                {isLoading ? 'Saving...' : 'Save Settings'}
            </button>
            {error && <p style={{ color: 'red' }}>Error saving settings: {error}</p>}
        </div>
    );
}