using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Services
{
    public class LockService : ILockService
    {
        private readonly ArmClient _armClient;

        public LockService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<ResourceLockExtended>> FetchAllLocksAsync()
        {
            var locks = new List<ResourceLockExtended>();

            await foreach (var subscription in _armClient.GetSubscriptions())
            {
                await foreach (var rg in subscription.GetResourceGroups())
                {
                    await foreach (var lockResource in rg.GetManagementLocks())
                    {
                        locks.Add(new ResourceLockExtended
                        {
                            Lock = lockResource,
                            SubscriptionName = subscription.Data.DisplayName,
                            ResourceName = rg.Data.Name,
                            ResourceType = "Resource Group"
                        });
                    }
                }
            }

            return locks;
        }

        public async Task<List<ResourceLockExtended>> FetchLocksInSubscriptionAsync(string subscriptionId)
        {
            var locks = new List<ResourceLockExtended>();
            var subscription = _armClient.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

            await foreach (var rg in subscription.GetResourceGroups())
            {
                await foreach (var lockResource in rg.GetManagementLocks())
                {
                    locks.Add(new ResourceLockExtended
                    {
                        Lock = lockResource,
                        SubscriptionName = subscription.Data.DisplayName,
                        ResourceName = rg.Data.Name,
                        ResourceType = "Resource Group"
                    });
                }
            }

            return locks;
        }

        public async Task<List<LockableResource>> FetchLockableResourcesAsync()
        {
            var resources = new List<LockableResource>();

            await foreach (var subscription in _armClient.GetSubscriptions())
            {
                await foreach (var rg in subscription.GetResourceGroups())
                {
                    var hasLock = false;
                    string? lockLevel = null;

                    await foreach (var lockResource in rg.GetManagementLocks())
                    {
                        hasLock = true;
                        lockLevel = lockResource.Data.Level.ToString();
                        break;
                    }

                    resources.Add(new LockableResource
                    {
                        ResourceId = rg.Id.ToString(),
                        Name = rg.Data.Name,
                        Type = "Resource Group",
                        SubscriptionName = subscription.Data.DisplayName,
                        SubscriptionId = subscription.Data.SubscriptionId,
                        ResourceGroup = rg.Data.Name,
                        HasLock = hasLock,
                        ExistingLockLevel = lockLevel
                    });
                }
            }

            return resources;
        }

        public async Task<List<LockableResource>> FetchLockableResourcesInSubscriptionAsync(string subscriptionId)
        {
            var resources = new List<LockableResource>();
            var subscription = _armClient.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

            await foreach (var rg in subscription.GetResourceGroups())
            {
                var hasLock = false;
                string? lockLevel = null;

                await foreach (var lockResource in rg.GetManagementLocks())
                {
                    hasLock = true;
                    lockLevel = lockResource.Data.Level.ToString();
                    break;
                }

                resources.Add(new LockableResource
                {
                    ResourceId = rg.Id.ToString(),
                    Name = rg.Data.Name,
                    Type = "Resource Group",
                    SubscriptionName = subscription.Data.DisplayName,
                    SubscriptionId = subscription.Data.SubscriptionId,
                    ResourceGroup = rg.Data.Name,
                    HasLock = hasLock,
                    ExistingLockLevel = lockLevel
                });
            }

            return resources;
        }

        public async Task<OperationResult> ApplyLockAsync(string resourceId, string lockName, string lockLevel)
        {
            try
            {
                var resourceIdentifier = new Azure.Core.ResourceIdentifier(resourceId);

                ManagementLockLevel level = lockLevel.ToLower() switch
                {
                    "readonly" => ManagementLockLevel.ReadOnly,
                    "cannotdelete" => ManagementLockLevel.CanNotDelete,
                    _ => ManagementLockLevel.CanNotDelete
                };

                var lockData = new ManagementLockData(level)
                {
                    Notes = $"Lock created by AzureOpsCLI on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
                };

                var rg = _armClient.GetResourceGroupResource(resourceIdentifier);
                var lockCollection = rg.GetManagementLocks();
                await lockCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, lockName, lockData);

                return new OperationResult
                {
                    Success = true,
                    Message = $"Lock '{lockName}' ({lockLevel}) applied successfully"
                };
            }
            catch (Exception ex)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Failed to apply lock: {ex.Message}"
                };
            }
        }

        public async Task<OperationResult> RemoveLockAsync(ManagementLockResource lockResource)
        {
            try
            {
                await lockResource.DeleteAsync(Azure.WaitUntil.Completed);
                return new OperationResult
                {
                    Success = true,
                    Message = $"Lock '{lockResource.Data.Name}' removed successfully"
                };
            }
            catch (Exception ex)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Failed to remove lock: {ex.Message}"
                };
            }
        }
    }
}
