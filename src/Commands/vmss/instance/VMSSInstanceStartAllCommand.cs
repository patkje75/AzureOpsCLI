using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vmss.instance
{
    public class VMSSInstanceStartAllCommand : AsyncCommand
    {
        private readonly IVMSSVMService _vmssvmService;

        public VMSSInstanceStartAllCommand(IVMSSVMService vmssvmService)
        {
            _vmssvmService = vmssvmService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var deallocatedVMSSs = await _vmssvmService.FetchAllStoppedVMSSInstancesAsync();

            if (!deallocatedVMSSs.Any())
            {
                AnsiConsole.MarkupLine("[red]No stopped Virtual Machine Scale Set instances found.[/]");
                return 0;
            }

            var vmNames = deallocatedVMSSs.Select(vm => $"{vm.VMSSVm.Data.Name} in scale set {vm.VMSS.Data.Name}, Location: {vm.VMSSVm.Data.Location} (Subscription {vm.SubscriptionName})").ToList();

            var selectedVMs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Scale Set instance to start:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more instances)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a Scale Set instance, [green]<enter>[/] to start selected instance)[/]")
                    .AddChoices(vmNames));

            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
            new TaskDescriptionColumn(),
            new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var tasks = selectedVMs.Select(vmName =>
                    {
                        var vmExtended = deallocatedVMSSs.First(v => $"{v.VMSSVm.Data.Name} in scale set {v.VMSS.Data.Name}, Location: {v.VMSSVm.Data.Location} (Subscription {v.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Starting {vmExtended.VMSSVm.Data.Name}");

                        return _vmssvmService.StartVMSSInstanceAsync(vmExtended.VMSSVm)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Start was [green]successfull[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to start [blue]{vmExtended.VMSSVm.Data.Name}[/]: {t.Result.Message}";
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
