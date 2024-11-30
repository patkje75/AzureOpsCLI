using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IMGService
    {
        Task<List<ManagementGroupExtended>> FetchManagementGroupsAsync();
    }
}
