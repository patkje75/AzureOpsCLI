using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.tags
{
    public class TagListAllCommand : AsyncCommand
    {
        private readonly ITagService _tagService;

        public TagListAllCommand(ITagService tagService)
        {
            _tagService = tagService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var resources = await _tagService.FetchAllResourcesWithTagsAsync();

                if (resources != null && resources.Any())
                {
                    var grid = new Grid();

                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(25));
                    grid.AddColumn(new GridColumn().Width(20));
                    grid.AddColumn(new GridColumn().Width(15));
                    grid.AddColumn(new GridColumn().Width(25));
                    grid.AddColumn(new GridColumn().Width(50));

                    grid.AddRow(
                        "[bold darkgreen]Resource Name[/]",
                        "[bold darkgreen]Resource Type[/]",
                        "[bold darkgreen]Resource Group[/]",
                        "[bold darkgreen]Location[/]",
                        "[bold darkgreen]Subscription[/]",
                        "[bold darkgreen]Tags[/]"
                    );

                    foreach (var resource in resources.OrderBy(r => r.SubscriptionName).ThenBy(r => r.ResourceName))
                    {
                        string tagsDisplay = resource.Tags.Any() 
                            ? string.Join(", ", resource.Tags.Select(t => $"{t.Key}={t.Value}"))
                            : "[dim]No tags[/]";

                        // Truncate long tag displays
                        if (tagsDisplay.Length > 50)
                        {
                            tagsDisplay = tagsDisplay.Substring(0, 47) + "...";
                        }

                        grid.AddRow(
                            $"[blue]{resource.ResourceName}[/]",
                            $"[yellow]{resource.ResourceType.Split('/').LastOrDefault()}[/]",
                            $"[yellow]{resource.ResourceGroupName}[/]",
                            $"[yellow]{resource.Location}[/]",
                            $"[yellow]{resource.SubscriptionName}[/]",
                            tagsDisplay
                        );
                    }

                    AnsiConsole.Write(grid);
                    AnsiConsole.MarkupLine($"\n[green]Total resources found: {resources.Count}[/]");
                    AnsiConsole.MarkupLine($"[green]Resources with tags: {resources.Count(r => r.Tags.Any())}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]No resources found.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to process resources: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}