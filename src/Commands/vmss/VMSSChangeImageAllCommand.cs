using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

public class VMSSChangeImageAllCommand : AsyncCommand<VMSSChangeImageAllCommand.Settings>
{
    private readonly IVMSSService _vmssService;
    private readonly IImageGalleryService _imageGalleryService;

    public VMSSChangeImageAllCommand(IVMSSService vmssService, IImageGalleryService imageGalleryService)
    {
        _vmssService = vmssService;
        _imageGalleryService = imageGalleryService;
    }

    public class Settings : CommandSettings
    {
        [Description("Upgrades the selected virtual machine scale set to the latest model after the image is changed.")]
        [CommandOption("-u|--upgrade")]
        public bool Upgrade { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var vmssList = await _vmssService.FetchAllVMSSAsync();
            if (!vmssList.Any())
            {
                AnsiConsole.MarkupLine("[red]No Virtual Machine Scale Sets found.[/]");
                return 0;
            }

            var vmssNames = vmssList.Select(vmss => $"{vmss.VMSS.Data.Name} in {vmss.VMSS.Data.Location} (Subscription {vmss.SubscriptionName})").ToList();
            var selectedVMSS = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Virtual Machine Scale Sets to update:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more VMSS)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
                    .AddChoices(vmssNames)
            );
            if (!selectedVMSS.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No Virtual Machine Scale Set selected.[/]");
                return 0;
            }

            var galleries = await _imageGalleryService.FetchAllImageGalleriesAsync();
            if (!galleries.Any())
            {
                AnsiConsole.MarkupLine("[red]No Image Galleries found.[/]");
                return 0;
            }

            var galleryNames = galleries.Select(g => g.gallery.Data.Name).ToList();
            var selectedGalleryName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select an [green]Image Gallery[/] for all VMSS:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more galleries)[/]")
                    .AddChoices(galleryNames)
            );

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
                    .Title("Select an [green]Image Version[/] for all VMSS:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more images and versions)[/]")
                    .AddChoices(imageChoices)
            );

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
                    var updateTasks = selectedVMSS.Select(async vmssName =>
                    {
                        var vmss = vmssList.First(v => $"{v.VMSS.Data.Name} in {v.VMSS.Data.Location} (Subscription {v.SubscriptionName})" == vmssName);
                        var task = ctx.AddTask($"Updating Virtual Machine Scale Set {vmss.VMSS.Data.Name} with image {selectedImageName} version {selectedImageVersion}...");

                        var updateResult = await _vmssService.UpdateVMSSImageAsync(vmss.VMSS, selectedImageResource, selectedImageVersion);
                        if (updateResult.Success)
                        {
                            task.Description = $"Image update was [green]successful[/] for {vmss.VMSS.Data.Name}";
                            task.Increment(100);

                            if (settings.Upgrade)
                            {
                                var upgradeTask = ctx.AddTask($"Upgrading {vmss.VMSS.Data.Name} to latest model...");
                                Thread.Sleep(2000);
                                var upgradeResult = await _vmssService.UpgradeVMSSToLatestModelAsync(vmss.VMSS);

                                if (upgradeResult.Success)
                                {
                                    upgradeTask.Description = $"Upgrade was [green]successful[/]: {upgradeResult.Message}";
                                    upgradeTask.Increment(100);
                                }
                                else
                                {
                                    upgradeTask.Description = $"[red]Failed[/] to upgrade {vmss.VMSS.Data.Name}: {upgradeResult.Message}";
                                    upgradeTask.Increment(100);
                                }
                            }
                        }
                        else
                        {
                            task.Description = $"[red]Failed[/] to update {vmss.VMSS.Data.Name}: {updateResult.Message}";
                            task.Increment(100);
                        }
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
