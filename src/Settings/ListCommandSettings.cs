using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Settings
{
    public class ListCommandSettings : CommandSettings
    {
        [CommandOption("-f|--filter <FILTER>")]
        [Description("Filter results by name (case-insensitive contains match)")]
        public string? Filter { get; set; }

        [CommandOption("-e|--export <FORMAT>")]
        [Description("Export results to file (csv or json)")]
        public string? ExportFormat { get; set; }
    }
}
