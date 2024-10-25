using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading.Tasks;

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
                grid.AddColumn(new GridColumn().Width(10));
                grid.AddColumn(new GridColumn().Width(30));

                grid.AddRow(
                    "[bold darkgreen]VM Name[/]",
                    "[bold darkgreen]Location[/]",
                    "[bold darkgreen]Subscription[/]",
                    "[bold darkgreen]Status[/]",
                    "[bold darkgreen]Image Name[/]"
                );

                foreach (var vm in vms)
                {

                    string status = vm.Status?.ToString() ?? "unknown";

                    var statusColor = status.Equals("running", StringComparison.OrdinalIgnoreCase) ? "green" : "red";

                    var imageReference = vm.VM.Data.StorageProfile?.ImageReference;
                    string imageReferenceId = imageReference?.Id;
                    string imageName = "Not a compute gallery image";

                    Console.WriteLine(imageReferenceId);

                    if (!string.IsNullOrEmpty(imageReferenceId))
                    {
                        var parts = imageReferenceId.Split('/');
                        if (parts.Length >= 10)
                        {
                            imageName = parts[10];
                        }
                    }

                    grid.AddRow(
                        $"[blue]{vm.VM.Data.Name}[/]", 
                        $"[yellow]{vm.VM.Data.Location}[/]",
                        $"[yellow]{vm.SubscriptionName}[/]", 
                        $"[{statusColor}]{status}[/]", 
                        $"[yellow]{imageName}[/]"

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
