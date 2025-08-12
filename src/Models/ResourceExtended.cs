using Azure.ResourceManager.Resources;

namespace AzureOpsCLI.Models
{
    public class ResourceExtended
    {
        public GenericResource Resource { get; set; }
        public string SubscriptionName { get; set; }
        public Dictionary<string, string> Tags => Resource.Data.Tags?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();
        public string ResourceType => Resource.Data.ResourceType.ToString();
        public string ResourceName => Resource.Data.Name;
        public string Location => Resource.Data.Location.ToString() ?? "Unknown";
        public string ResourceGroupName => Resource.Data.Id.ResourceGroupName ?? "Unknown";
    }
}