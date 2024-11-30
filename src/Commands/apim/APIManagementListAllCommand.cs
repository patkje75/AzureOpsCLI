using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.apim
{
    public class APIManagementListCommand : AsyncCommand
    {
        private readonly IAPIManagementService _apiManagementService;

        public APIManagementListCommand(IAPIManagementService apiManagementService)
        {
            _apiManagementService = apiManagementService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {

                var resources = await _apiManagementService.FetchAllAPIMAsync();

                if (!resources.Any())
                {
                    AnsiConsole.MarkupLine("[red]No API Management services found.[/]");
                    return 0;
                }

                var grid = new Grid();

                grid.AddColumn(new GridColumn().Width(35));
                grid.AddColumn(new GridColumn().Width(15));
                grid.AddColumn(new GridColumn().Width(30));
                grid.AddColumn(new GridColumn().Width(10));
                grid.AddColumn(new GridColumn().Width(16));
                grid.AddColumn(new GridColumn().Width(22));
                grid.AddColumn(new GridColumn().Width(12));

                grid.AddRow(
                    "[bold darkgreen]APIM Name[/]",
                    "[bold darkgreen]Location[/]",
                    "[bold darkgreen]Subscription[/]",
                    "[bold darkgreen]Sku[/]",
                    "[bold darkgreen]Platform Version[/]",
                    "[bold darkgreen]Public Network Access[/]",
                    "[bold darkgreen]VNet Type[/]"
                     );

                foreach (var resource in resources)
                {
                    var publicNetworkAccess = resource.APIManagementService.Data.PublicNetworkAccess?.ToString() ?? "Disabled";
                    var PublicNetworkAccessColor = publicNetworkAccess.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ? "red" : "green";

                    var virtualNetworkType = resource.APIManagementService.Data.VirtualNetworkType?.ToString() ?? "None";
                    var virtualNetworkColor = virtualNetworkType.Equals("External", StringComparison.OrdinalIgnoreCase) ? "red" : "green";



                    grid.AddRow(
                        $"[blue]{resource.APIManagementService.Data.Name}[/]",
                        $"[yellow]{resource.APIManagementService.Data.Location}[/]",
                        $"[yellow]{resource.SubscriptionName}[/]",
                        $"[yellow]{resource.APIManagementService.Data.Sku.Name}[/]",
                        $"[yellow]{resource.APIManagementService.Data.PlatformVersion}[/]",
                        $"[{PublicNetworkAccessColor}]{publicNetworkAccess}[/]",
                        $"[{virtualNetworkColor}]{virtualNetworkType}[/]"


                    );

                }

                AnsiConsole.Write(grid);

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                return -1;
            }
        }

    }
}
