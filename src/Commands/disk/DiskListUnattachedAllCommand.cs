using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Settings;
using AzureOpsCLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.disk
{
    public class DiskListUnattachedAllCommand : AsyncCommand<ListCommandSettings>
    {
        private readonly IDiskService _diskService;

        public DiskListUnattachedAllCommand(IDiskService diskService)
        {
            _diskService = diskService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, ListCommandSettings settings)
        {
            try
            {
                var disks = await AnsiConsole.Status()
                    .StartAsync("Fetching unattached managed disks...", async ctx =>
                    {
                        return await _diskService.FetchUnattachedDisksAsync();
                    });

                // Apply filter if specified
                if (!string.IsNullOrEmpty(settings.Filter))
                {
                    disks = disks.Where(d => d.Disk.Data.Name.Contains(settings.Filter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (disks != null && disks.Any())
                {
                    var table = new Table();
                    table.AddColumn(new TableColumn("[bold]Name[/]").Width(30));
                    table.AddColumn(new TableColumn("[bold]Subscription[/]").Width(25));
                    table.AddColumn(new TableColumn("[bold]Location[/]").Width(15));
                    table.AddColumn(new TableColumn("[bold]Size (GB)[/]").Width(10));
                    table.AddColumn(new TableColumn("[bold]Type[/]").Width(15));
                    table.AddColumn(new TableColumn("[bold]Status[/]").Width(12));

                    foreach (var disk in disks)
                    {
                        table.AddRow(
                            $"[blue]{disk.Disk.Data.Name}[/]",
                            disk.SubscriptionName,
                            disk.Disk.Data.Location.ToString(),
                            disk.Disk.Data.DiskSizeGB?.ToString() ?? "N/A",
                            disk.Disk.Data.Sku?.Name?.ToString() ?? "N/A",
                            "[yellow]Unattached[/]"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\nTotal: [yellow]{disks.Count}[/] unattached disks");

                    // Export if requested
                    if (!string.IsNullOrEmpty(settings.ExportFormat))
                    {
                        await ExportHelper.ExportDataAsync(
                            disks,
                            settings.ExportFormat,
                            "unattached-disks-all",
                            d => new[] { d.Disk.Data.Name, d.SubscriptionName, d.Disk.Data.Location.ToString(), d.Disk.Data.DiskSizeGB?.ToString() ?? "N/A", d.Disk.Data.Sku?.Name?.ToString() ?? "N/A" },
                            new[] { "Name", "Subscription", "Location", "SizeGB", "Type" },
                            d => new { Name = d.Disk.Data.Name, Subscription = d.SubscriptionName, Location = d.Disk.Data.Location.ToString(), SizeGB = d.Disk.Data.DiskSizeGB, Type = d.Disk.Data.Sku?.Name?.ToString() }
                        );
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]No unattached managed disks found.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to fetch unattached disks: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
