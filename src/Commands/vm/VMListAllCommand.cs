using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

public class VMListAllCommand : AsyncCommand
{
    private readonly IVMService _computeService;

    public VMListAllCommand(IVMService computeService)
    {
        _computeService = computeService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            var vms = await _computeService.FetchAllVMAsync();

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
