using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.aci
{

    public class ACIStopAllCommand : AsyncCommand
    {
        private readonly IACIService _aciService;

        public ACIStopAllCommand(IACIService aciService)
        {
            _aciService = aciService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var runningACIs = await _aciService.FetchRunningACIAsync();

            if (!runningACIs.Any())
            {
                AnsiConsole.MarkupLine("[red]No running Container Instances found.[/]");
                return 0;
            }

            var aciNames = runningACIs.Select(aci => $"{aci.ContainerGroup.Data.Name} in {aci.ContainerGroup.Data.Location} (Subscription {aci.SubscriptionName})").ToList();

            var selectedACIs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select container instance to stop:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more container instances)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a container instance, [green]<enter>[/] to start selected container instance)[/]")
                    .AddChoices(aciNames));

            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
            new TaskDescriptionColumn(),
            new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var tasks = selectedACIs.Select(aciName =>
                    {
                        var aciExtended = runningACIs.First(a => $"{a.ContainerGroup.Data.Name} in {a.ContainerGroup.Data.Location} (Subscription {a.SubscriptionName})" == aciName);
                        var task = ctx.AddTask($"Stopping {aciExtended.ContainerGroup.Data.Name}");

                        return _aciService.StopACIAsync(aciExtended.ContainerGroup)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Stop was [green]successful[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to stop [blue]{aciExtended.ContainerGroup.Data.Name}[/]: {t.Result.Message}";
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
