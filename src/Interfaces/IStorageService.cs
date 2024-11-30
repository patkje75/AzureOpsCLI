using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IStorageService
    {
        Task<List<StorageAccountResourceExtended>> FetchAllStorageAccountsAsync();
        Task<List<StorageAccountResourceExtended>> FetchStorageAccountsInSubscriptionAsync(string subscriptionId);
    }
}
