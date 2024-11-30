using Azure.ResourceManager.ApiManagement;

namespace AzureOpsCLI.Models
{
    public class ApiManagementServiceResourceExtended
    {
        public ApiManagementServiceResource APIManagementService { get; set; }
        public string SubscriptionName { get; set; }
    }
}
