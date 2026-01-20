using Azure.ResourceManager.Resources;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface ILockService
    {
        Task<List<ResourceLockExtended>> FetchAllLocksAsync();
        Task<List<ResourceLockExtended>> FetchLocksInSubscriptionAsync(string subscriptionId);
        Task<List<LockableResource>> FetchLockableResourcesAsync();
        Task<List<LockableResource>> FetchLockableResourcesInSubscriptionAsync(string subscriptionId);
        Task<OperationResult> ApplyLockAsync(string resourceId, string lockName, string lockLevel);
        Task<OperationResult> RemoveLockAsync(ManagementLockResource lockResource);
    }

    public class ResourceLockExtended
    {
        public ManagementLockResource Lock { get; set; } = null!;
        public string SubscriptionName { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
    }

    public class LockableResource
    {
        public string ResourceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SubscriptionName { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public string ResourceGroup { get; set; } = string.Empty;
        public bool HasLock { get; set; }
        public string? ExistingLockLevel { get; set; }
    }
}
