using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.tags
{
    public class TagExportAllCommand : AsyncCommand
    {
        private readonly ITagService _tagService;

        public TagExportAllCommand(ITagService tagService)
        {
            _tagService = tagService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var resources = await _tagService.FetchAllResourcesWithTagsAsync();

                if (!resources.Any())
                {
                    AnsiConsole.MarkupLine("[red]No resources found.[/]");
                    return -1;
                }

                // Choose export format
                var exportFormat = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Select export format:[/]")
                        .AddChoices(new[] { "JSON", "CSV" })
                );

                // Generate filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var extension = exportFormat.ToLower();
                var filename = $"azure-resources-tags-all_{timestamp}.{extension}";

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