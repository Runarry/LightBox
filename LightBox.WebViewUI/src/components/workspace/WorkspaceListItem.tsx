// import { h } from 'preact'; // Removed unused import h
import { useState } from 'preact/hooks';
import type { WorkspaceInfo } from '../../services/lightboxApi';
import { useWorkspaceStore } from '../../stores/workspaceStore';
import { useUIStore } from '../../stores/uiStore';
import './WorkspaceListItem.css'; // Create this file for styling

interface WorkspaceListItemProps {
    workspace: WorkspaceInfo;
    isActive: boolean;
}

const WorkspaceListItem = ({ workspace, isActive }: WorkspaceListItemProps) => {
    const { setActiveWorkspace, deleteWorkspace } = useWorkspaceStore(state => ({
        setActiveWorkspace: state.setActiveWorkspace,
        deleteWorkspace: state.deleteWorkspace,
    }));
    const { openWorkspaceModal, showModal: showUiStoreModal } = useUIStore(state => ({ // Added showModal
        openWorkspaceModal: state.openWorkspaceModal,
        showModal: state.showModal, // Added
    }));

    const [isMenuOpen, setIsMenuOpen] = useState(false);

    const handleSelectWorkspace = () => {
        if (!isActive) {
            setActiveWorkspace(workspace.id);
        }
        setIsMenuOpen(false); // Close menu if open
    };

    const handleEdit = (e: MouseEvent) => {
        e.stopPropagation(); // Prevent item click from firing
        // For editing, we pass WorkspaceInfo. The modal will handle it.
        // If WorkspaceModal needs full Workspace, uiStore.editingWorkspace might need to be Workspace type
        // and we might need to fetch full workspace details before opening modal,
        // or WorkspaceModal handles fetching if only WorkspaceInfo is provided.
        // For now, WorkspaceModal is set up to receive WorkspaceInfo | Workspace.
        openWorkspaceModal(workspace);
        setIsMenuOpen(false);
    };

    const handleDelete = async (e: MouseEvent) => {
        e.stopPropagation(); // Prevent item click from firing
        setIsMenuOpen(false);
        // The deleteWorkspace action in the store already handles confirmation.
        // We need to catch potential errors from it.
        try {
            await deleteWorkspace(workspace.id); // Assuming deleteWorkspace is async and might throw
        } catch (err: any) {
            console.error(`Error deleting workspace ${workspace.id}:`, err);
            const errorMessage = err.message || "An unexpected error occurred while deleting the workspace.";
            showUiStoreModal('Deletion Failed', `Could not delete workspace "${workspace.name}": ${errorMessage}`);
        }
    };

    const toggleMenu = (e: MouseEvent) => {
        e.stopPropagation(); // Prevent item click from firing
        setIsMenuOpen(!isMenuOpen);
    };
    
    // Placeholder for icon rendering
    const renderIcon = (iconName: string) => {
        // In a real app, you might use an icon library or SVGs
        // For now, just a simple text representation or a placeholder
        if (iconName?.toLowerCase().includes('briefcase')) return '💼';
        if (iconName?.toLowerCase().includes('code')) return '💻';
        if (iconName?.toLowerCase().includes('docs')) return '📄';
        return '📁'; // Default icon
    };

    return (
        <div
            className={`workspace-list-item ${isActive ? 'active' : ''}`}
            onClick={handleSelectWorkspace}
            role="button"
            tabIndex={0}
            onKeyDown={(e) => e.key === 'Enter' && handleSelectWorkspace()}
        >
            <span className="workspace-item-icon">{renderIcon(workspace.icon)}</span>
            <span className="workspace-item-name">{workspace.name}</span>
            <div className="workspace-item-actions">
                {isActive && ( // Show menu only for active (or always, depending on UX preference)
                    <button
                        className="more-options-button"
                        onClick={toggleMenu}
                        aria-label="More options"
                        aria-haspopup="true"
                        aria-expanded={isMenuOpen}
                    >
                        ...
                    </button>
                )}
                {isMenuOpen && (
                    <div className="actions-menu" role="menu">
                        <button role="menuitem" onClick={handleEdit}>Edit</button>
                        <button role="menuitem" onClick={handleDelete} className="delete">Delete</button>
                    </div>
                )}
            </div>
        </div>
    );
};

export default WorkspaceListItem;