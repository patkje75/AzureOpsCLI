using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IAPIManagementService
    {
        Task<List<ApiManagementServiceResourceExtended>> FetchAllAPIMAsync();
        Task<List<ApiManagementServiceResourceExtended>> FetchAPIMInSubscriptionsAsync(string subscriptionId);
        Task<OperationResult> BackupAPIMAsync(ApiManagementServiceResourceExtended apim, StorageAccountResourceExtended storageAccount, ManagedIdentity identity, string containerName);
    }
}
