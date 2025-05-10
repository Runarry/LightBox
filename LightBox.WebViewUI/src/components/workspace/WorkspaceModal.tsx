// import { h, Fragment } from 'preact'; // Removed unused imports h and Fragment
import { useState, useEffect } from 'preact/hooks';
import { useUIStore } from '../../stores/uiStore';
import { useWorkspaceStore } from '../../stores/workspaceStore';
import type { Workspace } from '../../services/lightboxApi';
import { Modal } from '../Modal'; // Corrected import based on linter error
import './WorkspaceModal.css'; // For specific styling

const WorkspaceModal = () => {
    const {
       isWorkspaceModalOpen,
       editingWorkspace,
       closeWorkspaceModal,
       showModal: showUiStoreModal // Renaming to avoid conflict with Modal component
   } = useUIStore(
        (state) => ({
            isWorkspaceModalOpen: state.isWorkspaceModalOpen,
            editingWorkspace: state.editingWorkspace,
            closeWorkspaceModal: state.closeWorkspaceModal,
            showModal: state.showModal,
        })
    );
    const { createWorkspace, updateWorkspace, error, clearError } = useWorkspaceStore(
        (state) => ({
            createWorkspace: state.createWorkspace,
            updateWorkspace: state.updateWorkspace,
            error: state.error,
            clearError: state.clearError,
        })
    );

    const [name, setName] = useState('');
    const [icon, setIcon] = useState('');
    // Add description if your Workspace model includes it and you want to edit it here
    // const [description, setDescription] = useState(''); 

    const isEditMode = !!editingWorkspace;

    useEffect(() => {
        if (isWorkspaceModalOpen) {
            clearError(); // Clear previous errors when modal opens
            if (editingWorkspace) {
                setName(editingWorkspace.name);
                setIcon(editingWorkspace.icon || ''); // Icon might be optional
                // if ('description' in editingWorkspace && editingWorkspace.description) {
                //     setDescription(editingWorkspace.description);
                // } else {
                //     setDescription('');
                // }
            } else {
                setName('');
                setIcon('');
                // setDescription('');
            }
        }
    }, [isWorkspaceModalOpen, editingWorkspace, clearError]);

    const handleSubmit = async (e: Event) => {
        e.preventDefault();
        if (!name.trim()) {
            // Basic validation, consider a more robust solution
            showUiStoreModal('Validation Error', 'Workspace name cannot be empty.');
            return;
        }

        let success = false;
        try {
            if (isEditMode && editingWorkspace) {
                const workspaceToUpdate: Workspace = {
                    ...(editingWorkspace as Workspace),
                    name,
                    icon,
                };
                await updateWorkspace(workspaceToUpdate); // This action should throw on error
                success = true;
            } else {
                await createWorkspace(name, icon); // This action should throw on error
                success = true;
            }

            if (success) {
                closeWorkspaceModal(); // Close modal on success
            }
        } catch (err: any) {
            // Errors from workspaceStore actions (create/update) should be caught here
            // The workspaceStore itself might set an error state, but we also want to show a modal.
            console.error("Error in handleSubmit (WorkspaceModal):", err);
            const errorMessage = err.message || "An unexpected error occurred.";
            showUiStoreModal(isEditMode ? 'Update Failed' : 'Creation Failed', `Could not ${isEditMode ? 'update' : 'create'} workspace: ${errorMessage}`);
            // The error state in workspaceStore might still be useful for displaying inline errors if needed.
        }
        // No need to check workspaceStore.error here if actions throw and we catch it.
    };

    if (!isWorkspaceModalOpen) {
        return null;
    }

    const modalTitle = isEditMode ? 'Edit Workspace' : 'Add New Workspace';

    return (
        <Modal title={modalTitle} onClose={closeWorkspaceModal} show={isWorkspaceModalOpen}>
            <form onSubmit={handleSubmit} className="workspace-form">
                {error && <div className="form-error">Error: {error}</div>}
                <div className="form-group">
                    <label htmlFor="workspaceName">Name:</label>
                    <input
                        type="text"
                        id="workspaceName"
                        value={name}
                        onInput={(e) => setName((e.target as HTMLInputElement).value)}
                        required
                    />
                </div>
                <div className="form-group">
                    <label htmlFor="workspaceIcon">Icon (e.g., "briefcase", "code"):</label>
                    <input
                        type="text"
                        id="workspaceIcon"
                        value={icon}
                        onInput={(e) => setIcon((e.target as HTMLInputElement).value)}
                    />
                    <small>Enter an icon identifier. Actual icon rendering depends on system/library.</small>
                </div>
                {/* 
                <div className="form-group">
                    <label htmlFor="workspaceDescription">Description (Optional):</label>
                    <textarea
                        id="workspaceDescription"
                        value={description}
                        onInput={(e) => setDescription((e.target as HTMLTextAreaElement).value)}
                    />
                </div>
                */}
                <div className="form-actions">
                    <button type="submit" className="button-primary">
                        {isEditMode ? 'Save Changes' : 'Create Workspace'}
                    </button>
                    <button type="button" onClick={closeWorkspaceModal} className="button-secondary">
                        Cancel
                    </button>
                </div>
            </form>
        </Modal>
    );
};

export default WorkspaceModal;