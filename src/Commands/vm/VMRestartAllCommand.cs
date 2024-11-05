using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vm
{

    public class VMRestartAllCommand : AsyncCommand
    {
        private readonly IVMService _computeService;

        public VMRestartAllCommand(IVMService computeService)
        {
            _computeService = computeService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var runningVMs = await _computeService.FetchAllRunnigVMAsync();

            if (!runningVMs.Any())
            {
                AnsiConsole.MarkupLine("[red]No running Virtual Machine found.[/]");
                return 0;
            }

            var vmNames = runningVMs.Select(vm => $"{vm.VM.Data.Name} in {vm.VM.Data.Location} (Subscription {vm.SubscriptionName})").ToList();

            var selectedVMs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select virtual machines to restart:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more VMs)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a VM, [green]<enter>[/] to restart selected VMs)[/]")
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
                        var vmExtended = runningVMs.First(v => $"{v.VM.Data.Name} in {v.VM.Data.Location} (Subscription {v.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Restarting {vmExtended.VM.Data.Name}");

                        return _computeService.RestartVMAsync(vmExtended.VM)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Restart was [green]successfull[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to restart [blue]{vmExtended.VM.Data.Name}[/]: {t.Result.Message}";
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
