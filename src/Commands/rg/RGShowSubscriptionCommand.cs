using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.rg
{
    public class RGShowSubscriptionCommand : AsyncCommand<RGShowSubscriptionCommand.Settings>
    {
        private readonly IRGService _rgService;
        private readonly ISubscritionService _subscriptionService;

        public RGShowSubscriptionCommand(IRGService rgService, ISubscritionService subscriptionService)
        {
            _rgService = rgService;
            _subscriptionService = subscriptionService;
        }

        public class Settings : CommandSettings
        {
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
            var rgs = await _rgService.FetchResourceGroupsBySubscriptionAsync(subscriptionId);
            if (!rgs.Any())
            {
                AnsiConsole.MarkupLine("[red]No resource groups found.[/]");
                return -1;
            }

            var rgName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]resource group[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more resource groups)[/]")
                    .AddChoices(rgs.Select(r => r.ResourceGroup.Data.Name)));

            var rg = await _rgService.GetResourceGroupAsync(subscriptionId, rgName);
            if (rg != null)
            {
                var grid = new Grid();
                grid.AddColumn(new GridColumn().NoWrap());
                grid.AddColumn(new GridColumn().NoWrap());
                grid.AddRow("[bold darkgreen]Name[/]", $"[blue]{rg.ResourceGroup.Data.Name}[/]");
                grid.AddRow("[bold darkgreen]Location[/]", $"[yellow]{rg.ResourceGroup.Data.Location}[/]");
                grid.AddRow("[bold darkgreen]Subscription[/]", $"[yellow]{rg.SubscriptionName}[/]");
                if (rg.ResourceGroup.Data.Tags != null && rg.ResourceGroup.Data.Tags.Any())
                {
                    foreach (var tag in rg.ResourceGroup.Data.Tags)
                    {
                        grid.AddRow($"[bold darkgreen]Tag: {tag.Key}[/]", tag.Value);
                    }
                }
                AnsiConsole.Write(grid);
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Resource group not found.[/]");
                return -1;
            }
        }
    }
}
