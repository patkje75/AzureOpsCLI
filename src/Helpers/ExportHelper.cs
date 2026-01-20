using System.Text.Json;
using Spectre.Console;

namespace AzureOpsCLI.Helpers
{
    public static class ExportHelper
    {
        public static async Task ExportToCsvAsync<T>(IEnumerable<T> data, string filename, Func<T, string[]> rowSelector, string[] headers)
        {
            var lines = new List<string> { string.Join(",", headers) };
            lines.AddRange(data.Select(item => string.Join(",", rowSelector(item).Select(EscapeCsvField))));
            await File.WriteAllLinesAsync(filename, lines);
        }

        public static async Task ExportToJsonAsync<T>(IEnumerable<T> data, string filename, Func<T, object> objectSelector)
        {
            var objects = data.Select(objectSelector).ToList();
            var json = JsonSerializer.Serialize(objects, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filename, json);
        }

        public static async Task<bool> ExportDataAsync<T>(
            IEnumerable<T> data,
            string? exportFormat,
            string resourceType,
            Func<T, string[]> csvRowSelector,
            string[] csvHeaders,
            Func<T, object> jsonObjectSelector)
        {
            if (string.IsNullOrEmpty(exportFormat))
                return false;

            var format = exportFormat.ToLower();
            if (format != "csv" && format != "json")
            {
                AnsiConsole.MarkupLine($"[red]Invalid export format '{exportFormat}'. Use 'csv' or 'json'.[/]");
                return false;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"{resourceType}_{timestamp}.{format}";

            try
            {
                if (format == "csv")
                {
                    await ExportToCsvAsync(data, filename, csvRowSelector, csvHeaders);
                }
                else
                {
                    await ExportToJsonAsync(data, filename, jsonObjectSelector);
                }

                AnsiConsole.MarkupLine($"[green]Exported to {filename}[/]");
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to export: {ex.Message}[/]");
                return false;
            }
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return field;

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
