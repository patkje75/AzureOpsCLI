using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.resourcelock
{
    public class LockRemoveAllCommand : AsyncCommand
    {
        private readonly ILockService _lockService;

        public LockRemoveAllCommand(ILockService lockService)
        {
            _lockService = lockService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var locks = await AnsiConsole.Status()
                    .StartAsync("Fetching resource locks...", async ctx =>
                    {
                        return await _lockService.FetchAllLocksAsync();
                    });

                if (!locks.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No resource locks found.[/]");
                    return 0;
                }

                var lockNames = locks.Select(l => $"{l.Lock.Data.Name} ({l.Lock.Data.Level}) on {l.ResourceName} - {l.SubscriptionName}").ToList();
                var selectedLocks = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Select lock(s) to [red]remove[/]:")
                        .NotRequired()
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more locks)[/]")
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to remove)[/]")
                        .AddChoices(lockNames));

                if (!selectedLocks.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No locks selected for removal.[/]");
                    return 0;
                }

                var confirmation = AnsiConsole.Confirm($"Are you sure you want to remove {selectedLocks.Count} lock(s)?", false);
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
                        var tasks = selectedLocks.Select(lockName =>
                        {
                            var lockItem = locks.First(l => $"{l.Lock.Data.Name} ({l.Lock.Data.Level}) on {l.ResourceName} - {l.SubscriptionName}" == lockName);
                            var task = ctx.AddTask($"Removing lock {lockItem.Lock.Data.Name}");

                            return _lockService.RemoveLockAsync(lockItem.Lock)
                                .ContinueWith(t =>
                                {
                                    if (t.Result.Success)
                                    {
                                        task.Description = $"Remove was [green]successful[/]: {t.Result.Message}";
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
                AnsiConsole.MarkupLine($"[red]Failed to remove locks: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
