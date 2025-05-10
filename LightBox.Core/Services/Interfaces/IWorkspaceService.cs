using LightBox.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightBox.Core.Services.Interfaces
{
    public interface IWorkspaceService
    {
        Task<WorkspaceInfo> CreateWorkspaceAsync(string name, string icon);
        Task<List<WorkspaceInfo>> GetWorkspacesAsync();
        Task<Workspace> GetWorkspaceByIdAsync(string workspaceId);
        Task SaveWorkspaceAsync(Workspace workspace);
        Task DeleteWorkspaceAsync(string workspaceId);
        Task SetActiveWorkspaceIdAsync(string workspaceId);
        Task<string> GetActiveWorkspaceIdAsync();
        Task<Workspace> GetActiveWorkspaceAsync();
    }
}