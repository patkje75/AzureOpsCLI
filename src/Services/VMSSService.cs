using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Resources;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;

namespace AzureOpsCLI.Services
{
    public class VMSSService : IVMSSService
    {
        private ArmClient _armClient;

        public VMSSService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }


        public async Task<List<VirtualMachineScaleSetResourceExtended>> FetchAllVMSSAsync()
        {
            List<VirtualMachineScaleSetResourceExtended> vmssAllList = new List<VirtualMachineScaleSetResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching virtual machine scale sets in {subscription.Data.DisplayName}...");
                            var vmssList = subscription.GetVirtualMachineScaleSets();
                            foreach (var vmss in vmssList)
                            {
                                var instanceViews = vmss.GetVirtualMachineScaleSetVms();
                                bool anyRunning = false;
                                bool latestModel = false;

                                foreach (var instance in instanceViews)
                                {
                                    var instanceView = await instance.GetInstanceViewAsync();
                                    var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                                    var isLatestModelApplied = instanceView.Value.Statuses.Any(s => s.Code == "LatestModelApplied");

                                    if (powerState == "running")
                                    {
                                        anyRunning = true;

                                    }

                                    if (isLatestModelApplied)
                                    {
                                        latestModel = true;
                                    }
                                }

                                string vmssStatus = anyRunning ? "running" : "stopped";

                                vmssAllList.Add(new VirtualMachineScaleSetResourceExtended
                                {
                                    VMSS = vmss,
                                    SubscriptionName = subscription.Data.DisplayName,
                                    Status = vmssStatus.ToLower(),
                                    LatestModel = latestModel,
                                    numberOfInstances = instanceViews.Count()
                                });

                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching virtual machine scale sets: {ex.Message}[/]");
            }
            return vmssAllList;
        }

        public async Task<List<VirtualMachineScaleSetResourceExtended>> FetchAllStoppedVMSSAsync()
        {
            List<VirtualMachineScaleSetResourceExtended> deallocatedVMSSList = new List<VirtualMachineScaleSetResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching Stopped virtual machine scale sets in all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            var vmssList = subscription.GetVirtualMachineScaleSets();
                            foreach (var vm in vmssList)
                            {
                                ctx.Status($"Fetching virtual machine scale sets in {subscription.Data.DisplayName}...");

                                var instanceViews = vm.GetVirtualMachineScaleSetVms();
                                bool anyRunning = false;
                                bool latestModel = false;

                                foreach (var instance in instanceViews)
                                {
                                    var instanceView = await instance.GetInstanceViewAsync();
                                    var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                                    var isLatestModelApplied = instanceView.Value.Statuses.Any(s => s.Code == "LatestModelApplied");

                                    if (powerState == "running")
                                    {
                                        anyRunning = true;

                                    }

                                    if (isLatestModelApplied)
                                    {
                                        latestModel = true;
                                    }
                                }

                                string vmssStatus = anyRunning ? "running" : "stopped";

                                if (vmssStatus.Equals("stopped"))
                                    deallocatedVMSSList.Add(new VirtualMachineScaleSetResourceExtended
                                    {
                                        VMSS = vm,
                                        SubscriptionName = subscription.Data.DisplayName,
                                        Status = vmssStatus.ToLower(),
                                        LatestModel = latestModel,
                                        numberOfInstances = instanceViews.Count()
                                    });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching or checking virtual machine scale sets: {ex.Message}[/]");
            }
            return deallocatedVMSSList;
        }

