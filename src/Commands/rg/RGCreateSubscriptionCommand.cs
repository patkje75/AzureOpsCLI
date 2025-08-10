using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.rg
{
    public class RGCreateSubscriptionCommand : AsyncCommand<RGCreateSubscriptionCommand.Settings>
    {
        private readonly IRGService _rgService;
        private readonly ISubscritionService _subscriptionService;

        public RGCreateSubscriptionCommand(IRGService rgService, ISubscritionService subscriptionService)
        {
            _rgService = rgService;
            _subscriptionService = subscriptionService;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("-n|--name <NAME>")]
            public string? Name { get; set; }

            [CommandOption("-l|--location <LOCATION>")]
            public string? Location { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (string.IsNullOrEmpty(settings.Name) || string.IsNullOrEmpty(settings.Location))
            {
                AnsiConsole.MarkupLine("[red]You must specify --name and --location.[/]");
                return 1;
            }

            var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();
            var selectedSubscription = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]subscription[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                    .AddChoices(subscriptionChoices));

            string subscriptionId = selectedSubscription.Split('(').Last().TrimEnd(')');
            var result = await _rgService.CreateResourceGroupAsync(subscriptionId, settings.Name, settings.Location);
            if (result)
            {
                AnsiConsole.MarkupLine($"[green]Resource group {settings.Name} created in {settings.Location}.[/]");
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }
}
