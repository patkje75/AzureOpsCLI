using Azure.ResourceManager.Compute;

namespace AzureOpsCLI.Models
{
    public class VirtualMachineScaleSetVmResourceExtended
    {
        public VirtualMachineScaleSetVmResource VMSSVm { get; set; }
        public VirtualMachineScaleSetResource VMSS { get; set; }
        public string SubscriptionName { get; set; }
        public string Status { get; set; }

    }
}
