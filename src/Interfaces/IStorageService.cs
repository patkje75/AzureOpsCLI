using Azure.ResourceManager.Storage;
using AzureOpsCLI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Interfaces
{
    public interface IStorageService
    {
        Task<List<StorageAccountResourceExtended>> FetchAllStorageAccountsAsync();
        Task<List<StorageAccountResourceExtended>> FetchStorageAccountsInSubscriptionAsync(string subscriptionId);
    }
}
