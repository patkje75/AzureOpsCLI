using Azure.ResourceManager.Resources;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface ITagService
    {
        Task<List<ResourceExtended>> FetchAllResourcesWithTagsAsync();
        Task<List<ResourceExtended>> FetchResourcesWithTagsInSubscriptionAsync(string subscriptionId);
        Task<OperationResult> ApplyTagsAsync(GenericResource resource, Dictionary<string, string> tags);
        Task<OperationResult> RemoveTagsAsync(GenericResource resource, List<string> tagKeys);
        Task<string> ExportTagsToJsonAsync(List<ResourceExtended> resources);
        Task<string> ExportTagsToCsvAsync(List<ResourceExtended> resources);
    }
}