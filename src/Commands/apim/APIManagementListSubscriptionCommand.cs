using AzureOpsCLI.Interfaces;
using Spectre.Console.Cli;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Commands.apim
{
    public class APIManagementListSubscriptionCommand : AsyncCommand
    {
        private readonly IAPIManagementService _apiManagementService;
        private readonly ISubscritionService _subscriptionService;

        public APIManagementListSubscriptionCommand(IAPIManagementService apiManagementService, ISubscritionService subscriptionService)
        {
            _apiManagementService = apiManagementService;
            _subscriptionService = subscriptionService;

        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();

                if (subscriptionChoices == null || !subscriptionChoices.Any())
                {
                    AnsiConsole.MarkupLine("[red]No subscriptions available or unable to fetch subscriptions.[/]");
                    return -1;
                }

                var selectedSubscription = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]subscription[/]:")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                        .AddChoices(subscriptionChoices));

                string subscriptionName = selectedSubscription.Split('(').Last().TrimEnd(')');

                var resources = await _apiManagementService.FetchAPIMInSubscriptionsAsync(subscriptionName);

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
