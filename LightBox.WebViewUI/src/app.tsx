import { useEffect, useState } from 'preact/hooks';
import './app.css'; // Styles for the new layout
import { initLightboxApiService } from './services/lightboxApi';

// Placeholder types for now, will be refined
type ViewMode = 'card' | 'list';
type Workspace = { id: string; name: string; };
type PluginInstance = { id: string; name: string; type: string; icon?: string; };

// --- TopBar Component ---
const TopBar = () => {
  const [viewMode, setViewMode] = useState<ViewMode>('card'); // Default to card view

  const toggleViewMode = (mode: ViewMode) => {
    setViewMode(mode);
    // TODO: Add logic to actually change the view in MainContent
    console.log("View mode set to:", mode);
  };

  const handleOpenLogs = () => {
    // TODO: Implement log window opening
    console.log("Open logs clicked");
    alert("Log window functionality to be implemented.");
  };

  return (
    <div class="top-bar">
      <div class="top-bar-left">
        <span class="app-title">LightBox</span>
        <div class="view-switch-buttons">
          <button 
            onClick={() => toggleViewMode('card')} 
            class={viewMode === 'card' ? 'active' : ''}
            aria-label="Card View"
          >
            {/* Placeholder for Card Icon - e.g., SVG or Font Icon */}
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
              <path d="M1 2.5A1.5 1.5 0 0 1 2.5 1h3A1.5 1.5 0 0 1 7 2.5v3A1.5 1.5 0 0 1 5.5 7h-3A1.5 1.5 0 0 1 1 5.5v-3zm8 0A1.5 1.5 0 0 1 10.5 1h3A1.5 1.5 0 0 1 15 2.5v3A1.5 1.5 0 0 1 13.5 7h-3A1.5 1.5 0 0 1 9 5.5v-3zm-8 8A1.5 1.5 0 0 1 2.5 9h3A1.5 1.5 0 0 1 7 10.5v3A1.5 1.5 0 0 1 5.5 15h-3A1.5 1.5 0 0 1 1 13.5v-3zm8 0A1.5 1.5 0 0 1 10.5 9h3A1.5 1.5 0 0 1 15 10.5v3A1.5 1.5 0 0 1 13.5 15h-3A1.5 1.5 0 0 1 9 13.5v-3z"/>
            </svg>
          </button>
          <button 
            onClick={() => toggleViewMode('list')} 
            class={viewMode === 'list' ? 'active' : ''}
            aria-label="List View"
          >
            {/* Placeholder for List Icon */}
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
              <path fill-rule="evenodd" d="M2.5 12a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5z"/>
            </svg>
          </button>
        </div>
      </div>
      <div class="global-log-button">
        <button onClick={handleOpenLogs} aria-label="Open Logs">
          {/* Placeholder for Log Icon */}
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
            <path d="M0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2zm5.5 10.5a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 0-1H6a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 0-1H6a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 0-1H6a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 0-1H6a.5.5 0 0 0-.5.5zm-2 4a.5.5 0 0 0 .5.5h.793l.5.5.5-.5h.793a.5.5 0 0 0 0-1H4a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h.793l.5.5.5-.5h.793a.5.5 0 0 0 0-1H4a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h.793l.5.5.5-.5h.793a.5.5 0 0 0 0-1H4a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h.793l.5.5.5-.5h.793a.5.5 0 0 0 0-1H4a.5.5 0 0 0-.5.5z"/>
          </svg>
        </button>
      </div>
    </div>
  );
};

