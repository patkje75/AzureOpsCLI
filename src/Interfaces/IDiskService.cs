using AzureOpsCLI.Models;
using Azure.ResourceManager.Compute;

namespace AzureOpsCLI.Interfaces
{
    public interface IDiskService
    {
        Task<List<ManagedDiskExtended>> FetchAllDisksAsync();
        Task<List<ManagedDiskExtended>> FetchDisksInSubscriptionAsync(string subscriptionId);
        Task<List<ManagedDiskExtended>> FetchUnattachedDisksAsync();
        Task<List<ManagedDiskExtended>> FetchUnattachedDisksInSubscriptionAsync(string subscriptionId);
        Task<OperationResult> CreateSnapshotAsync(ManagedDiskResource disk, string snapshotName);
        Task<OperationResult> DeleteDiskAsync(ManagedDiskResource disk);
    }

    public class ManagedDiskExtended
    {
        public ManagedDiskResource Disk { get; set; } = null!;
        public string SubscriptionName { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public bool IsAttached => Disk.Data.ManagedBy != null;
        public string AttachedTo => GetAttachedVmName();

        private string GetAttachedVmName()
        {
            if (Disk.Data.ManagedBy == null)
                return "Unattached";

            var managedByString = Disk.Data.ManagedBy.ToString();
            var parts = managedByString.Split('/');
            return parts.Length > 0 ? parts[^1] : "Unknown";
        }
    }
}
