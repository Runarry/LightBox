import { useEffect } from 'preact/hooks'; // Import useEffect
import { LocationProvider, Router, Route } from 'preact-iso';
import './app.css';
import { initLightboxApiService } from './services/lightboxApi'; // Import the init function

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

import { getApplicationSettings, getAllPluginDefinitions } from './services/lightboxApi'; // Import the new API functions

// Component to test JSBridge interactions (can be expanded or removed later)
const JSBridgeTest = () => {
    const testGetSettings = async () => {
        try {
            // Use the imported function
            const settings = await getApplicationSettings();
            console.log('Settings from lightboxApi:', settings);
            alert('Application Settings:\n' + JSON.stringify(settings, null, 2));
        } catch (e) {
            console.error('Error getting settings via lightboxApi:', e);
            alert('Error getting settings: ' + (e instanceof Error ? e.message : String(e)));
        }
    };

    const testGetPlugins = async () => {
        try {
            // Use the imported function
            const plugins = await getAllPluginDefinitions();
            console.log('Plugins from lightboxApi:', plugins);
            alert('Plugin Definitions:\n' + JSON.stringify(plugins, null, 2));
        } catch (e) {
            console.error('Error getting plugins via lightboxApi:', e);
            alert('Error getting plugins: ' + (e instanceof Error ? e.message : String(e)));
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
    useEffect(() => {
        // Initialize the API service when the App component mounts
        initLightboxApiService().then(success => {
            if (success) {
                console.info("Lightbox API Service initialized successfully.");
            } else {
                console.error("Failed to initialize Lightbox API Service.");
                // Optionally, display an error to the user or retry
            }
        });
    }, []); // Empty dependency array ensures this runs only once on mount

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
