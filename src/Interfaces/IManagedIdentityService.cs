using Azure.ResourceManager.ManagedServiceIdentities;

namespace AzureOpsCLI.Interfaces
{
    internal interface IManagedIdentityService<T>
    {
        Task<List<SystemAssignedIdentityResource>> GetManagedIdentitiesAsync(T resource);
    }
}
