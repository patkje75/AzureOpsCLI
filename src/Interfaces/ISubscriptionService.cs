namespace AzureOpsCLI.Interfaces
{
    public interface ISubscritionService
    {
        Task<List<string>> FetchSubscriptionsAsync();

    }
}
