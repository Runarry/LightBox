// import { h, Fragment } from 'preact'; // Removed unused imports h and Fragment
import { useEffect } from 'preact/hooks';
import { useWorkspaceStore } from '../../stores/workspaceStore';
import { useUIStore } from '../../stores/uiStore';
import WorkspaceList from './WorkspaceList';
import WorkspaceModal from './WorkspaceModal'; // To ensure it's rendered when needed
import './WorkspacePanel.css'; // Create this file for styling

// Placeholder icons - replace with actual icons or a library
const RefreshIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path fill-rule="evenodd" d="M8 3a5 5 0 1 0 4.546 2.914.5.5 0 0 1 .908-.417A6 6 0 1 1 8 2v1z"/><path d="M8 4.466V.534a.25.25 0 0 1 .41-.192l2.36 1.966c.12.1.12.284 0 .384L8.41 4.658A.25.25 0 0 1 8 4.466z"/></svg>;
const AddIcon = () => <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4z"/></svg>;

interface WorkspacePanelProps {
    isVisible: boolean; // To control panel visibility from parent
    onClose?: () => void; // Optional: If the panel itself has a close button or mechanism
}

const WorkspacePanel = ({ isVisible }: WorkspacePanelProps) => {
    const loadWorkspaces = useWorkspaceStore(state => state.loadWorkspaces);
    const openWorkspaceModal = useUIStore(state => state.openWorkspaceModal);

    // Load workspaces when the panel becomes visible or on initial mount if always visible
    useEffect(() => {
        if (isVisible) {
            loadWorkspaces();
            // Also try to load active workspace details if an ID is set but no data
            // This is also handled in workspaceStore, but can be triggered here too.
            const { activeWorkspaceId, activeWorkspace: currentActive, loadActiveWorkspace } = useWorkspaceStore.getState();
            if (activeWorkspaceId && !currentActive) {
                loadActiveWorkspace(activeWorkspaceId);
            }
        }
    }, [isVisible, loadWorkspaces]);

    const handleAddNewWorkspace = () => {
        openWorkspaceModal(null); // Open modal for creation (no pre-fill)
    };

    const handleRefreshWorkspaces = () => {
        loadWorkspaces();
        // Optionally, also refresh active workspace details
        const { activeWorkspaceId, loadActiveWorkspace } = useWorkspaceStore.getState();
        if (activeWorkspaceId) {
            loadActiveWorkspace(activeWorkspaceId);
        }
    };

    if (!isVisible) {
        return null;
    }

    return (
        <>
            <div className="workspace-panel">
                <div className="panel-header">
                    <h2>Workspaces</h2>
                    <div className="header-actions">
                        <button onClick={handleRefreshWorkspaces} aria-label="Refresh workspaces" title="Refresh" className="icon-button">
                            <RefreshIcon />
                        </button>
                        <button onClick={handleAddNewWorkspace} aria-label="Add new workspace" title="Add New" className="icon-button">
                            <AddIcon />
                        </button>
                        {/* Optional: Close button for the panel itself */}
                        {/* {onClose && <button onClick={onClose} aria-label="Close panel" title="Close" className="icon-button close-panel">&times;</button>} */}
                    </div>
                </div>
                <div className="panel-content">
                    <WorkspaceList />
                    {/* Display active workspace details if needed, or this is handled elsewhere */}
                    {/* {isLoadingActiveWorkspace && <p>Loading active workspace details...</p>}
                    {activeWorkspace && (
                        <div className="active-workspace-details">
                            <h3>Active: {activeWorkspace.name}</h3>
                            <p>{activeWorkspace.description}</p>
                        </div>
                    )} */}
                </div>
            </div>
            {/* WorkspaceModal is rendered here to be controlled by uiStore */}
            {/* It will be displayed on top of everything when its state is true */}
            <WorkspaceModal />
        </>
    );
};

export default WorkspacePanel;