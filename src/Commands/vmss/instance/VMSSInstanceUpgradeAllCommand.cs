using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vmss
{
    public class VMSSInstanceUpgradeAllCommand : AsyncCommand
    {
        private readonly IVMSSVMService _vmssVmService;

        public VMSSInstanceUpgradeAllCommand(IVMSSVMService vmssVmService)
        {
            _vmssVmService = vmssVmService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var allVMSSInstances = await _vmssVmService.FetchAllVMSSInstancesAsync();
            if (!allVMSSInstances.Any())
            {
                AnsiConsole.MarkupLine("[red]No Virtual Machine Scale Set Instances found.[/]");
                return 0;
            }

            var vmNames = allVMSSInstances.Select(vm => $"{vm.VMSSVm.Data.Name} in scale set {vm.VMSS.Data.Name}, Location: {vm.VMSSVm.Data.Location} (Subscription {vm.SubscriptionName})").ToList();
            var selectedVMSSs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Scale Set Instances to upgrade to the latest model:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more Virtual Machine Scales Sets)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a Scale Set, [green]<enter>[/] to upgrade selected Scale Set Instance)[/]")
                    .AddChoices(vmNames));

            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var tasks = selectedVMSSs.Select(vmName =>
                    {
                        var vmExtended = allVMSSInstances.First(vm => $"{vm.VMSSVm.Data.Name} in scale set {vm.VMSS.Data.Name}, Location: {vm.VMSSVm.Data.Location} (Subscription {vm.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Upgrading {vmExtended.VMSS.Data.Name} to the latest model");

                        return _vmssVmService.UpgradeVMSSInstanceToLatestModelAsync(vmExtended.VMSSVm, vmExtended.VMSS)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Upgrade was [green]successful[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to upgrade [blue]{vmExtended.VMSS.Data.Name}[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                            });
                    }).ToList();

                    await Task.WhenAll(tasks);
                });

            return 0;
        }
    }
}
