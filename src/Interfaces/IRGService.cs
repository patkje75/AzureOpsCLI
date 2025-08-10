using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IRGService
    {
        Task<List<ResourceGroupExtended>> FetchAllResourceGroupsAsync(string? filter = null);
        Task<List<ResourceGroupExtended>> FetchResourceGroupsBySubscriptionAsync(string subscriptionId, string? filter = null);
        Task<bool> CreateResourceGroupAsync(string subscriptionId, string resourceGroupName, string location);
        Task<ResourceGroupExtended?> GetResourceGroupAsync(string subscriptionId, string resourceGroupName);
        Task<bool> DeleteResourceGroupAsync(string subscriptionId, string resourceGroupName);
    }
}
