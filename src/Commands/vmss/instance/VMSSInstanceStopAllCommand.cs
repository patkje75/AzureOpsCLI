using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vmss.instance
{
    public class VMSSInstanceStopAllCommand : AsyncCommand
    {
        private readonly IVMSSVMService _vmssvmService;

        public VMSSInstanceStopAllCommand(IVMSSVMService vmssvmService)
        {
            _vmssvmService = vmssvmService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var runningVMSSInstances = await _vmssvmService.FetchAllRunningVMSSInstancesAsync();

            if (!runningVMSSInstances.Any())
            {
                AnsiConsole.MarkupLine("[red]No running Virtual Machine Scale Set instances found.[/]");
                return 0;
            }

            var vmNames = runningVMSSInstances.Select(vm => $"{vm.VMSSVm.Data.Name} in scale set {vm.VMSS.Data.Name}, Location: {vm.VMSSVm.Data.Location} (Subscription {vm.SubscriptionName})").ToList();

            var selectedVMs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Scale Set instance to stop:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more instances)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a Scale Set instance, [green]<enter>[/] to stop selected instance)[/]")
                    .AddChoices(vmNames));

            if (!selectedVMs.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No Virtual Machine Scale Set instances selected.[/]");
                return 0;
            }

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
                        var vmExtended = runningVMSSInstances.First(v => $"{v.VMSSVm.Data.Name} in scale set {v.VMSS.Data.Name}, Location: {v.VMSSVm.Data.Location} (Subscription {v.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Stopping {vmExtended.VMSSVm.Data.Name}");

                        return _vmssvmService.StopVMSSInstanceAsync(vmExtended.VMSSVm)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Stop was [green]successful[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to stop [blue]{vmExtended.VMSSVm.Data.Name}[/]: {t.Result.Message}";
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
