using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.aci
{

    public class ACIDeleteAllCommand : AsyncCommand
    {
        private readonly IACIService _aciService;

        public ACIDeleteAllCommand(IACIService aciService)
        {
            _aciService = aciService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var allACIs = await _aciService.FetchAllACIAsync();

            if (!allACIs.Any())
            {
                AnsiConsole.MarkupLine("[red]No Container Instances found.[/]");
                return 0;
            }

            var aciNames = allACIs.Select(aci => $"{aci.ContainerGroup.Data.Name} in {aci.ContainerGroup.Data.Location} (Subscription {aci.SubscriptionName})").ToList();

            var selectedACIs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select container instance to delete:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more container instances)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a container instance, [green]<enter>[/] to delete selected container instance)[/]")
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
                        var aciExtended = allACIs.First(a => $"{a.ContainerGroup.Data.Name} in {a.ContainerGroup.Data.Location} (Subscription {a.SubscriptionName})" == aciName);
                        var task = ctx.AddTask($"Deleting {aciExtended.ContainerGroup.Data.Name}");

                        return _aciService.DeleteACIAsync(aciExtended.ContainerGroup)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Delete was [green]successful[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to delete [blue]{aciExtended.ContainerGroup.Data.Name}[/]: {t.Result.Message}";
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
