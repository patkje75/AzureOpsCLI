using Azure.ResourceManager.Compute;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IVMSSVMService
    {
        Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchAllVMSSInstancesAsync();
        Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchAllStoppedVMSSInstancesAsync();
        Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchAllRunningVMSSInstancesAsync();
        Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchVMSSInstancesInSubscriptionAsync(string subscriptionId);
        Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchStoppedVMSSInstancesInSubscriptionAsync(string subscriptionId);
        Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchRunningVMSSInstancesInSubscriptionAsync(string subscriptionId);
        Task<OperationResult> StartVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssVmResource);
        Task<OperationResult> StopVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssVmResource);
        Task<OperationResult> RestartVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssVmResource);
        Task<OperationResult> ReimageVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssVmResource);
        Task<OperationResult> UpgradeVMSSInstanceToLatestModelAsync(VirtualMachineScaleSetVmResource vmssVmResource, VirtualMachineScaleSetResource vmssResource);
        Task<OperationResult> DeleteVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssVmResource);

    }
}
