using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vm
{
    public class IGImagesListAllCommand : AsyncCommand
    {
        private readonly IImageGalleryService _imageGalleryService;

        public IGImagesListAllCommand(IImageGalleryService imageGalleryService)
        {
            _imageGalleryService = imageGalleryService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var galleries = await _imageGalleryService.FetchAllImageGalleriesAsync();
                if (!galleries.Any())
                {
                    AnsiConsole.MarkupLine("[red]No image galleries found.[/]");
                    return 0;
                }

                var galleryNames = galleries.Select(g => g.gallery.Data.Name).ToList();
                var selectedGalleryName = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select an [green]Image Gallery[/]:")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more galleries)[/]")
                        .AddChoices(galleryNames));

                var selectedGallery = galleries.FirstOrDefault(g => g.gallery.Data.Name == selectedGalleryName);
                if (selectedGallery == null)
                {
                    AnsiConsole.MarkupLine($"[red]Error: Selected gallery '{selectedGalleryName}' not found.[/]");
                    return 1;
                }


                var images = await _imageGalleryService.ListImagesInGalleryAsync(selectedGallery);
                if (!images.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]No images found in gallery '{selectedGalleryName}'.[/]");
                    return 0;
                }

                var table = new Table
                {
                    Title = new TableTitle($"[bold green]Image Gallery: {selectedGalleryName}[/]")
                };
                table.AddColumn("Image Name");
                table.AddColumn("Image Version");

                foreach (var image in images)
                {
                    var versions = await _imageGalleryService.ListImageVersionsAsync(image);


                    table.AddRow($"[bold blue]{image.Data.Name}[/]", "");

                    if (!versions.Any())
                    {
                        table.AddRow("", "[yellow]No versions available[/]");
                    }
                    else
                    {
                        var sortedVersions = versions.OrderByDescending(v => v.Data.Name).ToList();
                        var latestVersion = sortedVersions.FirstOrDefault();

                        foreach (var version in sortedVersions)
                        {
                            if (version.Data.Name == latestVersion?.Data.Name)
                            {
                                table.AddRow("", $"[green]{version.Data.Name}[/]");
                            }
                            else
                            {
                                table.AddRow("", $"[yellow]{version.Data.Name}[/]");
                            }
                        }
                    }
                }

                AnsiConsole.Write(table);
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 1;
            }
        }


    }
}
