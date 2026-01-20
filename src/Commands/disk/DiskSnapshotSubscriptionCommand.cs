using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.disk
{
    public class DiskSnapshotSubscriptionCommand : AsyncCommand
    {
        private readonly IDiskService _diskService;
        private readonly ISubscritionService _subscriptionService;

        public DiskSnapshotSubscriptionCommand(IDiskService diskService, ISubscritionService subscriptionService)
        {
            _diskService = diskService;
            _subscriptionService = subscriptionService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();
                var selectedSubscription = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]subscription[/]:")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                        .AddChoices(subscriptionChoices));

                string subscriptionId = selectedSubscription.Split('(').Last().TrimEnd(')');

                var disks = await AnsiConsole.Status()
                    .StartAsync("Fetching managed disks...", async ctx =>
                    {
                        return await _diskService.FetchDisksInSubscriptionAsync(subscriptionId);
                    });

                if (!disks.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No managed disks found in the selected subscription.[/]");
                    return 0;
                }

                var diskNames = disks.Select(d => $"{d.Disk.Data.Name} ({d.Disk.Data.DiskSizeGB} GB)").ToList();
                var selectedDisks = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Select disk(s) to create snapshots:")
                        .NotRequired()
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more disks)[/]")
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle a disk, [green]<enter>[/] to create snapshots)[/]")
                        .AddChoices(diskNames));

                if (!selectedDisks.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No disks selected for snapshot.[/]");
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
                            var disk = disks.First(d => $"{d.Disk.Data.Name} ({d.Disk.Data.DiskSizeGB} GB)" == diskName);
                            var snapshotName = $"{disk.Disk.Data.Name}-snapshot-{DateTime.UtcNow:yyyyMMddHHmmss}";
                            var task = ctx.AddTask($"Creating snapshot for {disk.Disk.Data.Name}");

                            return _diskService.CreateSnapshotAsync(disk.Disk, snapshotName)
                                .ContinueWith(t =>
                                {
                                    if (t.Result.Success)
                                    {
                                        task.Description = $"Snapshot was [green]successful[/]: {t.Result.Message}";
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
                AnsiConsole.MarkupLine($"[red]Failed to create snapshots: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
