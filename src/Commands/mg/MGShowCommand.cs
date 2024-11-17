using Azure.ResourceManager.ManagementGroups.Models;
using AzureOpsCLI.Commands.aci;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Commands.mg
{

    public class MGShowCommand : AsyncCommand<MGShowCommand.Settings>
    {
        private readonly IMGService _mgService;

        public MGShowCommand(IMGService mgService)
        {
            _mgService = mgService;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("-c|--console")]
            public bool ConsoleOutput { get; set; }

            [CommandOption("-e|--exportmermaid <FILE_PATH>")]
            public string ExportMermaid { get; set; }

        }


        public override async Task<int> ExecuteAsync(CommandContext context, MGShowCommand.Settings settings)
        {

            if (!settings.ConsoleOutput && string.IsNullOrEmpty(settings.ExportMermaid))
            {
                AnsiConsole.MarkupLine("[red]Error: You must specify either --console|-c or --exportmermaid <FILE_PATH>|-e <FILE_PATH>.[/]");
                return 1;
            }


            var managementGroupStructure = await _mgService.FetchManagementGroupsAsync();
            var rootTree = new Tree("[green]Management Groups[/]");

            var groupLookup = managementGroupStructure.ToDictionary(mg => mg.DisplayName, mg => mg);

            var rootGroup = managementGroupStructure.FirstOrDefault(mg => mg.DisplayName == "Tenant Root Group");

            if (rootGroup == null)
            {
                AnsiConsole.MarkupLine("[red]Error[/]: Root management group (Tenant Root Group) not found.");
                return -1;
            }

            var rootNode = rootTree.AddNode($"[blue]{rootGroup.DisplayName}[/]");

            AddChildrenToTree(rootNode, rootGroup, groupLookup);

            if (!string.IsNullOrEmpty(settings.ExportMermaid))
            {
                var mermaid = GenerateMermaidDiagram(rootGroup, managementGroupStructure);
                var mermaidFile = "management_groups.mmd";
                var filePath = Path.Combine(settings.ExportMermaid, mermaidFile);

                try
                {
                    await File.WriteAllTextAsync(filePath, mermaid);
                    AnsiConsole.MarkupLine($"[green]Mermaid diagram exported to:[/] {filePath}");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error[/]: {ex.Message}");
                    return -1;
                }            
            }
            else if (settings.ConsoleOutput)
            {
                AnsiConsole.Write(rootTree);
            }

            return 0;
        }

        private void AddChildrenToTree(TreeNode parentNode, ManagementGroupExtended currentGroup, Dictionary<string, ManagementGroupExtended> groupLookup)
        {

            if (currentGroup == null || currentGroup.Children == null)
            {
                AnsiConsole.MarkupLine($"[yellow]CurrentGroup or Children is null for {currentGroup?.DisplayName}[/]");
                return;
            }

            foreach (var child in currentGroup.Children)
            {
                if (child == null || string.IsNullOrEmpty(child.DisplayName))
                {

                    AnsiConsole.MarkupLine($"[yellow]Skipping child with null or empty DisplayName under {currentGroup.DisplayName}[/]");
                    continue;
                }

                if (groupLookup.TryGetValue(child.DisplayName, out var childGroup))
                {
                    var childNode = parentNode.AddNode($"{Emoji.Known.WhiteMediumSmallSquare} [green]{childGroup.DisplayName}[/]");

                    AddChildrenToTree(childNode, childGroup, groupLookup);
                }
                else
                {
                    parentNode.AddNode($"{Emoji.Known.Key} [yellow]{child.DisplayName}[/]");
                }
            }
        }

        private string GenerateMermaidDiagram(ManagementGroupExtended rootGroup, List<ManagementGroupExtended> allGroups)
        {
            var builder = new StringBuilder();
            builder.AppendLine("graph TD");

            void BuildMermaidNode(ManagementGroupExtended group)
            {
                foreach (var subscription in group.Children.Where(c => c.ChildType == "/Subscription"))
                {
                    builder.AppendLine($"    {SanitizeMermaidId(group.DisplayName)} --> {SanitizeMermaidId(subscription.DisplayName)}[\"{subscription.DisplayName}\"]");
                }

                foreach (var child in allGroups.Where(mg => mg.Parent == group.DisplayName))
                {
                    builder.AppendLine($"    {SanitizeMermaidId(group.DisplayName)} --> {SanitizeMermaidId(child.DisplayName)}[\"{child.DisplayName}\"]");
                    BuildMermaidNode(child);
                }
            }

            builder.AppendLine($"    {SanitizeMermaidId(rootGroup.DisplayName)}[\"{rootGroup.DisplayName}\"]");
            BuildMermaidNode(rootGroup);

            return builder.ToString();
        }

        private string SanitizeMermaidId(string id)
        {
            return id.Replace(" ", "_").Replace("-", "_").Replace(".", "_");
        }


    }
}
