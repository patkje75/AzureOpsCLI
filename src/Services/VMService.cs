using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;

namespace AzureOpsCLI.Services
{
    public class VMService : IVMService
    {
        private ArmClient _armClient;

        public VMService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<VirtualMachineResourceExtended>> FetchAllVMAsync()
        {
            List<VirtualMachineResourceExtended> vmList = new List<VirtualMachineResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {

                            ctx.Status($"Fetching Virtual Machines in {subscription.Data.DisplayName}...");

                            var vms = subscription.GetVirtualMachines();
                            foreach (var vm in vms)
                            {
                                var instanceView = await vm.InstanceViewAsync();
                                string powerState = instanceView.Value.Statuses?
                                    .FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11) ?? "Unknown";

                                vmList.Add(new VirtualMachineResourceExtended
                                {
                                    VM = vm,
                                    Status = powerState,
                                    SubscriptionName = subscription.Data.DisplayName
                                });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching virtual machines: {ex.Message}[/]");
            }
            return vmList;
        }

        public async Task<List<VirtualMachineResourceExtended>> FetchAllDeallocatedVMAsync()
        {
            List<VirtualMachineResourceExtended> deallocatedVMList = new List<VirtualMachineResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching deallocated Virtual Machines in {subscription.Data.DisplayName}...");

                            var vms = subscription.GetVirtualMachines();
                            foreach (var vm in vms)
                            {
                                var instanceView = await vm.InstanceViewAsync();
                                var isDeallocated = instanceView.Value.Statuses.Any(s => s.Code == "PowerState/deallocated");

                                if (isDeallocated)
                                {
                                    deallocatedVMList.Add(new VirtualMachineResourceExtended
                                    {
                                        VM = vm,
                                        SubscriptionName = subscription.Data.DisplayName
                                    });
                                }
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching or checking VMs: {ex.Message}[/]");
            }
            return deallocatedVMList;
        }


        public async Task<List<VirtualMachineResourceExtended>> FetchVMInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineResourceExtended> vmList = new List<VirtualMachineResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                .StartAsync($"Fetching all virtual machines in subscription {subscription.Data.DisplayName}...", async ctx =>
                {
                    var vms = subscription.GetVirtualMachines();
                    foreach (var vm in vms)
                    {
                        try
                        {
                            var instanceView = await vm.InstanceViewAsync();
                            string powerState = instanceView.Value.Statuses?
                                .FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11) ?? "Unknown";

                            vmList.Add(new VirtualMachineResourceExtended
                            {
                                VM = vm,
                                Status = powerState,
                                SubscriptionName = subscription.Data.DisplayName
                            });
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Error fetching instance view for Virtual Machine [blue]{vm.Data.Name}[/] in subscription [yellow]{subscription.Data.DisplayName}[/]: {ex.Message}[/]");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching virtual machines for subscription {subscriptionId}: {ex.Message}[/]");
            }
            return vmList;
        }

        public async Task<List<VirtualMachineResourceExtended>> FetchDeallocatedVMInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineResourceExtended> deallocatedVMList = new List<VirtualMachineResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);
                var vms = subscription.GetVirtualMachines();
                await AnsiConsole.Status()
                    .StartAsync($"Fetching deallocated virtual machines in subscription {subscription.Data.DisplayName}...", async ctx =>
                    {
                        foreach (var vm in vms)
                        {
                            var instanceView = await vm.InstanceViewAsync();
                            var isDeallocated = instanceView.Value.Statuses.Any(s => s.Code == "PowerState/deallocated");
                            if (isDeallocated)
                            {
                                deallocatedVMList.Add(new VirtualMachineResourceExtended
                                {
                                    VM = vm,
                                    Status = "deallocated",
                                    SubscriptionName = subscription.Data.DisplayName
                                });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching or checking VMs in subscription {subscriptionId}: {ex.Message}[/]");
            }
            return deallocatedVMList;
        }

        public async Task<List<VirtualMachineResourceExtended>> FetchAllRunnigVMAsync()
        {
            List<VirtualMachineResourceExtended> runningVMList = new List<VirtualMachineResourceExtended>();

            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching running virtual machines in all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching running VMs in {subscription.Data.DisplayName}...");

                            var vms = subscription.GetVirtualMachines();
                            foreach (var vm in vms)
                            {
                                var instanceView = await vm.InstanceViewAsync();
                                string powerState = instanceView.Value.Statuses?
                                    .FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11) ?? "Unknown";

                                if (powerState == "running")
                                {
                                    runningVMList.Add(new VirtualMachineResourceExtended
                                    {
                                        VM = vm,
                                        Status = powerState,
                                        SubscriptionName = subscription.Data.DisplayName
                                    });
                                }
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching running virtual machines: {ex.Message}[/]");
            }
            return runningVMList;
        }

        public async Task<List<VirtualMachineResourceExtended>> FetchRunningVMInSubscriptionAsync(string subscriptionId)
        {
            List<VirtualMachineResourceExtended> runningVMList = new List<VirtualMachineResourceExtended>();

            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                    .StartAsync($"Fetching running virtual machines in subscription {subscription.Data.DisplayName}...", async ctx =>
                    {
                        var vms = subscription.GetVirtualMachines();
                        foreach (var vm in vms)
                        {
                            try
                            {
                                var instanceView = await vm.InstanceViewAsync();
                                string powerState = instanceView.Value.Statuses?
                                    .FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.Code.Substring(11) ?? "Unknown";

                                if (powerState == "running")
                                {
                                    runningVMList.Add(new VirtualMachineResourceExtended
                                    {
                                        VM = vm,
                                        Status = powerState,
                                        SubscriptionName = subscription.Data.DisplayName
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error fetching instance view for Virtual Machine [blue]{vm.Data.Name}[/] in subscription [yellow]{subscription.Data.DisplayName}[/]: {ex.Message}[/]");
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching running virtual machines for subscription {subscriptionId}: {ex.Message}[/]");
            }
            return runningVMList;
        }


        public async Task<OperationResult> StartVMAsync(VirtualMachineResource vmResource)
        {
            if (vmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine resource must not be null." };
            }

            try
            {
                await vmResource.PowerOnAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine [blue]{vmResource.Data.Name}[/] started successfully." };
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
                    403 => "You do not have permission to start this virtual machine.",
                    404 => "The specified virtual machine could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> StopVMAsync(VirtualMachineResource vmResource)
        {
            if (vmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine resource must not be null." };
            }

            try
            {
                await vmResource.DeallocateAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine [blue]{vmResource.Data.Name}[/] stopped successfully." };
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
                    403 => "You do not have permission to stop this virtual machine.",
                    404 => "The specified virtual machine could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> RestartVMAsync(VirtualMachineResource vmResource)
        {
            if (vmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine resource must not be null." };
            }

            try
            {
                await vmResource.RestartAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Virtual Machine [blue]{vmResource.Data.Name}[/] restarted." };
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
                    403 => "You do not have permission to restart this virtual machine.",
                    404 => "The specified virtual machine could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> DeleteVMAsync(VirtualMachineResource vmResource)
        {
            if (vmResource == null)
            {
                return new OperationResult { Success = false, Message = "Virtual Machine resource must not be null." };
            }

            try
            {
                await vmResource.DeleteAsync(WaitUntil.Completed, true, CancellationToken.None);
                return new OperationResult { Success = true, Message = $"Virtual Machine [blue]{vmResource.Data.Name}[/] deleted." };
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
                    403 => "You do not have permission to delete this virtual machine.",
                    404 => "The specified virtual machine could not be found.",
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
