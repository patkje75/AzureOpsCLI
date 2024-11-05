using Azure.ResourceManager.Compute;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IVMSSService
    {
        Task<List<VirtualMachineScaleSetResourceExtended>> FetchAllVMSSAsync();
        Task<List<VirtualMachineScaleSetResourceExtended>> FetchAllStoppedVMSSAsync();
        Task<List<VirtualMachineScaleSetResourceExtended>> FetchAllRunningVMSSAsync();
        Task<List<VirtualMachineScaleSetResourceExtended>> FetchVMSSInSubscriptionAsync(string subscriptionId);
        Task<List<VirtualMachineScaleSetResourceExtended>> FetchStoppedVMSSInSubscriptionAsync(string subscriptionId);
        Task<List<VirtualMachineScaleSetResourceExtended>> FetchRunningVMSSInSubscriptionAsync(string subscriptionId);
        Task<OperationResult> StartVMSSAsync(VirtualMachineScaleSetResource vmssResource);
        Task<OperationResult> StopVMSSAsync(VirtualMachineScaleSetResource vmssResource);
        Task<OperationResult> RestartVMSSAsync(VirtualMachineScaleSetResource vmssResource);
        Task<OperationResult> ReimageVMSSAsync(VirtualMachineScaleSetResource vmssResource);
        Task<OperationResult> UpdateVMSSImageAsync(VirtualMachineScaleSetResource vmss, GalleryImageResource image, string imageVersion);
        Task<OperationResult> UpgradeVMSSToLatestModelAsync(VirtualMachineScaleSetResource vmssResource);
        Task<OperationResult> DeleteVMSSAsync(VirtualMachineScaleSetResource vmssResource);

    }
}