        public async Task<List<VirtualMachineScaleSetResourceExtended>> FetchStoppedVMSSInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineScaleSetResourceExtended> deallocatedVMSSList = new List<VirtualMachineScaleSetResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);
                var vmssList = subscription.GetVirtualMachineScaleSets();

                await AnsiConsole.Status()
                    .StartAsync($"Fetching Stopped virtual machine scale sets in subscription {subscription.Data.DisplayName}...", async ctx =>
                    {
                        foreach (var vm in vmssList)
                        {
                            var instanceViews = vm.GetVirtualMachineScaleSetVms();
                            bool anyRunning = false;
                            bool latestModel = false;

                            foreach (var instance in instanceViews)
                            {
                                var instanceView = await instance.GetInstanceViewAsync();
                                var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                                var isLatestModelApplied = instanceView.Value.Statuses.Any(s => s.Code == "LatestModelApplied");


                                if (powerState == "running")
                                {
                                    anyRunning = true;

                                }

                                if (isLatestModelApplied)
                                {
                                    latestModel = true;
                                }
                            }

                            string vmssStatus = anyRunning ? "running" : "stopped";

                            if (vmssStatus.Equals("stopped"))
                                deallocatedVMSSList.Add(new VirtualMachineScaleSetResourceExtended
                                {
                                    VMSS = vm,
                                    SubscriptionName = subscription.Data.DisplayName,
                                    Status = vmssStatus.ToLower(),
                                    LatestModel = latestModel,
                                    numberOfInstances = instanceViews.Count()
                                });
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching or checking virtual machine scale sets in subscription {subscriptionId}: {ex.Message}[/]");
            }
            return deallocatedVMSSList;
        }

        public async Task<List<VirtualMachineScaleSetResourceExtended>> FetchVMSSInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineScaleSetResourceExtended> vmssAllList = new List<VirtualMachineScaleSetResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                .StartAsync($"Fetching virtual machine scale sets in subscription {subscription.Data.DisplayName}...", async ctx =>
                {
                    var vmssList = subscription.GetVirtualMachineScaleSets();
                    foreach (var vmss in vmssList)
                    {
                        var instanceViews = vmss.GetVirtualMachineScaleSetVms();
                        bool anyRunning = false;
                        bool latestModel = false;
                        foreach (var instance in instanceViews)
                        {
                            var instanceView = await instance.GetInstanceViewAsync();
                            var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                            var isLatestModelApplied = instanceView.Value.Statuses.Any(s => s.Code == "LatestModelApplied");

                            if (powerState == "running")
                            {
                                anyRunning = true;

                            }

                            if (isLatestModelApplied)
                            {
                                latestModel = true;
                            }
                        }

                        string vmssStatus = anyRunning ? "running" : "stopped";

                        vmssAllList.Add(new VirtualMachineScaleSetResourceExtended
                        {
                            VMSS = vmss,
                            SubscriptionName = subscription.Data.DisplayName,
                            Status = vmssStatus.ToLower(),
                            LatestModel = latestModel,
                            numberOfInstances = instanceViews.Count()
                        });

                    }
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching virtual machines scale sets for subscription {subscriptionId}: {ex.Message}[/]");

            }
            return vmssAllList;
        }

        public async Task<List<VirtualMachineScaleSetResourceExtended>> FetchAllRunningVMSSAsync()
        {
            List<VirtualMachineScaleSetResourceExtended> runningVMSSList = new List<VirtualMachineScaleSetResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching Running virtual machine scale sets in all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            var vmssList = subscription.GetVirtualMachineScaleSets();
                            foreach (var vm in vmssList)
                            {
                                ctx.Status($"Fetching virtual machine scale sets in {subscription.Data.DisplayName}...");

                                var instanceViews = vm.GetVirtualMachineScaleSetVms();
                                bool anyRunning = false;
                                bool latestModel = false;

                                foreach (var instance in instanceViews)
                                {
                                    var instanceView = await instance.GetInstanceViewAsync();
                                    var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                                    var isLatestModelApplied = instanceView.Value.Statuses.Any(s => s.Code == "LatestModelApplied");

                                    if (powerState == "running")
                                    {
                                        anyRunning = true;

                                    }

                                    if (isLatestModelApplied)
                                    {
                                        latestModel = true;
                                    }
                                }

                                string vmssStatus = anyRunning ? "running" : "stopped";


                                if (vmssStatus.Equals("running"))
                                    runningVMSSList.Add(new VirtualMachineScaleSetResourceExtended
                                    {
                                        VMSS = vm,
                                        SubscriptionName = subscription.Data.DisplayName,
                                        Status = vmssStatus.ToLower(),
                                        LatestModel = latestModel,
                                        numberOfInstances = instanceViews.Count()

                                    });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching or checking virtual machine scale sets: {ex.Message}[/]");
            }
            return runningVMSSList;
        }

        public async Task<List<VirtualMachineScaleSetResourceExtended>> FetchRunningVMSSInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineScaleSetResourceExtended> runningVMSSList = new List<VirtualMachineScaleSetResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                .StartAsync($"Fetching running virtual machine scale sets in subscription {subscription.Data.DisplayName}...", async ctx =>
                {
                    var vmssList = subscription.GetVirtualMachineScaleSets();
                    foreach (var vmss in vmssList)
                    {
                        var instanceViews = vmss.GetVirtualMachineScaleSetVms();
                        bool anyRunning = false;

                        bool latestModel = false;

                        foreach (var instance in instanceViews)
                        {
                            var instanceView = await instance.GetInstanceViewAsync();
                            var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                            var isLatestModelApplied = instanceView.Value.Statuses.Any(s => s.Code == "LatestModelApplied");

                            if (powerState == "running")
                            {
                                anyRunning = true;

                            }

                            if (isLatestModelApplied)
                            {
                                latestModel = true;
                            }
                        }

                        string vmssStatus = anyRunning ? "running" : "stopped";

                        if (vmssStatus.Equals("running"))
                            runningVMSSList.Add(new VirtualMachineScaleSetResourceExtended
                            {
                                VMSS = vmss,
                                SubscriptionName = subscription.Data.DisplayName,
                                Status = vmssStatus.ToLower(),
                                LatestModel = latestModel,
                                numberOfInstances = instanceViews.Count()
                            });

                    }
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching virtual machines scale sets for subscription {subscriptionId}: {ex.Message}[/]");

            }
            return runningVMSSList;
        }


        public async Task<OperationResult> StartVMSSAsync(VirtualMachineScaleSetResource vmssResource)
        {
            if (vmssResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set resource must not be null." };
            }

            try
            {
                await vmssResource.PowerOnAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssResource.Data.Name}[/] started." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to start this virtual machine scale set.",
                    404 => "The specified virtual machine scale set could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }


        public async Task<OperationResult> StopVMSSAsync(VirtualMachineScaleSetResource vmssResource)
        {
            if (vmssResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set resource must not be null." };
            }

            try
            {
                await vmssResource.PowerOffAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssResource.Data.Name}[/] stopped." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to stop this virtual machine scale set.",
                    404 => "The specified virtual machine scale set could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> RestartVMSSAsync(VirtualMachineScaleSetResource vmssResource)
        {
            if (vmssResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set resource must not be null." };
            }

            try
            {
                await vmssResource.RestartAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssResource.Data.Name}[/] restarted." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to restart this virtual machine scale set.",
                    404 => "The specified virtual machine scale set could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> ReimageVMSSAsync(VirtualMachineScaleSetResource vmssResource)
        {

            if (vmssResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set resource must not be null." };
            }

            try
            {
                await vmssResource.ReimageAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssResource.Data.Name}[/] reimaged." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to reimage this virtual machine scale set.",
                    404 => "The specified virtual machine scale set could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> UpdateVMSSImageAsync(VirtualMachineScaleSetResource vmssResource, GalleryImageResource image, string imageVersion)
        {
            try
            {

                GalleryImageVersionResource imageVersionResource = null;
                await foreach (var version in image.GetGalleryImageVersions().GetAllAsync())
                {
                    if (version.Data.Name == imageVersion)
                    {
                        imageVersionResource = version;
                        break;
                    }
                }


                if (imageVersionResource == null)
                {
                    return new OperationResult { Success = false, Message = $"Image version [yellow]'{imageVersion}'[/] not found for image [yellow]'{image.Data.Name}'[/]." };
                }


                var imageReference = new ImageReference
                {
                    Id = imageVersionResource.Data.Id
                };


                var patch = new VirtualMachineScaleSetPatch
                {
                    VirtualMachineProfile = new VirtualMachineScaleSetUpdateVmProfile
                    {
                        StorageProfile = new VirtualMachineScaleSetUpdateStorageProfile
                        {
                            ImageReference = imageReference
                        }
                    }
                };

                await vmssResource.UpdateAsync(WaitUntil.Completed, patch);

                return new OperationResult { Success = true, Message = $"[blue]{vmssResource.Data.Name}[/] was updated with image [yellow]{image.Data.Name}[/] version [yellow]{imageVersion}[/]" };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to update this virtual machine scale set.",
                    404 => "The specified virtual machine scale set could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> UpgradeVMSSToLatestModelAsync(VirtualMachineScaleSetResource vmssResource)
        {
            try
            {
                var instances = vmssResource.GetVirtualMachineScaleSetVms();
                var instanceIds = new List<string>();

                await foreach (var instance in instances.GetAllAsync())
                {
                    instanceIds.Add(instance.Data.InstanceId);
                }

                if (!instanceIds.Any())
                {
                    return new OperationResult { Success = false, Message = "No instances found in the Virtual Machine Scale Set." };
                }

                var instanceRequiredIds = new VirtualMachineScaleSetVmInstanceRequiredIds(instanceIds);

                await vmssResource.UpdateInstancesAsync(WaitUntil.Completed, instanceRequiredIds);

                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssResource.Data.Name}[/] updated to the latest model for all instances." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to update this virtual machine scale set.",
                    404 => "The specified virtual machine scale set could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> DeleteVMSSAsync(VirtualMachineScaleSetResource vmssResource)
        {

            if (vmssResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set resource must not be null." };
            }

            try
            {
                await vmssResource.DeleteAsync(WaitUntil.Completed, true, CancellationToken.None);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssResource.Data.Name}[/] deleted." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to delete this virtual machine scale set.",
                    404 => "The specified virtual machine scale set could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }



    }
}
