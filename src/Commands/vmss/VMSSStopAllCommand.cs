using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vmss
{
    public class VMSSStopAllCommand : AsyncCommand
    {
        private readonly IVMSSService _vmssService;

        public VMSSStopAllCommand(IVMSSService vmssService)
        {
            _vmssService = vmssService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var runningVMSSs = await _vmssService.FetchAllRunningVMSSAsync();

            if (!runningVMSSs.Any())
            {
                AnsiConsole.MarkupLine("[red]No running Virtual Machine Scale Sets found.[/]");
                return 0;
            }

            var vmNames = runningVMSSs.Select(vm => $"{vm.VMSS.Data.Name} in {vm.VMSS.Data.Location} (Subscription {vm.SubscriptionName})").ToList();

            var selectedVMs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Scale Set to stop:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more Virtual Machine Scales Sets)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a Scale Set, [green]<enter>[/] to start selected Scale Set)[/]")
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
                        var vmExtended = runningVMSSs.First(v => $"{v.VMSS.Data.Name} in {v.VMSS.Data.Location} (Subscription {v.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Stopping {vmExtended.VMSS.Data.Name}");

                        return _vmssService.StopVMSSAsync(vmExtended.VMSS)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Stop was [green]successfull[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to stop [blue]{vmExtended.VMSS.Data.Name}[/]: {t.Result.Message}";
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
