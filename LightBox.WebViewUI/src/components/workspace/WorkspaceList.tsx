// import { h } from 'preact'; // Removed unused import h
import { useWorkspaceStore } from '../../stores/workspaceStore';
import WorkspaceListItem from './WorkspaceListItem';
import './WorkspaceList.css'; // Create this file for styling

const WorkspaceList = () => {
    const { workspaces, activeWorkspaceId, isLoadingWorkspaces, error } = useWorkspaceStore(
        (state) => ({
            workspaces: state.workspaces,
            activeWorkspaceId: state.activeWorkspaceId,
            isLoadingWorkspaces: state.isLoadingWorkspaces,
            error: state.error,
        })
    );

    if (isLoadingWorkspaces) {
        return <div className="workspace-list-loading">Loading workspaces...</div>;
    }

    if (error && !workspaces.length) { // Show error prominently if list is empty due to it
        return <div className="workspace-list-error">Error loading workspaces: {error}</div>;
    }

    if (!workspaces.length) {
        return <div className="workspace-list-empty">No workspaces found. Add one to get started!</div>;
    }

    return (
        <div className="workspace-list">
            {workspaces.map((ws) => (
                <WorkspaceListItem
                    key={ws.id}
                    workspace={ws}
                    isActive={ws.id === activeWorkspaceId}
                />
            ))}
        </div>
    );
};

export default WorkspaceList;