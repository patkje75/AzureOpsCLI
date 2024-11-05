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
    public class VMSSVMService : IVMSSVMService
    {
        private ArmClient _armClient;

        public VMSSVMService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }



        public async Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchAllVMSSInstancesAsync()
        {
            List<VirtualMachineScaleSetVmResourceExtended> allVMSSInstances = new List<VirtualMachineScaleSetVmResourceExtended>();

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

                                foreach (var instance in instanceViews)
                                {
                                    var instanceView = await instance.GetInstanceViewAsync();
                                    var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11) ?? "Unknown";

                                    allVMSSInstances.Add(new VirtualMachineScaleSetVmResourceExtended
                                    {
                                        VMSS = vmss,
                                        VMSSVm = instance,
                                        SubscriptionName = subscription.Data.DisplayName,
                                        Status = powerState,
                                    });
                                }
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching virtual machine scale set instances: {ex.Message}[/]");
            }

            return allVMSSInstances;
        }

        public async Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchVMSSInstancesInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineScaleSetVmResourceExtended> allVMSSInstances = new List<VirtualMachineScaleSetVmResourceExtended>();

            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                    .StartAsync($"Fetching virtual machine scale set instances in subscription {subscription.Data.DisplayName}...", async ctx =>
                    {
                        var vmssList = subscription.GetVirtualMachineScaleSets();

                        foreach (var vmss in vmssList)
                        {
                            var instanceViews = vmss.GetVirtualMachineScaleSetVms();

                            foreach (var instance in instanceViews)
                            {
                                var instanceView = await instance.GetInstanceViewAsync();
                                var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11) ?? "Unknown";

                                allVMSSInstances.Add(new VirtualMachineScaleSetVmResourceExtended
                                {
                                    VMSS = vmss,
                                    VMSSVm = instance,
                                    SubscriptionName = subscription.Data.DisplayName,
                                    Status = powerState,
                                });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching VMSS instances for subscription {subscriptionId}: {ex.Message}[/]");
            }

            return allVMSSInstances;
        }


        public async Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchAllStoppedVMSSInstancesAsync()
        {
            List<VirtualMachineScaleSetVmResourceExtended> stoppedVMSSInstances = new List<VirtualMachineScaleSetVmResourceExtended>();

            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching stopped virtual machine scale set instances in {subscription.Data.DisplayName}...");
                            var vmssList = subscription.GetVirtualMachineScaleSets();

                            foreach (var vmss in vmssList)
                            {
                                var instanceViews = vmss.GetVirtualMachineScaleSetVms();

                                foreach (var instance in instanceViews)
                                {
                                    var instanceView = await instance.GetInstanceViewAsync();
                                    var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                                    bool anyRunning = false;


                                    if (powerState == "running")
                                    {
                                        anyRunning = true;

                                    }

                                    string vmssStatus = anyRunning ? "running" : "stopped";

                                    if (vmssStatus.Equals("stopped"))
                                        stoppedVMSSInstances.Add(new VirtualMachineScaleSetVmResourceExtended
                                        {
                                            VMSS = vmss,
                                            VMSSVm = instance,
                                            SubscriptionName = subscription.Data.DisplayName,
                                            Status = vmssStatus,

                                        });
                                }
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching virtual machine scale set instances: {ex.Message}[/]");
            }

            return stoppedVMSSInstances;
        }

        public async Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchStoppedVMSSInstancesInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineScaleSetVmResourceExtended> stoppedVMSSInstances = new List<VirtualMachineScaleSetVmResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);
                var vmssList = subscription.GetVirtualMachineScaleSets();

                await AnsiConsole.Status()
                    .StartAsync($"Fetching Stopped virtual machine scale set instances in subscription {subscription.Data.DisplayName}...", async ctx =>
                    {
                        foreach (var vmss in vmssList)
                        {
                            var instanceViews = vmss.GetVirtualMachineScaleSetVms();

                            foreach (var instance in instanceViews)
                            {
                                var instanceView = await instance.GetInstanceViewAsync();
                                var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                                bool anyRunning = false;


                                if (powerState == "running")
                                {
                                    anyRunning = true;

                                }

                                string vmssStatus = anyRunning ? "running" : "stopped";

                                if (vmssStatus.Equals("stopped"))
                                    stoppedVMSSInstances.Add(new VirtualMachineScaleSetVmResourceExtended
                                    {
                                        VMSS = vmss,
                                        VMSSVm = instance,
                                        SubscriptionName = subscription.Data.DisplayName,
                                        Status = vmssStatus,

                                    });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching or checking virtual machine scale sets in subscription {subscriptionId}: {ex.Message}[/]");
            }
            return stoppedVMSSInstances;
        }


        public async Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchAllRunningVMSSInstancesAsync()
        {
            List<VirtualMachineScaleSetVmResourceExtended> runningVMSSInstances = new List<VirtualMachineScaleSetVmResourceExtended>();

            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching running virtual machine scale set instances in {subscription.Data.DisplayName}...");
                            var vmssList = subscription.GetVirtualMachineScaleSets();

                            foreach (var vmss in vmssList)
                            {
                                var instanceViews = vmss.GetVirtualMachineScaleSetVms();

                                foreach (var instance in instanceViews)
                                {
                                    var instanceView = await instance.GetInstanceViewAsync();
                                    var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                                    bool anyRunning = false;


                                    if (powerState == "running")
                                    {
                                        anyRunning = true;

                                    }

                                    string vmssStatus = anyRunning ? "running" : "stopped";

                                    if (vmssStatus.Equals("running"))
                                        runningVMSSInstances.Add(new VirtualMachineScaleSetVmResourceExtended
                                        {
                                            VMSS = vmss,
                                            VMSSVm = instance,
                                            SubscriptionName = subscription.Data.DisplayName,
                                            Status = vmssStatus,

                                        });
                                }
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching virtual machine scale set instances: {ex.Message}[/]");
            }

            return runningVMSSInstances;
        }



        public async Task<List<VirtualMachineScaleSetVmResourceExtended>> FetchRunningVMSSInstancesInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineScaleSetVmResourceExtended> runningVMSSInstances = new List<VirtualMachineScaleSetVmResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);
                var vmssList = subscription.GetVirtualMachineScaleSets();

                await AnsiConsole.Status()
                    .StartAsync($"Fetching running virtual machine scale set instances in subscription {subscription.Data.DisplayName}...", async ctx =>
                    {
                        foreach (var vmss in vmssList)
                        {
                            var instanceViews = vmss.GetVirtualMachineScaleSetVms();

                            foreach (var instance in instanceViews)
                            {
                                var instanceView = await instance.GetInstanceViewAsync();
                                var powerState = instanceView.Value.Statuses.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11);
                                bool anyRunning = false;


                                if (powerState == "running")
                                {
                                    anyRunning = true;

                                }

                                string vmssStatus = anyRunning ? "running" : "stopped";

                                if (vmssStatus.Equals("running"))
                                    runningVMSSInstances.Add(new VirtualMachineScaleSetVmResourceExtended
                                    {
                                        VMSS = vmss,
                                        VMSSVm = instance,
                                        SubscriptionName = subscription.Data.DisplayName,
                                        Status = vmssStatus,

                                    });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching or checking virtual machine scale sets in subscription {subscriptionId}: {ex.Message}[/]");
            }
            return runningVMSSInstances;
        }


        public async Task<OperationResult> ReimageVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssvmResource)
        {
            if (vmssvmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set resource must not be null." };
            }

            try
            {
                await vmssvmResource.ReimageAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssvmResource.Data.Name}[/] reimaged." };
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

        public async Task<OperationResult> RestartVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssvmResource)
        {
            if (vmssvmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set resource must not be null." };
            }

            try
            {
                await vmssvmResource.RestartAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssvmResource.Data.Name}[/] restarted." };
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

        public async Task<OperationResult> StartVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssvmResource)
        {
            if (vmssvmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set resource must not be null." };
            }

            try
            {
                await vmssvmResource.PowerOnAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssvmResource.Data.Name}[/] started." };
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

        public async Task<OperationResult> StopVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssvmResource)
        {
            if (vmssvmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set instance must not be null." };
            }

            try
            {
                await vmssvmResource.PowerOffAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set [blue]{vmssvmResource.Data.Name}[/] stopped." };
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

        public async Task<OperationResult> UpgradeVMSSInstanceToLatestModelAsync(VirtualMachineScaleSetVmResource vmssVmResource, VirtualMachineScaleSetResource vmssResource)
        {
            if (vmssVmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine Scale Set instance must not be null." };
            }

            try
            {
                if (vmssVmResource.Data.LatestModelApplied == true)
                {
                    return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set instance [blue]{vmssVmResource.Data.Name}[/] is already using the latest model." };
                }

                var instanceIds = new List<string> { vmssVmResource.Data.InstanceId };
                var instanceRequiredIds = new VirtualMachineScaleSetVmInstanceRequiredIds(instanceIds);

                await vmssResource.UpdateInstancesAsync(WaitUntil.Completed, instanceRequiredIds);

                return new OperationResult { Success = true, Message = $"Virtual Machine Scale Set instance [blue]{vmssVmResource.Data.Name}[/] upgraded to the latest model." };
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
                    403 => "You do not have permission to upgrade this virtual machine scale set instance.",
                    404 => "The specified virtual machine scale set instance could not be found.",
                    _ => $"{errorMessage}"
                };

                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }



        public async Task<OperationResult> DeleteVMSSInstanceAsync(VirtualMachineScaleSetVmResource vmssResource)
        {
            throw new NotImplementedException();
        }
    }
}
