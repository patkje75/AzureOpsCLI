using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

public class VMSSChangeImageSubscriptionCommand : AsyncCommand
{
    private readonly IVMSSService _vmssService;
    private readonly IImageGalleryService _imageGalleryService;
    private readonly ISubscritionService _subscriptionService;

    public VMSSChangeImageSubscriptionCommand(IVMSSService vmssService, IImageGalleryService imageGalleryService, ISubscritionService subscriptionService)
    {
        _vmssService = vmssService;
        _imageGalleryService = imageGalleryService;
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
            var vmssList = await _vmssService.FetchVMSSInSubscriptionAsync(subscriptionId);

            if (!vmssList.Any())
            {
                AnsiConsole.MarkupLine("[red]No Virtual Machine Scale Sets found in the selected subscription.[/]");
                return 0;
            }

            var vmssNames = vmssList.Select(vmss => $"{vmss.VMSS.Data.Name} in {vmss.VMSS.Data.Location}").ToList();
            var selectedVMSS = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select VM Scale Sets to update:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more Virtual Machine Scales Sets)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
                    .AddChoices(vmssNames));

            if (!selectedVMSS.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No VM Scale Set selected.[/]");
                return 0;
            }

            var galleries = await _imageGalleryService.FetchAllImageGalleriesAsync();
            if (!galleries.Any())
            {
                AnsiConsole.MarkupLine("[red]No Image Galleries found in the selected subscription.[/]");
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

            var imageChoices = new List<string>();
            foreach (var image in images)
            {
                var versions = await _imageGalleryService.ListImageVersionsAsync(image);
                imageChoices.AddRange(versions.Select(version => $"{image.Data.Name} - {version.Data.Name}"));
            }

            var selectedImage = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select an [green]Image Version[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more images and versions)[/]")
                    .AddChoices(imageChoices));

            var selectedImageName = selectedImage.Split(" - ")[0];
            var selectedImageVersion = selectedImage.Split(" - ")[1];
            var selectedImageResource = images.FirstOrDefault(img => img.Data.Name == selectedImageName);
            if (selectedImageResource == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Selected image '{selectedImageName}' not found.[/]");
                return 1;
            }

            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var updateTasks = selectedVMSS.Select(vmssName =>
                    {
                        var vmss = vmssList.First(v => $"{v.VMSS.Data.Name} in {v.VMSS.Data.Location}" == vmssName);
                        var task = ctx.AddTask($"Updating VMSS {vmss.VMSS.Data.Name} with image {selectedImageName} version {selectedImageVersion}...");

                        return _vmssService.UpdateVMSSImageAsync(vmss.VMSS, selectedImageResource, selectedImageVersion)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"[green]Successfully[/] updated [green]{vmss.VMSS.Data.Name}[/] with image [yellow]{selectedImageName}[/] version [yellow]{selectedImageVersion}[/]";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to update [blue]{vmss.VMSS.Data.Name}[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                            });
                    }).ToList();

                    await Task.WhenAll(updateTasks);
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
