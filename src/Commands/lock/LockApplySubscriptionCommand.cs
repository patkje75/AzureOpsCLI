using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.resourcelock
{
    public class LockApplySubscriptionCommand : AsyncCommand
    {
        private readonly ILockService _lockService;
        private readonly ISubscritionService _subscriptionService;

        public LockApplySubscriptionCommand(ILockService lockService, ISubscritionService subscriptionService)
        {
            _lockService = lockService;
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

                var resources = await AnsiConsole.Status()
                    .StartAsync("Fetching resource groups...", async ctx =>
                    {
                        return await _lockService.FetchLockableResourcesInSubscriptionAsync(subscriptionId);
                    });

                // Filter to show only unlocked resources
                var unlockedResources = resources.Where(r => !r.HasLock).ToList();

                if (!unlockedResources.Any())
                {
                    AnsiConsole.MarkupLine("[green]All resource groups in this subscription already have locks applied.[/]");
                    return 0;
                }

                var resourceNames = unlockedResources.Select(r => $"{r.Name} ({r.Type})").ToList();
                var selectedResources = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Select resource group(s) to apply lock:")
                        .NotRequired()
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more resources)[/]")
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to apply locks)[/]")
                        .AddChoices(resourceNames));

                if (!selectedResources.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No resources selected.[/]");
                    return 0;
                }

                var lockLevel = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select lock level:")
                        .AddChoices(new[] { "CanNotDelete", "ReadOnly" }));

                await AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new SpinnerColumn(),
                    })
                    .StartAsync(async ctx =>
                    {
                        var tasks = selectedResources.Select(resourceName =>
                        {
                            var resource = unlockedResources.First(r => $"{r.Name} ({r.Type})" == resourceName);
                            var lockName = $"lock-{resource.Name}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                            var task = ctx.AddTask($"Applying lock to {resource.Name}");

                            return _lockService.ApplyLockAsync(resource.ResourceId, lockName, lockLevel)
                                .ContinueWith(t =>
                                {
                                    if (t.Result.Success)
                                    {
                                        task.Description = $"Lock was [green]successful[/]: {t.Result.Message}";
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
                AnsiConsole.MarkupLine($"[red]Failed to apply locks: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
