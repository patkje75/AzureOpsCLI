using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.tags
{
    public class TagExportSubscriptionCommand : AsyncCommand
    {
        private readonly ITagService _tagService;
        private readonly ISubscritionService _subscriptionService;

        public TagExportSubscriptionCommand(ITagService tagService, ISubscritionService subscriptionService)
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
                var subscriptionDisplayName = selectedSubscription.Split(' ')[0];
                var resources = await _tagService.FetchResourcesWithTagsInSubscriptionAsync(subscriptionId);

                if (!resources.Any())
                {
                    AnsiConsole.MarkupLine("[red]No resources found in the selected subscription.[/]");
                    return -1;
                }

                // Choose export format
                var exportFormat = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Select export format:[/]")
                        .AddChoices(new[] { "JSON", "CSV" })
                );

                // Generate filename with timestamp and subscription name
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var extension = exportFormat.ToLower();
                var safeName = string.Join("_", subscriptionDisplayName.Split(Path.GetInvalidFileNameChars()));
                var filename = $"azure-resources-tags_{safeName}_{timestamp}.{extension}";

                string exportContent;
                if (exportFormat == "JSON")
                {
                    exportContent = await _tagService.ExportTagsToJsonAsync(resources);
                }
                else
                {
                    exportContent = await _tagService.ExportTagsToCsvAsync(resources);
                }

                // Write to file
                await File.WriteAllTextAsync(filename, exportContent);

                AnsiConsole.MarkupLine($"[green]✓ Export completed successfully![/]");
                AnsiConsole.MarkupLine($"[blue]File saved as:[/] {filename}");
                AnsiConsole.MarkupLine($"[yellow]Subscription:[/] {subscriptionDisplayName}");
                AnsiConsole.MarkupLine($"[yellow]Total resources exported:[/] {resources.Count}");
                AnsiConsole.MarkupLine($"[yellow]Resources with tags:[/] {resources.Count(r => r.Tags.Any())}");
                
                // Show file size
                var fileInfo = new FileInfo(filename);
                AnsiConsole.MarkupLine($"[yellow]File size:[/] {fileInfo.Length / 1024.0:F1} KB");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to export tags: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}