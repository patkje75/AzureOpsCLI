using Azure.ResourceManager.ContainerInstance;


namespace AzureOpsCLI.Models
{
    public class ContainerGroupResourceExtended
    {
        public ContainerGroupResource ContainerGroup { get; set; }
        public string Status { get; set; }
        public string SubscriptionName { get; set; }
    }
}