// --- MainContent Component ---
const MainContent = () => {
  // Mock data for now
  const plugins: PluginInstance[] = [
    { id: '1', name: 'My Awesome Tool', type: 'Data Analyzer' },
    { id: '2', name: 'Quick Note Taker', type: 'Utility' },
    { id: '3', name: 'Game Asset Viewer', type: 'Graphics' },
  ];

  const handleAddPlugin = () => {
    // TODO: Implement add plugin dialog
    console.log("Add plugin clicked");
    alert("Add plugin functionality to be implemented.");
  };

  const handlePluginClick = (plugin: PluginInstance) => {
    // TODO: Implement plugin start logic via JSBridge
    console.log("Plugin clicked:", plugin.name);
    alert(`Starting plugin: ${plugin.name}`);
  };
  
  const handlePluginConfigure = (plugin: PluginInstance, event: MouseEvent) => {
    event.stopPropagation(); // Prevent click on parent from firing
    // TODO: Implement plugin configuration dialog
    console.log("Configure plugin:", plugin.name);
    alert(`Configure plugin: ${plugin.name} (dialog to be implemented)`);
  };

  // This is a very basic representation. Card/List view logic will be more complex.
  // For now, just listing them.
  return (
    <div class="main-content">
      <button onClick={handleAddPlugin} style={{ marginBottom: '1rem' }}>
        + Add Plugin
      </button>
      <div class="plugin-grid"> {/* Placeholder for actual grid/list styling */}
        {plugins.map(plugin => (
          <div 
            key={plugin.id} 
            class="plugin-card" /* Style this for card view */
            onClick={() => handlePluginClick(plugin)}
            style={{ border: '1px solid #ccc', padding: '1rem', marginBottom: '0.5rem', cursor: 'pointer', backgroundColor: 'white' }}
          >
            <h4>{plugin.name}</h4>
            <p>Type: {plugin.type}</p>
            <button 
              onClick={(e) => handlePluginConfigure(plugin, e)}
              style={{marginTop: '0.5rem', fontSize: '0.8rem', padding: '0.25rem 0.5rem'}}
            >
              Configure (...)
            </button>
          </div>
        ))}
      </div>
    </div>
  );
};

// --- BottomBar Component ---
const BottomBar = () => {
  // Mock data for now
  const workspaces: Workspace[] = [
    { id: 'ws1', name: 'Project A' },
    { id: 'ws2', name: 'Sandbox' },
    { id: 'ws3', name: 'Game Design Docs' },
  ];
  const [activeWorkspace, setActiveWorkspace] = useState<string>('ws1');

  const handleWorkspaceClick = (workspaceId: string) => {
    setActiveWorkspace(workspaceId);
    // TODO: Add logic to load plugins for this workspace
    console.log("Workspace changed to:", workspaceId);
  };

  const handleWorkspaceRightClick = (event: MouseEvent, workspaceId: string) => {
    event.preventDefault();
    // TODO: Implement context menu for workspace settings
    console.log("Right-clicked on workspace:", workspaceId);
    alert(`Workspace settings for ${workspaceId} (context menu to be implemented)`);
  };
  
  const handleAddWorkspace = () => {
     // TODO: Implement add workspace logic
    console.log("Add workspace clicked");
    alert("Add workspace functionality to be implemented (max 5 workspaces).");
  };

  return (
    <div class="bottom-bar">
      <div class="workspace-buttons">
        {workspaces.map(ws => (
          <button 
            key={ws.id} 
            onClick={() => handleWorkspaceClick(ws.id)}
            onContextMenu={(e) => handleWorkspaceRightClick(e, ws.id)}
            class={activeWorkspace === ws.id ? 'active' : ''}
            title={ws.name}
          >
            {/* Display first letter or a short identifier. Full name on hover (title attribute) */}
            {ws.name.substring(0, 2).toUpperCase()}
          </button>
        ))}
        {workspaces.length < 5 && (
          <button onClick={handleAddWorkspace} title="Add new workspace" aria-label="Add new workspace">
            +
          </button>
        )}
      </div>
    </div>
  );
};

// --- App Component ---
export function App() {
  useEffect(() => {
    initLightboxApiService().then(success => {
      if (success) {
        console.info("Lightbox API Service initialized successfully.");
      } else {
        console.error("Failed to initialize Lightbox API Service.");
      }
    });
  }, []);

  return (
    <div class="app-container">
      <TopBar />
      <MainContent />
      <BottomBar />
    </div>
  );
}
