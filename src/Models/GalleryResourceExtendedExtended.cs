using Azure.ResourceManager.Compute;

namespace AzureOpsCLI.Models
{
    public class GalleryResourceExtended
    {
        public GalleryResource gallery { get; set; }
        public string SubscriptionName { get; set; }

    }
}
