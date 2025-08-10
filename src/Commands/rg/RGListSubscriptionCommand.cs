using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.rg
{
    public class RGListSubscriptionCommand : AsyncCommand<RGListSubscriptionCommand.Settings>
    {
        private readonly IRGService _rgService;
        private readonly ISubscritionService _subscriptionService;

        public RGListSubscriptionCommand(IRGService rgService, ISubscritionService subscriptionService)
        {
            _rgService = rgService;
            _subscriptionService = subscriptionService;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("-f|--filter <FILTER>")]
            public string? Filter { get; set; }

            [CommandOption("-e|--export <FILE_PATH>")]
            public string? ExportPath { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();
            var selectedSubscription = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]subscription[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                    .AddChoices(subscriptionChoices));

            string subscriptionId = selectedSubscription.Split('(').Last().TrimEnd(')');
            var rgs = await _rgService.FetchResourceGroupsBySubscriptionAsync(subscriptionId, settings.Filter);
            if (rgs.Any())
            {
                var grid = new Grid();
                grid.AddColumn(new GridColumn().Width(35));
                grid.AddColumn(new GridColumn().Width(20));
                grid.AddColumn(new GridColumn().Width(30));
                grid.AddRow("[bold darkgreen]Resource Group[/]", "[bold darkgreen]Location[/]", "[bold darkgreen]Subscription[/]");

                foreach (var rg in rgs)
                {
                    grid.AddRow($"[blue]{rg.ResourceGroup.Data.Name}[/]", $"[yellow]{rg.ResourceGroup.Data.Location}[/]", $"[yellow]{rg.SubscriptionName}[/]");
                }

                AnsiConsole.Write(grid);

                if (!string.IsNullOrEmpty(settings.ExportPath))
                {
                    try
                    {
                        var lines = new List<string> { "Name,Location,Subscription" };
                        lines.AddRange(rgs.Select(r => $"{r.ResourceGroup.Data.Name},{r.ResourceGroup.Data.Location},{r.SubscriptionName}"));
                        File.WriteAllLines(settings.ExportPath, lines);
                        AnsiConsole.MarkupLine($"[green]Exported to {settings.ExportPath}[/]");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Failed to export: {ex.Message}[/]");
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]No resource groups found.[/]");
            }
            return 0;
        }
    }
}
