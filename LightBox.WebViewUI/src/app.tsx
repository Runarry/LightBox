import { LocationProvider, Router, Route } from 'preact-iso'; // Removed Link
import './app.css';

// Import pages
import { SettingsPage } from './pages/SettingsPage';
import { PluginsPage } from './pages/PluginsPage';

// A simple Home component for the root path
const HomePage = () => (
    <div>
        <h1>Welcome to LightBox</h1>
        <p>Use the navigation above to manage settings or view plugins.</p>
        <p>
            This UI interacts with the backend via <code>window.lightboxBridge</code>.
        </p>
        <JSBridgeTest />
    </div>
);

// A simple Not Found component
const NotFound = () => (
    <section>
        <h1>404: Not Found</h1>
        <p>It's gone :(</p>
    </section>
);

// Component to test JSBridge interactions (can be expanded or removed later)
const JSBridgeTest = () => {
    const testGetSettings = async () => {
        if (window.lightboxBridge?.getApplicationSettings) {
            try {
                const settingsJson = await window.lightboxBridge.getApplicationSettings();
                console.log('Raw settings from bridge:', settingsJson);
                const settings = JSON.parse(settingsJson);
                alert('Application Settings:\n' + JSON.stringify(settings, null, 2));
            } catch (e) {
                console.error('Error getting settings via bridge:', e);
                alert('Error getting settings: ' + (e instanceof Error ? e.message : String(e)));
            }
        } else {
            alert('lightboxBridge.getApplicationSettings is not available.');
        }
    };

    const testGetPlugins = async () => {
        if (window.lightboxBridge?.getAllPluginDefinitions) {
            try {
                const pluginsJson = await window.lightboxBridge.getAllPluginDefinitions();
                console.log('Raw plugins from bridge:', pluginsJson);
                const plugins = JSON.parse(pluginsJson);
                alert('Plugin Definitions:\n' + JSON.stringify(plugins, null, 2));
            } catch (e) {
                console.error('Error getting plugins via bridge:', e);
                alert('Error getting plugins: ' + (e instanceof Error ? e.message : String(e)));
            }
        } else {
            alert('lightboxBridge.getAllPluginDefinitions is not available.');
        }
    };
    
    return (
        <div style={{ marginTop: '20px', borderTop: '1px solid #eee', paddingTop: '10px' }}>
            <h4>JSBridge Test Area</h4>
            <button onClick={testGetSettings}>Test GetApplicationSettings</button>
            <button onClick={testGetPlugins} style={{ marginLeft: '10px' }}>Test GetAllPluginDefinitions</button>
        </div>
    );
};


export function App() {
    return (
        <LocationProvider>
            <div class="app">
                <nav>
                    <a href="/" class="nav-link">Home</a>
                    <a href="/settings" class="nav-link">Settings</a>
                    <a href="/plugins" class="nav-link">Plugins</a>
                </nav>
                <main>
                    <Router>
                        <Route path="/" component={HomePage} />
                        <Route path="/settings" component={SettingsPage} />
                        <Route path="/plugins" component={PluginsPage} />
                        <Route default component={NotFound} />
                    </Router>
                </main>
            </div>
        </LocationProvider>
    );
}
