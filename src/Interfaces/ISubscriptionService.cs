using Azure.ResourceManager.Resources;

namespace AzureOpsCLI.Interfaces
{
    public interface ISubscritionService
    {
        Task<List<string>> FetchSubscriptionsAsync();
        Task<List<SubscriptionResource>> FetchAllSubscriptionsAsync();
    }
}
