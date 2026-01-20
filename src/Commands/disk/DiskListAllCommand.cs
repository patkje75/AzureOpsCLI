using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Settings;
using AzureOpsCLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.disk
{
    public class DiskListAllCommand : AsyncCommand<ListCommandSettings>
    {
        private readonly IDiskService _diskService;

        public DiskListAllCommand(IDiskService diskService)
        {
            _diskService = diskService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, ListCommandSettings settings)
        {
            try
            {
                var disks = await AnsiConsole.Status()
                    .StartAsync("Fetching managed disks...", async ctx =>
                    {
                        return await _diskService.FetchAllDisksAsync();
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
                    table.AddColumn(new TableColumn("[bold]Attached To[/]").Width(25));

                    foreach (var disk in disks)
                    {
                        var statusColor = disk.IsAttached ? "green" : "yellow";
                        var status = disk.IsAttached ? "Attached" : "Unattached";

                        table.AddRow(
                            $"[blue]{disk.Disk.Data.Name}[/]",
                            disk.SubscriptionName,
                            disk.Disk.Data.Location.ToString(),
                            disk.Disk.Data.DiskSizeGB?.ToString() ?? "N/A",
                            disk.Disk.Data.Sku?.Name?.ToString() ?? "N/A",
                            $"[{statusColor}]{status}[/]",
                            disk.AttachedTo
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\nTotal: [blue]{disks.Count}[/] disks, [yellow]{disks.Count(d => !d.IsAttached)}[/] unattached");

                    // Export if requested
                    if (!string.IsNullOrEmpty(settings.ExportFormat))
                    {
                        await ExportHelper.ExportDataAsync(
                            disks,
                            settings.ExportFormat,
                            "managed-disks-all",
                            d => new[] { d.Disk.Data.Name, d.SubscriptionName, d.Disk.Data.Location.ToString(), d.Disk.Data.DiskSizeGB?.ToString() ?? "N/A", d.IsAttached ? "Attached" : "Unattached", d.AttachedTo },
                            new[] { "Name", "Subscription", "Location", "SizeGB", "Status", "AttachedTo" },
                            d => new { Name = d.Disk.Data.Name, Subscription = d.SubscriptionName, Location = d.Disk.Data.Location.ToString(), SizeGB = d.Disk.Data.DiskSizeGB, IsAttached = d.IsAttached, AttachedTo = d.AttachedTo }
                        );
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No managed disks found.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to fetch disks: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }
    }
}
