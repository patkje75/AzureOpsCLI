using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.tags
{
    public class TagRemoveSubscriptionCommand : AsyncCommand
    {
        private readonly ITagService _tagService;
        private readonly ISubscritionService _subscriptionService;

        public TagRemoveSubscriptionCommand(ITagService tagService, ISubscritionService subscriptionService)
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
                var resourcesWithTags = resources.Where(r => r.Tags.Any()).ToList();

                if (!resourcesWithTags.Any())
                {
                    AnsiConsole.MarkupLine("[red]No resources with tags found in the selected subscription.[/]");
                    return -1;
                }

                // Select resources to remove tags from
                var selectedResources = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("[green]Select resources to remove tags from:[/]")
                        .PageSize(15)
                        .MoreChoicesText("[grey](Move up and down to reveal more resources)[/]")
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle a resource, [green]<enter>[/] to accept)[/]")
                        .AddChoices(resourcesWithTags.Select(r => $"{r.ResourceName} ({r.ResourceType.Split('/').LastOrDefault()}) [{r.Tags.Count} tags]"))
                );

                if (!selectedResources.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No resources selected.[/]");
                    return 0;
                }

                // Get all unique tag keys from selected resources
                var allTagKeys = new HashSet<string>();
                foreach (var selectedResource in selectedResources)
                {
                    var resourceName = selectedResource.Split(' ')[0];
                    var resource = resourcesWithTags.FirstOrDefault(r => r.ResourceName == resourceName);
                    if (resource != null)
                    {
                        foreach (var tagKey in resource.Tags.Keys)
                        {
                            allTagKeys.Add(tagKey);
                        }
                    }
                }

                if (!allTagKeys.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tags found on selected resources.[/]");
                    return 0;
                }

                // Select tag keys to remove
                var selectedTagKeys = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("[red]Select tag keys to remove:[/]")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more tag keys)[/]")
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle a tag key, [green]<enter>[/] to accept)[/]")
                        .AddChoices(allTagKeys.OrderBy(k => k))
                );

                if (!selectedTagKeys.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tag keys selected for removal.[/]");
                    return 0;
                }

                // Confirm the operation
                AnsiConsole.MarkupLine($"\n[yellow]You are about to remove the following tag keys from {selectedResources.Count} resource(s):[/]");
                foreach (var tagKey in selectedTagKeys)
                {
                    AnsiConsole.MarkupLine($"[red]{tagKey}[/]");
                }

                if (!AnsiConsole.Confirm("\nContinue with tag removal?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                    return 0;
                }

                // Remove tags
                var successCount = 0;
                var failureCount = 0;

                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[red]Removing tags...[/]", maxValue: selectedResources.Count);

                        foreach (var selectedResource in selectedResources)
                        {
                            var resourceName = selectedResource.Split(' ')[0];
                            var resource = resourcesWithTags.FirstOrDefault(r => r.ResourceName == resourceName);
                            
                            if (resource != null)
                            {
                                // Only remove tags that exist on this resource
                                var tagsToRemove = selectedTagKeys.Where(key => resource.Tags.ContainsKey(key)).ToList();
                                
                                if (tagsToRemove.Any())
                                {
                                    var result = await _tagService.RemoveTagsAsync(resource.Resource, tagsToRemove);
                                    if (result.Success)
                                    {
                                        successCount++;
                                        AnsiConsole.MarkupLine($"[green]✓[/] {result.Message}");
                                    }
                                    else
                                    {
                                        failureCount++;
                                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to remove tags from [blue]{resource.ResourceName}[/]: {result.Message}");
                                    }
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[yellow]⚠[/] No matching tags to remove from [blue]{resource.ResourceName}[/]");
                                }
                            }
                            
                            task.Increment(1);
                        }
                    });

                AnsiConsole.MarkupLine($"\n[green]Operation completed: {successCount} successful, {failureCount} failed.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to remove tags: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}