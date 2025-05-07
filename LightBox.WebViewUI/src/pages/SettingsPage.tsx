import { useEffect, useState } from 'preact/hooks';
import useSettingsStore from '../stores/settingsStore';

export function SettingsPage() {
    const { settings, isLoading, error, loadSettings, updateSettings, saveSettings, addPluginScanDirectory, removePluginScanDirectory, setLogFilePath } = useSettingsStore();
    const [newScanDir, setNewScanDir] = useState('');

    useEffect(() => {
        // --- Add logs ---
        console.log("SettingsPage useEffect triggered.");
        console.log("Current settings state:", settings);
        console.log("Current isLoading state:", isLoading);
        // --- End logs ---

        if (!settings && !isLoading) {
            console.log("Condition met: Calling loadSettings()");
            loadSettings();
        } else {
            console.log("Condition NOT met: Skipping loadSettings()");
        }
    }, [settings, isLoading]); // Removed loadSettings from dependencies for testing

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
                {Array.isArray(settings.pluginScanDirectories) ? (
                    <>
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
                    </>
                ) : (
                    <p>Plugin scan directories are not available or in an invalid format.</p>
                    // Still provide a way to add a directory if the list is somehow corrupted/missing
                )}
                 {/* Moved the "Add Directory" part outside the conditional rendering of the list,
                    or ensure it's available even if pluginScanDirectories is not an array initially.
                    For simplicity, let's ensure the add functionality is always there if settings object exists.
                 */}
                {!Array.isArray(settings.pluginScanDirectories) && (
                    <div>
                        <input
                            type="text"
                            value={newScanDir}
                            onInput={(e) => setNewScanDir((e.target as HTMLInputElement).value)}
                            placeholder="Add new scan directory"
                        />
                        <button onClick={handleAddScanDir}>Add Directory</button>
                    </div>
                )}
            </section>

            {/* The following sections remain unchanged, but ensure they also handle potential undefined/null from settings if necessary */}
            <section>
                    <input
                        type="text"
                        value={newScanDir}
                        onInput={(e) => setNewScanDir((e.target as HTMLInputElement).value)}
                        placeholder="Add new scan directory"
                    />
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