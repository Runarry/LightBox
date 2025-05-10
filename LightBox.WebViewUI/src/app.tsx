import { useEffect } from 'preact/hooks';
import './app.css'; // Styles for the new layout
import { initLightboxApiService } from './services/lightboxApi';
import { useUIStore } from './stores/uiStore'; // Import store
// Modal component from uiStore is now for generic alerts/confirms.
// WorkspaceModal is specific and self-contained, rendered by WorkspacePanel.
// import { Modal } from './components/Modal'; // This Modal is the generic one.
import WorkspacePanel from './components/workspace/WorkspacePanel'; // Import WorkspacePanel
import { Icon } from './components/common/Icon'; // Import Icon component

// Placeholder types for now, will be refined
// type ViewMode = 'card' | 'list'; // This is now in uiStore.ts
// type Workspace = { id: string; name: string; icon?: string; }; // This type is now in lightboxApi.ts
type PluginInstance = { id: string; name: string; type: string; icon?: string; };

import { useTranslation } from 'react-i18next'; // Import useTranslation

// --- TopBar Component ---
const TopBar = () => {
  const viewMode = useUIStore(state => state.viewMode);
  const setViewMode = useUIStore(state => state.setViewMode);
  const showModal = useUIStore(state => state.showModal);
  const currentLanguage = useUIStore(state => state.currentLanguage);
  const supportedLanguages = useUIStore(state => state.supportedLanguages);
  const setLanguage = useUIStore(state => state.setLanguage);
  const toggleWorkspacePanel = useUIStore(state => state.toggleWorkspacePanel);
  const { t } = useTranslation();

  const handleOpenLogs = () => {
    // TODO: Implement log window opening
    console.log("Open logs clicked");
    showModal(t('topbar.logViewerTitle', "Log Viewer"), t('topbar.logViewerMessage', "Log window functionality to be implemented."));
  };

  return (
    <div class="top-bar">
      <div class="top-bar-left">
        <button onClick={toggleWorkspacePanel} class="workspace-toggle-button" title={t('topbar.toggleWorkspaces', "Toggle Workspaces")}>
          <Icon name="workspaces" />
        </button>
        <span class="app-title">{t('appTitle', 'LightBox')}</span>
        <div class="view-switch-buttons">
          <button
            onClick={() => setViewMode('card')}
            class={viewMode === 'card' ? 'active' : ''}
            aria-label={t('topbar.cardViewAriaLabel', "Card View")}
            title={t('topbar.cardViewTitle', "Card View")}
          >
            <Icon name="card-view" />
          </button>
          <button
            onClick={() => setViewMode('list')}
            class={viewMode === 'list' ? 'active' : ''}
            aria-label={t('topbar.listViewAriaLabel', "List View")}
            title={t('topbar.listViewTitle', "List View")}
          >
            <Icon name="list-view" />
          </button>
        </div>
      </div>
      <div class="top-bar-right"> {/* Changed class for better grouping */}
        <div class="language-switcher-container"> {/* Container for styling */}
          {supportedLanguages && supportedLanguages.length > 0 && (
            <select
              value={currentLanguage}
              onChange={(e) => setLanguage((e.target as HTMLSelectElement).value)}
              aria-label={t('settingsPage.language', "Select Language")} /* Reusing existing key */
              class="language-select" /* Added class for styling */
            >
              {supportedLanguages.map(lang => (
                <option key={lang.code} value={lang.code}>{lang.name}</option>
              ))}
            </select>
          )}
        </div>
        <div class="global-log-button">
          <button onClick={handleOpenLogs} aria-label={t('topbar.openLogsAriaLabel', "Open Logs")} title={t('topbar.openLogsTitle', "Open Logs")}>
            <Icon name="logs" />
          </button>
        </div>
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
  // This component might be removed or simplified if WorkspacePanel handles all workspace interactions.
  // For now, let's keep it minimal or as a status bar.
  const { t } = useTranslation();
  // Potentially display active workspace name here from workspaceStore
  // const { activeWorkspace } = useWorkspaceStore(state => ({ activeWorkspace: state.activeWorkspace }));

  return (
    <div class="bottom-bar">
      {/* {activeWorkspace ? <span>{t('bottombar.activeWorkspace', "Active Workspace:")} {activeWorkspace.name}</span> : <span>{t('bottombar.noActiveWorkspace', "No active workspace")}</span>} */}
      <span>{t('bottombar.statusReady', "Ready")}</span>
    </div>
  );
};

// --- App Component ---
export function App() {
  console.log("【App 组件】App 组件函数体执行");
  // 避免 selector 死循环：分别调用 useUIStore
  const isModalOpen = useUIStore(state => state.isModalOpen);
  const modalTitle = useUIStore(state => state.modalTitle);
  const modalMessage = useUIStore(state => state.modalMessage);
  const modalActions = useUIStore(state => state.modalActions);
  const hideModal = useUIStore(state => state.hideModal);
  const isWorkspacePanelOpen = useUIStore(state => state.isWorkspacePanelOpen);
  const closeWorkspacePanel = useUIStore(state => state.closeWorkspacePanel);
  
  useEffect(() => {
    console.log("【App 组件】useEffect 初始化执行");
    initLightboxApiService().then(success => {
      if (success) {
        console.info("Lightbox API Service initialized successfully.");
        // Potentially load initial data like workspaces or active workspace here
        // useWorkspaceStore.getState().loadWorkspaces();
        // useWorkspaceStore.getState().loadActiveWorkspace(); // if an ID might be pre-set
      } else {
        console.error("Failed to initialize Lightbox API Service.");
        // Show a global error modal?
        useUIStore.getState().showModal("Initialization Error", "Failed to connect to the backend service. Some features may not work.");
      }
    });
    // console.log("【App 组件】initLightboxApiService 调用已再次注释"); // Re-enable the call
  }, []);

  console.log("【App 组件】准备执行 return 语句");
  return (
    <div class="app-container">
      {/* {console.log("【App 组件】开始渲染 return 内容")} */}
      <TopBar /> {/* TopBar will now use toggleWorkspacePanel from uiStore directly */}
      <WorkspacePanel isVisible={isWorkspacePanelOpen} onClose={closeWorkspacePanel} />
      <MainContent />
      <BottomBar />
      {/* Generic Modal for alerts/confirmations from uiStore */}
      {isModalOpen && (
        <div class="modal-overlay" onClick={hideModal}>
            <div class="modal-content" onClick={(e) => e.stopPropagation()}>
                {modalTitle && <div class="modal-header"><h2>{modalTitle}</h2></div>}
                <div class="modal-body">
                    {typeof modalMessage === 'string' ? <p>{modalMessage}</p> : modalMessage}
                </div>
                {modalActions && modalActions.length > 0 && (
                    <div class="modal-footer">
                        {modalActions.map((action, index) => (
                            <button key={index} onClick={action.onClick} class={`modal-button ${action.className || ''}`}>
                                {action.label}
                            </button>
                        ))}
                    </div>
                )}
            </div>
        </div>
      )}
    </div>
  );
}
