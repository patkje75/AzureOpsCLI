using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Services
{
    public class DiskService : IDiskService
    {
        private readonly ArmClient _armClient;

        public DiskService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<ManagedDiskExtended>> FetchAllDisksAsync()
        {
            var disks = new List<ManagedDiskExtended>();

            await foreach (var subscription in _armClient.GetSubscriptions())
            {
                var diskCollection = subscription.GetManagedDisksAsync();
                await foreach (var disk in diskCollection)
                {
                    disks.Add(new ManagedDiskExtended
                    {
                        Disk = disk,
                        SubscriptionName = subscription.Data.DisplayName,
                        SubscriptionId = subscription.Data.SubscriptionId
                    });
                }
            }

            return disks;
        }

        public async Task<List<ManagedDiskExtended>> FetchDisksInSubscriptionAsync(string subscriptionId)
        {
            var disks = new List<ManagedDiskExtended>();
            var subscription = _armClient.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

            var diskCollection = subscription.GetManagedDisksAsync();
            await foreach (var disk in diskCollection)
            {
                disks.Add(new ManagedDiskExtended
                {
                    Disk = disk,
                    SubscriptionName = subscription.Data.DisplayName,
                    SubscriptionId = subscription.Data.SubscriptionId
                });
            }

            return disks;
        }

        public async Task<List<ManagedDiskExtended>> FetchUnattachedDisksAsync()
        {
            var disks = await FetchAllDisksAsync();
            return disks.Where(d => !d.IsAttached).ToList();
        }

        public async Task<List<ManagedDiskExtended>> FetchUnattachedDisksInSubscriptionAsync(string subscriptionId)
        {
            var disks = await FetchDisksInSubscriptionAsync(subscriptionId);
            return disks.Where(d => !d.IsAttached).ToList();
        }

        public async Task<OperationResult> CreateSnapshotAsync(ManagedDiskResource disk, string snapshotName)
        {
            try
            {
                var subscription = _armClient.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{disk.Id.SubscriptionId}"));
                var resourceGroupResponse = await subscription.GetResourceGroupAsync(disk.Id.ResourceGroupName);
                var snapshotCollection = resourceGroupResponse.Value.GetSnapshots();

                var snapshotData = new SnapshotData(disk.Data.Location)
                {
                    CreationData = new DiskCreationData(DiskCreateOption.Copy)
                    {
                        SourceResourceId = disk.Id
                    },
                    Sku = new SnapshotSku { Name = SnapshotStorageAccountType.StandardLrs }
                };

                var operation = await snapshotCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, snapshotName, snapshotData);

                return new OperationResult
                {
                    Success = operation.HasCompleted,
                    Message = $"Snapshot '{snapshotName}' created for disk '{disk.Data.Name}'"
                };
            }
            catch (Exception ex)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Failed to create snapshot: {ex.Message}"
                };
            }
        }

        public async Task<OperationResult> DeleteDiskAsync(ManagedDiskResource disk)
        {
            try
            {
                if (disk.Data.ManagedBy != null)
                {
                    return new OperationResult
                    {
                        Success = false,
                        Message = $"Disk '{disk.Data.Name}' is attached to a VM and cannot be deleted"
                    };
                }

                var operation = await disk.DeleteAsync(Azure.WaitUntil.Completed);

                return new OperationResult
                {
                    Success = true,
                    Message = $"Disk '{disk.Data.Name}' deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Failed to delete disk: {ex.Message}"
                };
            }
        }
    }
}
