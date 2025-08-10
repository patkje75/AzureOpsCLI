using Azure.ResourceManager.Resources;

namespace AzureOpsCLI.Models
{
    public class ResourceGroupExtended
    {
        public ResourceGroupResource ResourceGroup { get; set; }
        public string SubscriptionName { get; set; }
    }
}
