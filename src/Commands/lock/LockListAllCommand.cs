using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Settings;
using AzureOpsCLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.resourcelock
{
    public class LockListAllCommand : AsyncCommand<ListCommandSettings>
    {
        private readonly ILockService _lockService;

        public LockListAllCommand(ILockService lockService)
        {
            _lockService = lockService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, ListCommandSettings settings)
        {
            try
            {
                var locks = await AnsiConsole.Status()
                    .StartAsync("Fetching resource locks...", async ctx =>
                    {
                        return await _lockService.FetchAllLocksAsync();
                    });

                // Apply filter if specified
                if (!string.IsNullOrEmpty(settings.Filter))
                {
                    locks = locks.Where(l => l.Lock.Data.Name.Contains(settings.Filter, StringComparison.OrdinalIgnoreCase) ||
                                             l.ResourceName.Contains(settings.Filter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (locks != null && locks.Any())
                {
                    var table = new Table();
                    table.AddColumn(new TableColumn("[bold]Lock Name[/]").Width(25));
                    table.AddColumn(new TableColumn("[bold]Level[/]").Width(15));
                    table.AddColumn(new TableColumn("[bold]Resource[/]").Width(30));
                    table.AddColumn(new TableColumn("[bold]Type[/]").Width(20));
                    table.AddColumn(new TableColumn("[bold]Subscription[/]").Width(25));

                    foreach (var lockItem in locks)
                    {
                        var levelColor = lockItem.Lock.Data.Level == Azure.ResourceManager.Resources.Models.ManagementLockLevel.ReadOnly ? "red" : "yellow";
                        table.AddRow(
                            $"[blue]{lockItem.Lock.Data.Name}[/]",
                            $"[{levelColor}]{lockItem.Lock.Data.Level}[/]",
                            lockItem.ResourceName,
                            lockItem.ResourceType,
                            lockItem.SubscriptionName
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\nTotal: [blue]{locks.Count}[/] lock(s)");

                    // Export if requested
                    if (!string.IsNullOrEmpty(settings.ExportFormat))
                    {
                        await ExportHelper.ExportDataAsync(
                            locks,
                            settings.ExportFormat,
                            "resource-locks-all",
                            l => new[] { l.Lock.Data.Name, l.Lock.Data.Level.ToString(), l.ResourceName, l.ResourceType, l.SubscriptionName },
                            new[] { "LockName", "Level", "Resource", "Type", "Subscription" },
                            l => new { LockName = l.Lock.Data.Name, Level = l.Lock.Data.Level.ToString(), Resource = l.ResourceName, Type = l.ResourceType, Subscription = l.SubscriptionName }
                        );
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No resource locks found.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to fetch locks: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
