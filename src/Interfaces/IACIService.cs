using Azure.ResourceManager.ContainerInstance;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IACIService
    {
        Task<List<ContainerGroupResourceExtended>> FetchAllACIAsync();
        Task<List<ContainerGroupResourceExtended>> FetchRunningACIAsync();
        Task<List<ContainerGroupResourceExtended>> FetchStoppedACIAsync();
        Task<List<ContainerGroupResourceExtended>> FetchACIInSubscriptionAsync(string subscriptionId);
        Task<List<ContainerGroupResourceExtended>> FetchRunningACIInSubscriptionAsync(string subscriptionId);
        Task<List<ContainerGroupResourceExtended>> FetchStoppedACIInSubscriptionAsync(string subscriptionId);

        Task<OperationResult> StartACIAsync(ContainerGroupResource aciResource);
        Task<OperationResult> StopACIAsync(ContainerGroupResource aciResource);
        Task<OperationResult> RestartACIAsync(ContainerGroupResource aciResource);
        Task<OperationResult> DeleteACIAsync(ContainerGroupResource aciResource);
        Task<OperationResult> ExecuteContainerCommandAsync(ContainerGroupResource aciResource, string command);
        Task<OperationResult> GetContainerLogsAsync(ContainerGroupResource aciResource);

    }
}
