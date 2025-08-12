using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.tags
{
    public class TagApplySubscriptionCommand : AsyncCommand
    {
        private readonly ITagService _tagService;
        private readonly ISubscritionService _subscriptionService;

        public TagApplySubscriptionCommand(ITagService tagService, ISubscritionService subscriptionService)
        {
            _tagService = tagService;
            _subscriptionService = subscriptionService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var subscriptions = await _subscriptionService.FetchSubscriptionsAsync();
                if (!subscriptions.Any())
                {
                    AnsiConsole.MarkupLine("[red]No subscriptions found.[/]");
                    return -1;
                }

                var selectedSubscription = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Select a subscription:[/]")
                        .PageSize(10)
                        .AddChoices(subscriptions)
                );

                var subscriptionId = selectedSubscription.Split('(', ')')[1];
                var resources = await _tagService.FetchResourcesWithTagsInSubscriptionAsync(subscriptionId);

                if (!resources.Any())
                {
                    AnsiConsole.MarkupLine("[red]No resources found in the selected subscription.[/]");
                    return -1;
                }

                // Select resources to tag
                var selectedResources = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("[green]Select resources to apply tags to:[/]")
                        .PageSize(15)
                        .MoreChoicesText("[grey](Move up and down to reveal more resources)[/]")
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle a resource, [green]<enter>[/] to accept)[/]")
                        .AddChoices(resources.Select(r => $"{r.ResourceName} ({r.ResourceType.Split('/').LastOrDefault()})"))
                );

                if (!selectedResources.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No resources selected.[/]");
                    return 0;
                }

                // Get tags to apply
                var tags = new Dictionary<string, string>();
                var addMoreTags = true;

                AnsiConsole.MarkupLine("\n[green]Enter tags to apply (key=value pairs):[/]");
                
                while (addMoreTags)
                {
                    var tagKey = AnsiConsole.Ask<string>("[yellow]Tag key:[/]");
                    var tagValue = AnsiConsole.Ask<string>("[yellow]Tag value:[/]");
                    
                    tags[tagKey] = tagValue;
                    
                    addMoreTags = AnsiConsole.Confirm("Add another tag?", false);
                }

                if (!tags.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tags specified.[/]");
                    return 0;
                }

                // Confirm the operation
                AnsiConsole.MarkupLine($"\n[yellow]You are about to apply the following tags to {selectedResources.Count} resource(s):[/]");
                foreach (var tag in tags)
                {
                    AnsiConsole.MarkupLine($"[blue]{tag.Key}[/] = [green]{tag.Value}[/]");
                }

                if (!AnsiConsole.Confirm("\nContinue with tag application?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                    return 0;
                }

                // Apply tags
                var successCount = 0;
                var failureCount = 0;

                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Applying tags...[/]", maxValue: selectedResources.Count);

                        foreach (var selectedResource in selectedResources)
                        {
                            var resourceName = selectedResource.Split(' ')[0];
                            var resource = resources.FirstOrDefault(r => r.ResourceName == resourceName);
                            
                            if (resource != null)
                            {
                                var result = await _tagService.ApplyTagsAsync(resource.Resource, tags);
                                if (result.Success)
                                {
                                    successCount++;
                                    AnsiConsole.MarkupLine($"[green]✓[/] {result.Message}");
                                }
                                else
                                {
                                    failureCount++;
                                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to apply tags to [blue]{resource.ResourceName}[/]: {result.Message}");
                                }
                            }
                            
                            task.Increment(1);
                        }
                    });

                AnsiConsole.MarkupLine($"\n[green]Operation completed: {successCount} successful, {failureCount} failed.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to apply tags: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}