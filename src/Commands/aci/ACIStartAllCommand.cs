using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.aci
{

    public class ACIStartAllCommand : AsyncCommand
    {
        private readonly IACIService _aciService;

        public ACIStartAllCommand(IACIService aciService)
        {
            _aciService = aciService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var stoppedACIs = await _aciService.FetchStoppedACIAsync();

            if (!stoppedACIs.Any())
            {
                AnsiConsole.MarkupLine("[red]No stopped Container Instances found.[/]");
                return 0;
            }

            var aciNames = stoppedACIs.Select(aci => $"{aci.ContainerGroup.Data.Name} in {aci.ContainerGroup.Data.Location} (Subscription {aci.SubscriptionName})").ToList();

            var selectedACIs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select contaioner instance to start:")
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
                        var aciExtended = stoppedACIs.First(a => $"{a.ContainerGroup.Data.Name} in {a.ContainerGroup.Data.Location} (Subscription {a.SubscriptionName})" == aciName);
                        var task = ctx.AddTask($"Starting {aciExtended.ContainerGroup.Data.Name}");

                        return _aciService.StartACIAsync(aciExtended.ContainerGroup)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Start was [green]successfull[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to start [blue]{aciExtended.ContainerGroup.Data.Name}[/]: {t.Result.Message}";
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
