using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.disk
{
    public class DiskDeleteAllCommand : AsyncCommand
    {
        private readonly IDiskService _diskService;

        public DiskDeleteAllCommand(IDiskService diskService)
        {
            _diskService = diskService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var disks = await AnsiConsole.Status()
                    .StartAsync("Fetching unattached disks...", async ctx =>
                    {
                        return await _diskService.FetchUnattachedDisksAsync();
                    });

                if (!disks.Any())
                {
                    AnsiConsole.MarkupLine("[green]No unattached/orphaned disks found.[/]");
                    return 0;
                }

                AnsiConsole.MarkupLine($"[yellow]Found {disks.Count} unattached disk(s)[/]");

                var diskNames = disks.Select(d => $"{d.Disk.Data.Name} ({d.Disk.Data.DiskSizeGB} GB) - {d.SubscriptionName}").ToList();
                var selectedDisks = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Select unattached disk(s) to [red]delete[/]:")
                        .NotRequired()
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more disks)[/]")
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle a disk, [green]<enter>[/] to delete)[/]")
                        .AddChoices(diskNames));

                if (!selectedDisks.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No disks selected for deletion.[/]");
                    return 0;
                }

                var confirmation = AnsiConsole.Confirm($"Are you sure you want to delete {selectedDisks.Count} disk(s)? [red]This action cannot be undone.[/]", false);
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
                        var tasks = selectedDisks.Select(diskName =>
                        {
                            var disk = disks.First(d => $"{d.Disk.Data.Name} ({d.Disk.Data.DiskSizeGB} GB) - {d.SubscriptionName}" == diskName);
                            var task = ctx.AddTask($"Deleting {disk.Disk.Data.Name}");

                            return _diskService.DeleteDiskAsync(disk.Disk)
                                .ContinueWith(t =>
                                {
                                    if (t.Result.Success)
                                    {
                                        task.Description = $"Delete was [green]successful[/]: {t.Result.Message}";
                                        task.Increment(100);
                                    }
                                    else
                                    {
                                        task.Description = $"[red]Failed[/]: {t.Result.Message}";
                                        task.Increment(100);
                                    }
                                });
                        }).ToList();

                        await Task.WhenAll(tasks);
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to delete disks: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
