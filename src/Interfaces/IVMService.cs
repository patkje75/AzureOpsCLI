using Azure.ResourceManager.Compute;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IVMService
    {
        Task<List<VirtualMachineResourceExtended>> FetchAllVMAsync();
        Task<List<VirtualMachineResourceExtended>> FetchAllDeallocatedVMAsync();
        Task<List<VirtualMachineResourceExtended>> FetchAllRunnigVMAsync();

        Task<List<VirtualMachineResourceExtended>> FetchVMInSubscriptionAsync(string subscriptionId);
        Task<List<VirtualMachineResourceExtended>> FetchDeallocatedVMInSubscriptionAsync(string subscriptionId);
        Task<List<VirtualMachineResourceExtended>> FetchRunningVMInSubscriptionAsync(string subscriptionId);
        Task<OperationResult> StartVMAsync(VirtualMachineResource vmResource);
        Task<OperationResult> StopVMAsync(VirtualMachineResource vmResource);
        Task<OperationResult> RestartVMAsync(VirtualMachineResource vmResource);
        Task<OperationResult> DeleteVMAsync(VirtualMachineResource vmResource);
    }

}