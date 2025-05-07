using LightBox.Core.Models;
using System.Threading.Tasks;

namespace LightBox.Core.Services.Interfaces
{
    public interface IApplicationSettingsService
    {
        Task<ApplicationSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(ApplicationSettings settings);
    }
}