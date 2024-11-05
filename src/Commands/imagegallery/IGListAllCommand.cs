using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.imagegallery
{
    public class IGListAllCommand : AsyncCommand
    {
        private readonly IImageGalleryService _imageGalleryService;

        public IGListAllCommand(IImageGalleryService imageGalleryService)
        {
            _imageGalleryService = imageGalleryService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var imageGalleries = await _imageGalleryService.FetchAllImageGalleriesAsync();

                if (imageGalleries != null && imageGalleries.Any())
                {
                    var grid = new Grid();

                    grid.AddColumn(new GridColumn().Width(35));
                    grid.AddColumn(new GridColumn().Width(15));
                    grid.AddColumn(new GridColumn().Width(30));

                    grid.AddRow(
                        "[bold darkgreen]Image Gallery Name[/]",
                        "[bold darkgreen]Location[/]",
                        "[bold darkgreen]Subscription[/]"
                    );

                    foreach (var ig in imageGalleries)
                    {
                        grid.AddRow(
                            $"[blue]{ig.gallery.Data.Name}[/]",
                            $"[yellow]{ig.gallery.Data.Location}[/]",
                            $"[yellow]{ig.SubscriptionName}[/]"
                        );
                    }

                    AnsiConsole.Write(grid);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]No image galleries found.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to process image galleries: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
