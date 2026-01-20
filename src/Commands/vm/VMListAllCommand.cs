using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Settings;
using AzureOpsCLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

public class VMListAllCommand : AsyncCommand<ListCommandSettings>
{
    private readonly IVMService _computeService;

    public VMListAllCommand(IVMService computeService)
    {
        _computeService = computeService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ListCommandSettings settings)
    {
        try
        {
            var vms = await _computeService.FetchAllVMAsync();

            // Apply filter if specified
            if (!string.IsNullOrEmpty(settings.Filter))
            {
                vms = vms.Where(vm => vm.VM.Data.Name.Contains(settings.Filter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (vms != null && vms.Any())
            {
                var grid = new Grid();

                grid.AddColumn(new GridColumn().Width(35));
                grid.AddColumn(new GridColumn().Width(30));
                grid.AddColumn(new GridColumn().Width(30));
                grid.AddColumn(new GridColumn().Width(20));
                grid.AddColumn(new GridColumn().Width(30));
                grid.AddColumn(new GridColumn().Width(20));
                grid.AddColumn(new GridColumn().Width(17));

                grid.AddRow(
                    "[bold darkgreen]VM Name[/]",
                    "[bold darkgreen]Location[/]",
                    "[bold darkgreen]Subscription[/]",
                    "[bold darkgreen]Status[/]",
                    "[bold darkgreen]Image Name[/]",
                    "[bold darkgreen]Version[/]",
                    "[bold darkgreen]Marketplace Image[/]"
                     );

                foreach (var vm in vms)
                {

                    string status = vm.Status?.ToString() ?? "unknown";
                    var imageReference = vm.VM.Data.StorageProfile?.ImageReference;
                    string imageReferenceId = imageReference?.Id;
                    string imageName = "No image name found";
                    string imageVersion = "No version specified";
                    bool marketplace = false;


                    if (!string.IsNullOrEmpty(imageReferenceId))
                    {
                        var parts = imageReferenceId.Split('/');
                        if (parts.Length >= 10)
                        {
                            imageName = parts[10];
                            imageVersion = imageReference.ExactVersion;
                        }

                    }
                    else
                    {
                        imageName = imageReference.Sku;
                        imageVersion = imageReference.Version;
                        marketplace = true;
                    }

                    var marketplaceColor = marketplace != true ? "red" : "green";
                    var statusColor = status.Equals("running", StringComparison.OrdinalIgnoreCase) ? "green" : "red";

                    grid.AddRow(
                        $"[blue]{vm.VM.Data.Name}[/]",
                        $"[yellow]{vm.VM.Data.Location}[/]",
                        $"[yellow]{vm.SubscriptionName}[/]",
                        $"[{statusColor}]{status}[/]",
                        $"[yellow]{imageName}[/]",
                        $"[yellow]{imageVersion}[/]",
                        $"[{marketplaceColor}]{marketplace}[/]"
                    );
                }

                AnsiConsole.Write(grid);

                // Export if requested
                if (!string.IsNullOrEmpty(settings.ExportFormat))
                {
                    await ExportHelper.ExportDataAsync(
                        vms,
                        settings.ExportFormat,
                        "virtual-machines-all",
                        vm => new[] {
                            vm.VM.Data.Name,
                            vm.VM.Data.Location.ToString(),
                            vm.SubscriptionName,
                            vm.Status ?? "unknown",
                            vm.VM.Data.StorageProfile?.ImageReference?.Sku ?? "N/A",
                            vm.VM.Data.StorageProfile?.ImageReference?.Version ?? "N/A"
                        },
                        new[] { "Name", "Location", "Subscription", "Status", "ImageName", "Version" },
                        vm => new {
                            Name = vm.VM.Data.Name,
                            Location = vm.VM.Data.Location.ToString(),
                            Subscription = vm.SubscriptionName,
                            Status = vm.Status ?? "unknown",
                            ImageName = vm.VM.Data.StorageProfile?.ImageReference?.Sku ?? "N/A",
                            Version = vm.VM.Data.StorageProfile?.ImageReference?.Version ?? "N/A"
                        }
                    );
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]No virtual machines found.[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to process virtual machines: {ex.Message}[/]");
            return -1;
        }

        return 0;
    }
}
