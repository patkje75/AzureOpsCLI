using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.rg
{
    public class RGDeleteSubscriptionCommand : AsyncCommand<RGDeleteSubscriptionCommand.Settings>
    {
        private readonly IRGService _rgService;
        private readonly ISubscritionService _subscriptionService;

        public RGDeleteSubscriptionCommand(IRGService rgService, ISubscritionService subscriptionService)
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
                    .Title("Select a [green]resource group[/] to delete:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more resource groups)[/]")
                    .AddChoices(rgs.Select(r => r.ResourceGroup.Data.Name)));

            if (!AnsiConsole.Confirm($"Are you sure you want to delete [yellow]{rgName}[/]?"))
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                return 0;
            }

            var result = await _rgService.DeleteResourceGroupAsync(subscriptionId, rgName);
            if (result)
            {
                AnsiConsole.MarkupLine($"[green]Deleted resource group {rgName}.[/]");
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }
}
