using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ManagementGroups.Models;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;

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
                            catch (RequestFailedException ex)
                            {
                                string cleanErrorMessage = ex.ErrorCode switch
                                {
                                    "AuthorizationFailed" => "Authorization failed. Ensure you have permission to access management groups.",
                                    "ResourceNotFound" => "The specified management group could not be found.",
                                    _ => ex.Message.Split("\r\n").FirstOrDefault()?.Trim() ?? ex.Message
                                };

                                AnsiConsole.MarkupLine($"[red]Error processing management group '{mg.Data.DisplayName}': {cleanErrorMessage}[/]");
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Unexpected error processing management group '{mg.Data.DisplayName}': {ex.Message}[/]");
                            }
                        }
                    });
            }
            catch (RequestFailedException ex)
            {
                string cleanErrorMessage = ex.ErrorCode switch
                {
                    "AuthorizationFailed" => "Authorization failed. Ensure you have permission to read management groups.",
                    _ => ex.Message.Split("\r\n").FirstOrDefault()?.Trim() ?? ex.Message
                };

                AnsiConsole.MarkupLine($"[red]Error fetching management groups: {cleanErrorMessage}[/]");
                return new List<ManagementGroupExtended>();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Unexpected error fetching management groups: {ex.Message}[/]");
                return new List<ManagementGroupExtended>();
            }

            return managementGroup;
        }
    }
}
