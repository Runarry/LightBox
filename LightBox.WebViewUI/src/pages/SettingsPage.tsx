import { useEffect, useState } from 'preact/hooks';
import useSettingsStore from '../stores/settingsStore';
import { useUIStore } from '../stores/uiStore'; // Import useUIStore
import { useTranslation } from 'react-i18next'; // Import useTranslation

export function SettingsPage() {
    const { settings, isLoading, error, loadSettings, updateSettings, saveSettings, addPluginScanDirectory, removePluginScanDirectory, setLogFilePath } = useSettingsStore();
    const { currentLanguage, supportedLanguages, setLanguage } = useUIStore(); // Get i18n state and actions
    const { t } = useTranslation(); // Get t function
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
        return <div>{t('loading')}</div>; {/* Assuming 'loading' key exists or will be added */}
    }

    if (error) {
        return <div>{t('settingsPage.errorLoading', { error: error })}</div>; {/* Assuming 'settingsPage.errorLoading' key exists */}
    }

    if (!settings) {
        return <div>{t('settingsPage.noSettings')}</div>; {/* Assuming 'settingsPage.noSettings' key exists */}
    }

    return (
        <div>
            <h2>{t('settingsPage.title')}</h2>

            <section>
                <h3>{t('settingsPage.language')}</h3>
                <select value={currentLanguage} onChange={(e) => setLanguage((e.target as HTMLSelectElement).value)}>
                    {supportedLanguages.map(lang => (
                        <option key={lang.code} value={lang.code}>{lang.name}</option>
                    ))}
                </select>
            </section>

            <section>
                <h3>{t('settingsPage.pluginScanDirectories')}</h3>
                {Array.isArray(settings.pluginScanDirectories) ? (
                    <>
                        <ul>
                            {settings.pluginScanDirectories.map((dir) => (
                                <li key={dir}>
                                    {dir}{' '}
                                    <button onClick={() => removePluginScanDirectory(dir)}>{t('settingsPage.removeButton')}</button>
                                </li>
                            ))}
                        </ul>
                        <div>
                            <input
                                type="text"
                                value={newScanDir}
                                onInput={(e) => setNewScanDir((e.target as HTMLInputElement).value)}
                                placeholder={t('settingsPage.addScanDirectoryPlaceholder')}
                            />
                            <button onClick={handleAddScanDir}>{t('settingsPage.addDirectoryButton')}</button>
                        </div>
                    </>
                ) : (
                    <p>{t('settingsPage.pluginScanDirectoriesNotAvailable', 'Plugin scan directories are not available or in an invalid format.')}</p>
                )}
                {/*
                  The following block for adding a directory when pluginScanDirectories is not an array
                  is somewhat redundant if the goal is to always show the add interface.
                  Consider simplifying the logic to always show the add input if settings object exists,
                  or ensure the initial state of pluginScanDirectories is always an array.
                  For now, keeping the logic as it was, but with translations.
                */}
                {!Array.isArray(settings.pluginScanDirectories) && (
                     <div>
                         <input
                             type="text"
                             value={newScanDir}
                             onInput={(e) => setNewScanDir((e.target as HTMLInputElement).value)}
                             placeholder={t('settingsPage.addScanDirectoryPlaceholder')}
                         />
                         <button onClick={handleAddScanDir}>{t('settingsPage.addDirectoryButton')}</button>
                     </div>
                 )}
            </section>

            <section>
                <h3>{t('settingsPage.logFilePath')}</h3>
                <input
                    type="text"
                    value={settings.logFilePath}
                    onInput={(e) => setLogFilePath((e.target as HTMLInputElement).value)}
                />
            </section>
            
            <section>
                <h3>{t('settingsPage.logLevel')}</h3>
                <select
                    value={settings.logLevel}
                    onChange={(e) => updateSettings({ logLevel: (e.target as HTMLSelectElement).value })}
                >
                    {/* It's better to translate these options as well if they are user-facing */}
                    <option value="Verbose">{t('logLevels.verbose', 'Verbose')}</option>
                    <option value="Debug">{t('logLevels.debug', 'Debug')}</option>
                    <option value="Information">{t('logLevels.information', 'Information')}</option>
                    <option value="Warning">{t('logLevels.warning', 'Warning')}</option>
                    <option value="Error">{t('logLevels.error', 'Error')}</option>
                    <option value="Fatal">{t('logLevels.fatal', 'Fatal')}</option>
                </select>
            </section>

            <section>
                <h3>{t('settingsPage.ipcApiPort')}</h3>
                <input
                    type="number"
                    value={settings.ipcApiPort}
                    onInput={(e) => {
                        const port = parseInt((e.target as HTMLInputElement).value, 10);
                        updateSettings({ ipcApiPort: isNaN(port) ? 0 : port });
                    }}
                />
                <p><small>{t('settingsPage.ipcApiPortDescription')}</small></p>
            </section>

            <button onClick={saveSettings} disabled={isLoading}>
                {isLoading ? t('settingsPage.savingSettingsButton') : t('settingsPage.saveSettingsButton')}
            </button>
            {error && <p style={{ color: 'red' }}>{t('settingsPage.errorSaving', { error: error })}</p>} {/* Assuming key exists */}
        </div>
    );
}