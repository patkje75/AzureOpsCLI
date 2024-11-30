using Azure.ResourceManager.Storage;

namespace AzureOpsCLI.Models
{
    public class StorageAccountResourceExtended
    {
        public StorageAccountResource StorageAccountResource { get; set; }
        public string SubscriptionName { get; set; }
    }
}
