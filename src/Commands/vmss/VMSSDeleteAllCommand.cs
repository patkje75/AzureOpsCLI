using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vmss
{
    public class VMSSDeleteAllCommand : AsyncCommand
    {
        private readonly IVMSSService _vmssService;

        public VMSSDeleteAllCommand(IVMSSService vmssService)
        {
            _vmssService = vmssService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var allVMSSs = await _vmssService.FetchAllVMSSAsync();
            if (!allVMSSs.Any())
            {
                AnsiConsole.MarkupLine("[red]No Virtual Machine Scale Sets found.[/]");
                return 0;
            }

            var vmNames = allVMSSs.Select(vm => $"{vm.VMSS.Data.Name} in {vm.VMSS.Data.Location} (Subscription {vm.SubscriptionName})").ToList();
            var selectedVMs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Scale Set(s) to delete:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more Virtual Machine Scales Sets)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a Scale Set, [green]<enter>[/] to delete selected Scale Sets)[/]")
                    .AddChoices(vmNames));

            if (!selectedVMs.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No VMSS selected for deletion.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine("The following Scale Sets will be [red]deleted:[/]");
            foreach (var vmName in selectedVMs)
            {
                AnsiConsole.MarkupLine($"[blue]- {vmName}[/]");
            }

            var confirmation = AnsiConsole.Confirm("Are you sure you want to delete the selected Scale Set(s)? [red]This action cannot be undone.[/]", false);
            if (!confirmation)
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
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
                        var vmExtended = allVMSSs.First(v => $"{v.VMSS.Data.Name} in {v.VMSS.Data.Location} (Subscription {v.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Deleting {vmExtended.VMSS.Data.Name}");

                        return _vmssService.DeleteVMSSAsync(vmExtended.VMSS)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Delete was [green]successfull[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to delete [blue]{vmExtended.VMSS.Data.Name}[/]: {t.Result.Message}";
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
