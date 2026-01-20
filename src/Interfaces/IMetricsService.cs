using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IMetricsService
    {
        Task<List<VmMetrics>> FetchAllVMMetricsAsync();
        Task<List<VmMetrics>> FetchVMMetricsInSubscriptionAsync(string subscriptionId);
        Task<List<VmssMetrics>> FetchAllVMSSMetricsAsync();
        Task<List<VmssMetrics>> FetchVMSSMetricsInSubscriptionAsync(string subscriptionId);
    }

    public class VmMetrics
    {
        public string Name { get; set; } = string.Empty;
        public string SubscriptionName { get; set; } = string.Empty;
        public string ResourceGroup { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double? CpuPercentage { get; set; }
        public double? MemoryPercentage { get; set; }
        public double? DiskReadBytesPerSec { get; set; }
        public double? DiskWriteBytesPerSec { get; set; }
        public double? NetworkInBytesPerSec { get; set; }
        public double? NetworkOutBytesPerSec { get; set; }
    }

    public class VmssMetrics
    {
        public string Name { get; set; } = string.Empty;
        public string SubscriptionName { get; set; } = string.Empty;
        public string ResourceGroup { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int InstanceCount { get; set; }
        public double? AvgCpuPercentage { get; set; }
        public double? AvgMemoryPercentage { get; set; }
    }
}
