using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ManagementGroups.Models;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Services
{
    public class MGService : IMGService
    {
        private ArmClient _armClient;
        public MGService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<ManagementGroupExtended>> FetchManagementGroupsAsync()
        {
            List<ManagementGroupExtended> managementGroup = new List<ManagementGroupExtended>();

            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching management groups...", async ctx =>
                    {
                        var mgs = _armClient.GetManagementGroups().GetAllAsync();

                        await foreach (var mg in mgs)
                        {
                            try
                            {
                                ctx.Status($"Processing management group: [blue]{mg.Data.DisplayName}[/]");

                                var mgr = await mg.GetAsync(ManagementGroupExpandType.Children, true);

                                ManagementGroupExtended mgmGrpExt = new ManagementGroupExtended
                                {
                                    DisplayName = mg.Data.DisplayName ?? "Unknown",
                                    Parent = mgr.Value.Data.Details.Parent?.DisplayName ?? "None",
                                    Children = new List<ManagementGroupChildInfo>()
                                };

                                if (mgr.Value.Data.Children != null)
                                {
                                    foreach (var child in mgr.Value.Data.Children)
                                    {
                                        mgmGrpExt.Children.Add(child);
                                    }
                                }

                                managementGroup.Add(mgmGrpExt);
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error processing management group '{mg.Data.DisplayName}': {ex.Message}[/]");
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching management groups: {ex.Message}[/]");
                return new List<ManagementGroupExtended>();
            }

            return managementGroup;
        }
    }
}
