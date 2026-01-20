using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Settings;
using AzureOpsCLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.metrics
{
    public class MetricsVMAllCommand : AsyncCommand<ListCommandSettings>
    {
        private readonly IMetricsService _metricsService;

        public MetricsVMAllCommand(IMetricsService metricsService)
        {
            _metricsService = metricsService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, ListCommandSettings settings)
        {
            try
            {
                var metrics = await AnsiConsole.Status()
                    .StartAsync("Fetching VM metrics...", async ctx =>
                    {
                        return await _metricsService.FetchAllVMMetricsAsync();
                    });

                // Apply filter if specified
                if (!string.IsNullOrEmpty(settings.Filter))
                {
                    metrics = metrics.Where(m => m.Name.Contains(settings.Filter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (metrics != null && metrics.Any())
                {
                    var table = new Table();
                    table.AddColumn(new TableColumn("[bold]Name[/]").Width(25));
                    table.AddColumn(new TableColumn("[bold]Subscription[/]").Width(20));
                    table.AddColumn(new TableColumn("[bold]Location[/]").Width(15));
                    table.AddColumn(new TableColumn("[bold]CPU %[/]").Width(10));
                    table.AddColumn(new TableColumn("[bold]Memory[/]").Width(12));
                    table.AddColumn(new TableColumn("[bold]Disk R[/]").Width(12));
                    table.AddColumn(new TableColumn("[bold]Disk W[/]").Width(12));
                    table.AddColumn(new TableColumn("[bold]Net In[/]").Width(12));
                    table.AddColumn(new TableColumn("[bold]Net Out[/]").Width(12));

                    foreach (var m in metrics)
                    {
                        var cpuColor = GetCpuColor(m.CpuPercentage);
                        table.AddRow(
                            $"[blue]{m.Name}[/]",
                            m.SubscriptionName,
                            m.Location,
                            $"[{cpuColor}]{FormatDouble(m.CpuPercentage)}[/]",
                            FormatBytes(m.MemoryPercentage),
                            FormatBytes(m.DiskReadBytesPerSec),
                            FormatBytes(m.DiskWriteBytesPerSec),
                            FormatBytes(m.NetworkInBytesPerSec),
                            FormatBytes(m.NetworkOutBytesPerSec)
                        );
                    }

                    AnsiConsole.Write(table);

                    // Export if requested
                    if (!string.IsNullOrEmpty(settings.ExportFormat))
                    {
                        await ExportHelper.ExportDataAsync(
                            metrics,
                            settings.ExportFormat,
                            "vm-metrics-all",
                            m => new[] { m.Name, m.SubscriptionName, m.Location, FormatDouble(m.CpuPercentage), FormatBytes(m.MemoryPercentage), FormatBytes(m.DiskReadBytesPerSec) },
                            new[] { "Name", "Subscription", "Location", "CPU%", "Memory", "DiskRead" },
                            m => new { m.Name, m.SubscriptionName, m.Location, m.CpuPercentage, m.MemoryPercentage, m.DiskReadBytesPerSec, m.DiskWriteBytesPerSec }
                        );
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No VM metrics found.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to fetch metrics: {ex.Message}[/]");
                return -1;
            }

            return 0;
        }

        private static string FormatDouble(double? value) => value.HasValue ? $"{value:F1}" : "N/A";

        private static string FormatBytes(double? bytes)
        {
            if (!bytes.HasValue) return "N/A";
            var kb = bytes.Value / 1024;
            if (kb < 1024) return $"{kb:F1} KB";
            var mb = kb / 1024;
            return $"{mb:F1} MB";
        }

        private static string GetCpuColor(double? cpu)
        {
            if (!cpu.HasValue) return "grey";
            if (cpu < 50) return "green";
            if (cpu < 80) return "yellow";
            return "red";
        }
    }
}
