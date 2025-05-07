import { useEffect, useState } from 'preact/hooks';
import './app.css'; // Styles for the new layout
import { initLightboxApiService } from './services/lightboxApi';
import { useUIStore } from './stores/uiStore'; // Import store
import { Modal } from './components/Modal'; // Import Modal component
// import type { ViewMode as UIStoreViewMode } from './stores/uiStore'; // Import type - Removed as unused

// Placeholder types for now, will be refined
// type ViewMode = 'card' | 'list'; // This is now in uiStore.ts
type Workspace = { id: string; name: string; icon?: string; }; // Added icon property
type PluginInstance = { id: string; name: string; type: string; icon?: string; };

// --- TopBar Component ---
const TopBar = () => {
  // const [viewMode, setViewMode] = useState<ViewMode>('card'); // Removed local state
  const { viewMode, setViewMode } = useUIStore(); // Use Zustand store

  // toggleViewMode is now directly using setViewMode from the store
  // const toggleViewMode = (mode: UIStoreViewMode) => {
  //   setViewMode(mode);
  //   console.log("View mode set to:", mode);
  // };

  const { showModal } = useUIStore(); // Get showModal for use in this component

  const handleOpenLogs = () => {
    // TODO: Implement log window opening
    console.log("Open logs clicked");
    showModal("Log Viewer", "Log window functionality to be implemented.");
  };

  return (
    <div class="top-bar">
      <div class="top-bar-left">
        <span class="app-title">LightBox</span>
        <div class="view-switch-buttons">
          <button 
            onClick={() => setViewMode('card')} 
            class={viewMode === 'card' ? 'active' : ''}
            aria-label="Card View"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
              <path d="M1 2.5A1.5 1.5 0 0 1 2.5 1h3A1.5 1.5 0 0 1 7 2.5v3A1.5 1.5 0 0 1 5.5 7h-3A1.5 1.5 0 0 1 1 5.5v-3zm8 0A1.5 1.5 0 0 1 10.5 1h3A1.5 1.5 0 0 1 15 2.5v3A1.5 1.5 0 0 1 13.5 7h-3A1.5 1.5 0 0 1 9 5.5v-3zm-8 8A1.5 1.5 0 0 1 2.5 9h3A1.5 1.5 0 0 1 7 10.5v3A1.5 1.5 0 0 1 5.5 15h-3A1.5 1.5 0 0 1 1 13.5v-3zm8 0A1.5 1.5 0 0 1 10.5 9h3A1.5 1.5 0 0 1 15 10.5v3A1.5 1.5 0 0 1 13.5 15h-3A1.5 1.5 0 0 1 9 13.5v-3z"/>
            </svg>
          </button>
          <button 
            onClick={() => setViewMode('list')} 
            class={viewMode === 'list' ? 'active' : ''}
            aria-label="List View"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
              <path fill-rule="evenodd" d="M2.5 12a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5z"/>
            </svg>
          </button>
        </div>
      </div>
      <div class="global-log-button">
        <button onClick={handleOpenLogs} aria-label="Open Logs">
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
  const { viewMode, showModal } = useUIStore(); // Use Zustand store to get viewMode and showModal
  // Mock data for now
  const plugins: PluginInstance[] = [
    { id: '1', name: 'My Awesome Tool', type: 'Data Analyzer', icon: '📊' },
    { id: '2', name: 'Quick Note Taker', type: 'Utility', icon: '📝' },
    { id: '3', name: 'Game Asset Viewer', type: 'Graphics', icon: '🖼️' },
    { id: '4', name: 'Another Tool', type: 'Utility' },
  ];

  const handleAddPlugin = () => {
    console.log("Add plugin clicked");
    showModal("Add Plugin", "Add plugin functionality to be implemented.");
  };

  const handlePluginClick = (plugin: PluginInstance) => {
    console.log("Plugin clicked:", plugin.name);
    // This alert can be removed if starting a plugin doesn't need immediate user feedback via modal
    // For now, let's assume it might show a quick "Starting..." or success/failure from JSBridge
    showModal("Plugin Action", `Attempting to start plugin: ${plugin.name}`);
  };
  
  const handlePluginConfigure = (plugin: PluginInstance) => {
    console.log("Configure plugin:", plugin.name);
    showModal("Configure Plugin", `Configuration dialog for ${plugin.name} to be implemented here.`);
  };

  const handlePluginContextMenu = (plugin: PluginInstance, event: MouseEvent) => {
    event.preventDefault();
    console.log(`Context menu for plugin: ${plugin.name} at (${event.clientX}, ${event.clientY})`);
    
    showModal(
      `Actions for ${plugin.name}`,
      `Select an action for "${plugin.name}":`,
      [
        { label: 'Open', onClick: () => handlePluginClick(plugin), className: 'primary' },
        { label: 'Configure', onClick: () => handlePluginConfigure(plugin) },
        { label: 'Cancel', onClick: () => useUIStore.getState().hideModal(), className: 'secondary' }
      ]
    );
  };

  return (
    <div class="main-content">
      <button onClick={handleAddPlugin} class="add-plugin-button">
        + Add Plugin
      </button>
      
      {/* Conditional rendering based on viewMode */}
      {viewMode === 'card' ? (
        <div class="plugin-grid">
          {plugins.map(plugin => (
            <div
              key={plugin.id}
              class="plugin-card"
              onClick={() => handlePluginClick(plugin)}
              onContextMenu={(e) => handlePluginContextMenu(plugin, e)}
              title={`${plugin.name} (${plugin.type})`} // Hover tooltip
            >
              {plugin.icon && <span class="plugin-icon">{plugin.icon}</span>}
              {/* Name and type removed from direct display in card mode */}
            </div>
          ))}
        </div>
      ) : (
        <div class="plugin-list">
          {plugins.map(plugin => (
            <div
              key={plugin.id}
              class="plugin-list-item"
              onClick={() => handlePluginClick(plugin)}
              onContextMenu={(e) => handlePluginContextMenu(plugin, e)}
            >
              <div class="plugin-info-container">
                {plugin.icon && <span class="plugin-icon">{plugin.icon}</span>}
                <div>
                  <h5>{plugin.name}</h5>
                  <p>{plugin.type}</p>
                </div>
              </div>
              {/* Configure button removed */}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// --- BottomBar Component ---
const BottomBar = () => {
  const { showModal: showBottomBarModal } = useUIStore(); // Alias to avoid conflict if needed, or ensure context
  // Mock data for now
  const workspaces: Workspace[] = [
    { id: 'ws1', name: 'Project Alpha', icon: '🚀' },
    { id: 'ws2', name: 'Sandbox Environment', icon: '🧪' },
    { id: 'ws3', name: 'Game Design Docs', icon: '🎮' },
    { id: 'ws4', name: 'Notes', icon: '📋' },
  ];
  const [activeWorkspace, setActiveWorkspace] = useState<string>('ws1');

  const handleWorkspaceClick = (workspaceId: string) => {
    setActiveWorkspace(workspaceId);
    console.log("Workspace changed to:", workspaceId);
  };

  const handleWorkspaceRightClick = (event: MouseEvent, workspaceId: string) => {
    event.preventDefault();
    console.log("Right-clicked on workspace:", workspaceId);
    // Example for workspace context menu using the modal
    const ws = workspaces.find(w => w.id === workspaceId);
    showBottomBarModal(
      `Workspace: ${ws?.name || workspaceId}`,
      "Select workspace action:",
      [
        { label: 'Rename', onClick: () => showBottomBarModal("Rename", `Rename functionality for ${ws?.name} to be implemented.`) },
        { label: 'Delete', onClick: () => showBottomBarModal("Delete", `Delete confirmation for ${ws?.name} to be implemented.`, [{label: "Confirm Delete", onClick: () => console.log("Delete confirmed for", ws?.name), className: 'danger'}, {label: "Cancel", onClick: () => useUIStore.getState().hideModal()} ]) },
        { label: 'Cancel', onClick: () => useUIStore.getState().hideModal(), className: 'secondary' }
      ]
    );
  };
  
  const handleAddWorkspace = () => {
    console.log("Add workspace clicked");
    showBottomBarModal("Add Workspace", "Functionality to add a new workspace (max 5) to be implemented.");
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
            {ws.icon || ws.name.charAt(0).toUpperCase()}
          </button>
        ))}
        {workspaces.length < 5 && (
          <button onClick={handleAddWorkspace} title="Add new workspace" aria-label="Add new workspace" style={{ fontSize: '1.2rem' }}>
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
      <Modal /> {/* Render Modal component at the top level */}
    </div>
  );
}
