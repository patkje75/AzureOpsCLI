using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using AzureOpsCLI.Interfaces;

namespace AzureOpsCLI.Services
{
    public class MetricsService : IMetricsService
    {
        private readonly ArmClient _armClient;
        private readonly MetricsQueryClient _metricsClient;

        public MetricsService()
        {
            var credential = new DefaultAzureCredential();
            _armClient = new ArmClient(credential);
            _metricsClient = new MetricsQueryClient(credential);
        }

        public async Task<List<VmMetrics>> FetchAllVMMetricsAsync()
        {
            var metrics = new List<VmMetrics>();

            await foreach (var subscription in _armClient.GetSubscriptions())
            {
                var vms = subscription.GetVirtualMachinesAsync();
                await foreach (var vm in vms)
                {
                    var vmMetrics = await GetVmMetricsAsync(vm, subscription.Data.DisplayName);
                    if (vmMetrics != null)
                    {
                        metrics.Add(vmMetrics);
                    }
                }
            }

            return metrics;
        }

        public async Task<List<VmMetrics>> FetchVMMetricsInSubscriptionAsync(string subscriptionId)
        {
            var metrics = new List<VmMetrics>();
            var subscription = _armClient.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

            var vms = subscription.GetVirtualMachinesAsync();
            await foreach (var vm in vms)
            {
                var vmMetrics = await GetVmMetricsAsync(vm, subscription.Data.DisplayName);
                if (vmMetrics != null)
                {
                    metrics.Add(vmMetrics);
                }
            }

            return metrics;
        }

        public async Task<List<VmssMetrics>> FetchAllVMSSMetricsAsync()
        {
            var metrics = new List<VmssMetrics>();

            await foreach (var subscription in _armClient.GetSubscriptions())
            {
                var vmssCollection = subscription.GetVirtualMachineScaleSetsAsync();
                await foreach (var vmss in vmssCollection)
                {
                    var vmssMetrics = await GetVmssMetricsAsync(vmss, subscription.Data.DisplayName);
                    if (vmssMetrics != null)
                    {
                        metrics.Add(vmssMetrics);
                    }
                }
            }

            return metrics;
        }

        public async Task<List<VmssMetrics>> FetchVMSSMetricsInSubscriptionAsync(string subscriptionId)
        {
            var metrics = new List<VmssMetrics>();
            var subscription = _armClient.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

            var vmssCollection = subscription.GetVirtualMachineScaleSetsAsync();
            await foreach (var vmss in vmssCollection)
            {
                var vmssMetrics = await GetVmssMetricsAsync(vmss, subscription.Data.DisplayName);
                if (vmssMetrics != null)
                {
                    metrics.Add(vmssMetrics);
                }
            }

            return metrics;
        }

        private async Task<VmMetrics?> GetVmMetricsAsync(VirtualMachineResource vm, string subscriptionName)
        {
            try
            {
                var resourceId = vm.Id.ToString();
                var timeSpan = new QueryTimeRange(TimeSpan.FromHours(1));

                var response = await _metricsClient.QueryResourceAsync(
                    resourceId,
                    new[] { "Percentage CPU", "Available Memory Bytes", "Disk Read Bytes", "Disk Write Bytes", "Network In Total", "Network Out Total" },
                    new MetricsQueryOptions
                    {
                        Granularity = TimeSpan.FromMinutes(5),
                        TimeRange = timeSpan
                    });

                var result = new VmMetrics
                {
                    Name = vm.Data.Name,
                    SubscriptionName = subscriptionName,
                    ResourceGroup = vm.Id.ResourceGroupName ?? string.Empty,
                    Location = vm.Data.Location.ToString()
                };

                foreach (var metric in response.Value.Metrics)
                {
                    var latestValue = metric.TimeSeries
                        .SelectMany(ts => ts.Values)
                        .Where(v => v.Average.HasValue)
                        .OrderByDescending(v => v.TimeStamp)
                        .FirstOrDefault()?.Average;

                    switch (metric.Name)
                    {
                        case "Percentage CPU":
                            result.CpuPercentage = latestValue;
                            break;
                        case "Available Memory Bytes":
                            result.MemoryPercentage = latestValue;
                            break;
                        case "Disk Read Bytes":
                            result.DiskReadBytesPerSec = latestValue;
                            break;
                        case "Disk Write Bytes":
                            result.DiskWriteBytesPerSec = latestValue;
                            break;
                        case "Network In Total":
                            result.NetworkInBytesPerSec = latestValue;
                            break;
                        case "Network Out Total":
                            result.NetworkOutBytesPerSec = latestValue;
                            break;
                    }
                }

                return result;
            }
            catch
            {
                return new VmMetrics
                {
                    Name = vm.Data.Name,
                    SubscriptionName = subscriptionName,
                    ResourceGroup = vm.Id.ResourceGroupName ?? string.Empty,
                    Location = vm.Data.Location.ToString()
                };
            }
        }

        private async Task<VmssMetrics?> GetVmssMetricsAsync(VirtualMachineScaleSetResource vmss, string subscriptionName)
        {
            try
            {
                var resourceId = vmss.Id.ToString();
                var timeSpan = new QueryTimeRange(TimeSpan.FromHours(1));

                var response = await _metricsClient.QueryResourceAsync(
                    resourceId,
                    new[] { "Percentage CPU", "Available Memory Bytes" },
                    new MetricsQueryOptions
                    {
                        Granularity = TimeSpan.FromMinutes(5),
                        TimeRange = timeSpan
                    });

                var result = new VmssMetrics
                {
                    Name = vmss.Data.Name,
                    SubscriptionName = subscriptionName,
                    ResourceGroup = vmss.Id.ResourceGroupName ?? string.Empty,
                    Location = vmss.Data.Location.ToString(),
                    InstanceCount = vmss.Data.Sku?.Capacity != null ? (int)vmss.Data.Sku.Capacity : 0
                };

                foreach (var metric in response.Value.Metrics)
                {
                    var latestValue = metric.TimeSeries
                        .SelectMany(ts => ts.Values)
                        .Where(v => v.Average.HasValue)
                        .OrderByDescending(v => v.TimeStamp)
                        .FirstOrDefault()?.Average;

                    switch (metric.Name)
                    {
                        case "Percentage CPU":
                            result.AvgCpuPercentage = latestValue;
                            break;
                        case "Available Memory Bytes":
                            result.AvgMemoryPercentage = latestValue;
                            break;
                    }
                }

                return result;
            }
            catch
            {
                return new VmssMetrics
                {
                    Name = vmss.Data.Name,
                    SubscriptionName = subscriptionName,
                    ResourceGroup = vmss.Id.ResourceGroupName ?? string.Empty,
                    Location = vmss.Data.Location.ToString(),
                    InstanceCount = vmss.Data.Sku?.Capacity != null ? (int)vmss.Data.Sku.Capacity : 0
                };
            }
        }
    }
}
