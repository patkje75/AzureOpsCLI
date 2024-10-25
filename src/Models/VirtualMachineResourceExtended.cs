using Azure.ResourceManager.Compute;

namespace AzureOpsCLI.Models
{
    public class VirtualMachineResourceExtended
    {
        public VirtualMachineResource VM { get; set; }
        public string SubscriptionName { get; set; }

        public string Status { get; set; }
    }
}
