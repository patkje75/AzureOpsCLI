using Azure.ResourceManager.ManagementGroups.Models;

namespace AzureOpsCLI.Models
{
    public class ManagementGroupExtended
    {
        public string DisplayName { get; set; }
        public string Parent { get; set; }
        public List<ManagementGroupChildInfo> Children { get; set; }
    }
}
