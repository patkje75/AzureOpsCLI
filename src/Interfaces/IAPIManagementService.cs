using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.Storage;
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
