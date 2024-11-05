using Azure.ResourceManager.Compute;

namespace AzureOpsCLI.Models
{
    public class VirtualMachineScaleSetResourceExtended
    {
        public VirtualMachineScaleSetResource VMSS { get; set; }
        public string SubscriptionName { get; set; }
        public string Status { get; set; }
        public bool LatestModel { get; set; }
        public int numberOfInstances { get; set; }
    }
}
