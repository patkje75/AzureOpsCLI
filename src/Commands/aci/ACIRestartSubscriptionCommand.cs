using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vm
{

    public class ACIRestartSubscriptionCommand : AsyncCommand
    {
        private readonly IACIService _aciService;
        private readonly ISubscritionService _subscriptionService;

        public ACIRestartSubscriptionCommand(IACIService aciService, ISubscritionService subscritionService)
        {
            _aciService = aciService;
            _subscriptionService = subscritionService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();
            var selectedSubscription = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]subscription[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                    .AddChoices(subscriptionChoices));

            string subscriptionId = selectedSubscription.Split('(').Last().TrimEnd(')');

            var runningACIs = await _aciService.FetchRunningACIInSubscriptionAsync(subscriptionId);

            if (!runningACIs.Any())
            {
                AnsiConsole.MarkupLine("[red]No running Container Instance found in the selected subscription.[/]");
                return 0;
            }

            var aciNames = runningACIs.Select(aci => $"{aci.ContainerGroup.Data.Name} in {aci.ContainerGroup.Data.Location} (Subscription {aci.SubscriptionName})").ToList();

            var selectedVMs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Container Instance to restart:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more container instances)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a Container Instances, [green]<enter>[/] to restart selected Container Instances)[/]")
                    .AddChoices(aciNames));

            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
            new TaskDescriptionColumn(),
            new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var tasks = selectedVMs.Select(vmName =>
                    {
                        var aciExtended = runningACIs.First(c => $"{c.ContainerGroup.Data.Name} in {c.ContainerGroup.Data.Location} (Subscription {c.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Restarting {aciExtended.ContainerGroup.Data.Name}");

                        return _aciService.RestartACIAsync(aciExtended.ContainerGroup)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Restart was [green]successful[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to restart [blue]{aciExtended.ContainerGroup.Data.Name}[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                            });
                    }).ToList();

                    await Task.WhenAll(tasks);
                });


            return 0;
        }
    }
}
