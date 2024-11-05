using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.aci
{
    public class ACIListAllCommand : AsyncCommand
    {
        private readonly IACIService _aciService;

        public ACIListAllCommand(IACIService aciService)
        {
            _aciService = aciService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var containerGroups = await _aciService.FetchAllACIAsync();

                if (containerGroups != null && containerGroups.Any())
                {
                    var grid = new Grid();

                    grid.AddColumn(new GridColumn().Width(35));
                    grid.AddColumn(new GridColumn().Width(15));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(10));

                    grid.AddRow(
                        "[bold darkgreen]Container Group Name[/]",
                        "[bold darkgreen]Location[/]",
                        "[bold darkgreen]Subscription[/]",
                        "[bold darkgreen]Status[/]"
                    );

                    foreach (var ci in containerGroups)
                    {
                        var statusColors = new Dictionary<string, string>
                        {
                            { "running", "green" },
                            { "waiting", "darkorange" }
                        };

                        string statusColor = statusColors.ContainsKey(ci.Status) ? statusColors[ci.Status] : "red";

                        grid.AddRow(
                            $"[blue]{ci.ContainerGroup.Data.Name}[/]",
                            $"[yellow]{ci.ContainerGroup.Data.Location}[/]",
                            $"[yellow]{ci.SubscriptionName}[/]",
                            $"[{statusColor}]{ci.Status}[/]"
                        );
                    }

                    AnsiConsole.Write(grid);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]No container instances found.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
