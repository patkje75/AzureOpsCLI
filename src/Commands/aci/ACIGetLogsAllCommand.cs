using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.aci
{
    public class ACIGetLogsAllCommand : AsyncCommand<ACIGetLogsAllCommand.Settings>
    {
        private readonly IACIService _aciService;

        public ACIGetLogsAllCommand(IACIService aciService)
        {
            _aciService = aciService;
        }
        public class Settings : CommandSettings
        {
            [CommandOption("-c|--console")]
            public bool ConsoleOutput { get; set; }

            [CommandOption("-f|--file <FILE_PATH>")]
            public string FilePath { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {

            if (!settings.ConsoleOutput && string.IsNullOrEmpty(settings.FilePath))
            {
                AnsiConsole.MarkupLine("[red]Error: You must specify either --console|-c or --file <FILE_PATH>|-f <FILE_PATH>.[/]");
                return 1;
            }

            var allACIs = await _aciService.FetchAllACIAsync();
            if (!allACIs.Any())
            {
                AnsiConsole.MarkupLine("[red]No Container Instances found.[/]");
                return 0;
            }

            var aciNames = allACIs.Select(aci => $"{aci.ContainerGroup.Data.Name} in {aci.ContainerGroup.Data.Location} (Subscription {aci.SubscriptionName})").ToList();
            var selectedACIs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select container instance to get logs from:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more container instances)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a container instance, [green]<enter>[/] to to get logs from selected container instance)[/]")
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
                        var task = ctx.AddTask($"Fetching logs from {aciExtended.ContainerGroup.Data.Name}");

                        return _aciService.GetContainerLogsAsync(aciExtended.ContainerGroup)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    var logMessage = t.Result.Message;

                                    if (settings.ConsoleOutput)
                                    {
                                        AnsiConsole.MarkupLine($"[green]Logs from container {aciExtended.ContainerGroup.Data.Name}:[/]");
                                        AnsiConsole.WriteLine(logMessage);
                                    }
                                    else if (!string.IsNullOrEmpty(settings.FilePath))
                                    {
                                        var filePath = Path.Combine(settings.FilePath, $"{aciExtended.ContainerGroup.Data.Name}_logs.txt");
                                        File.WriteAllText(filePath, logMessage);
                                        AnsiConsole.MarkupLine($"[green]Logs written to file:[/] {filePath}");
                                    }                                    
                                    task.Description = $"Fetching logs was [green]successful[/] for {aciExtended.ContainerGroup.Data.Name}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to fetch logs for [blue]{aciExtended.ContainerGroup.Data.Name}[/]: {t.Result.Message}";
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
