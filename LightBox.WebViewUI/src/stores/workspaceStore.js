import { create } from 'zustand';
import {
    getWorkspaces as apiGetWorkspaces,
    createWorkspace as apiCreateWorkspace,
    setActiveWorkspace as apiSetActiveWorkspace,
    getActiveWorkspace as apiGetActiveWorkspace,
    updateWorkspace as apiUpdateWorkspace,
    deleteWorkspace as apiDeleteWorkspace,
    // type Workspace, // Removed TS interface
    // type WorkspaceInfo, // Removed TS interface
} from '../services/lightboxApi';
// import { useUIStore } from './uiStore'; // For potential interaction like closing modal on success // TODO: [UI Refactor] uiStore.ts was removed, replace with new UI interaction logic if needed

// interface WorkspaceState { ... } // Removed TS interface

export const useWorkspaceStore = create((set, get) => ({
    workspaces: [],
    activeWorkspace: null,
    activeWorkspaceId: null, // Keep track of ID for faster lookups or UI state
    isLoadingWorkspaces: false,
    isLoadingActiveWorkspace: false,
    error: null,

    // Actions
    loadWorkspaces: async () => {
        set({ isLoadingWorkspaces: true, error: null });
        try {
            const workspaces = await apiGetWorkspaces();
            set({ workspaces, isLoadingWorkspaces: false });
        } catch (err) {
            console.error("Failed to load workspaces:", err);
            set({ error: err.message || 'Failed to load workspaces', isLoadingWorkspaces: false });
        }
    },

    createWorkspace: async (name, icon) => {
        set({ error: null });
        try {
            const newWorkspace = await apiCreateWorkspace(name, icon);
            set((state) => ({
                workspaces: [...state.workspaces, newWorkspace],
            }));
            // Optionally, close the modal via uiStore
            // TODO: [UI Refactor] uiStore.ts was removed. Implement alternative for closing modal.
            // useUIStore.getState().closeWorkspaceModal();
            return newWorkspace;
        } catch (err) {
            console.error("Failed to create workspace:", err);
            set({ error: err.message || 'Failed to create workspace' });
            return undefined;
        }
    },

    setActiveWorkspace: async (workspaceId) => {
        set({ error: null, activeWorkspaceId: workspaceId });
        try {
            await apiSetActiveWorkspace(workspaceId);
            // After setting, immediately load the full active workspace data
            await get().loadActiveWorkspace(workspaceId);
        } catch (err) {
            console.error(`Failed to set active workspace ${workspaceId}:`, err);
            set({ error: err.message || `Failed to set active workspace ${workspaceId}`, activeWorkspaceId: null, activeWorkspace: null });
        }
    },

    loadActiveWorkspace: async (workspaceId) => {
        const idToLoad = workspaceId || get().activeWorkspaceId;
        if (!idToLoad) {
            set({ activeWorkspace: null, isLoadingActiveWorkspace: false }); // Clear if no ID
            return;
        }
        set({ isLoadingActiveWorkspace: true, error: null });
        try {
            const workspace = await apiGetActiveWorkspace(); // JSBridge should know current active one
            if (workspace && workspace.id === idToLoad) {
                set({ activeWorkspace: workspace, activeWorkspaceId: workspace.id, isLoadingActiveWorkspace: false });
            } else if (!workspace && get().activeWorkspaceId === idToLoad) {
                // If API returns null but we expected this one, it means it's no longer active or doesn't exist
                set({ activeWorkspace: null, isLoadingActiveWorkspace: false, activeWorkspaceId: null });
                 console.warn(`Active workspace ${idToLoad} could not be loaded or is no longer active.`);
            } else {
                 // Loaded a different workspace than expected, or null when a different one was active
                set({ isLoadingActiveWorkspace: false }); // Keep current activeWorkspace if any
                if (workspace) { // if a workspace was loaded, but not the one we tried to set active
                    console.warn(`Loaded workspace ${workspace.id} but expected ${idToLoad} or another active one.`);
                } else {
                    console.warn(`Expected to load active workspace ${idToLoad} but got null and it wasn't the active one.`);
                }
            }
        } catch (err) {
            console.error(`Failed to load active workspace ${idToLoad}:`, err);
            set({ error: err.message || `Failed to load active workspace ${idToLoad}`, isLoadingActiveWorkspace: false, activeWorkspace: null, activeWorkspaceId: null });
        }
    },

    updateWorkspace: async (workspace) => {
        set({ error: null });
        try {
            await apiUpdateWorkspace(workspace);
            set((state) => ({
                workspaces: state.workspaces.map((ws) =>
                    ws.id === workspace.id ? { ...ws, name: workspace.name, icon: workspace.icon } : ws
                ),
                activeWorkspace: state.activeWorkspace?.id === workspace.id ? workspace : state.activeWorkspace,
            }));
            // Optionally, close the modal via uiStore
            // TODO: [UI Refactor] uiStore.ts was removed. Implement alternative for closing modal.
            // useUIStore.getState().closeWorkspaceModal();
        } catch (err) {
            console.error(`Failed to update workspace ${workspace.id}:`, err);
            set({ error: err.message || `Failed to update workspace ${workspace.id}` });
        }
    },

    deleteWorkspace: async (workspaceId) => {
        set({ error: null });
        try {
            // Basic confirmation, ideally use a proper modal from uiStore
            // const confirmed = window.confirm(`Are you sure you want to delete workspace ${workspaceId}?`);
            // if (!confirmed) return;
            
            // TODO: [UI Refactor] uiStore.ts was removed. Implement proper confirmation and error handling for delete.
            // For now, deletion will proceed without UI confirmation.
            // const uiActions = useUIStore.getState();
            // uiActions.showModal( ... );

            // Directly attempt deletion
            set({ isLoadingWorkspaces: true }); // Indicate loading state
            try {
                await apiDeleteWorkspace(workspaceId);
                set((state) => ({
                    workspaces: state.workspaces.filter((ws) => ws.id !== workspaceId),
                    activeWorkspace: state.activeWorkspace?.id === workspaceId ? null : state.activeWorkspace,
                    activeWorkspaceId: state.activeWorkspaceId === workspaceId ? null : state.activeWorkspaceId,
                    isLoadingWorkspaces: false,
                }));
                console.info(`Workspace ${workspaceId} deleted.`);
            } catch (errInner) {
                console.error(`Failed to delete workspace ${workspaceId}:`, errInner);
                set({ error: errInner.message || `Failed to delete workspace ${workspaceId}`, isLoadingWorkspaces: false });
                // TODO: [UI Refactor] uiStore.ts was removed. Implement alternative for showing error to user.
                // uiActions.showModal('Error', `Failed to delete workspace: ${errInner.message}`);
            }

        } catch (err) {
            console.error(`Error initiating deletion for workspace ${workspaceId}:`, err);
            set({ error: err.message || `Failed to initiate deletion for workspace ${workspaceId}` });
        }
    },
    
    clearActiveWorkspace: () => {
        set({ activeWorkspace: null, activeWorkspaceId: null });
    },

    clearError: () => {
        set({ error: null });
    },
}));

// Example of how to react to activeWorkspaceId changing to load the workspace
// This might be too complex or cause loops if not handled carefully.
// Consider if loadActiveWorkspace should be called explicitly after setActiveWorkspace.
// For now, loadActiveWorkspace is called within setActiveWorkspace.

// Auto-load active workspace if ID is set but workspace data is missing (e.g., on app load)
// This could be part of an initialization routine in your app.
// For example, in your main App component's useEffect:
// useEffect(() => {
//   const { activeWorkspaceId, activeWorkspace, loadActiveWorkspace } = useWorkspaceStore.getState();
//   if (activeWorkspaceId && !activeWorkspace) {
//     loadActiveWorkspace(activeWorkspaceId);
//   }
// }, []);