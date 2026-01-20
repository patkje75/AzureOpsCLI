using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;

namespace AzureOpsCLI.Services
{
    public class RGService : IRGService
    {
        private readonly ArmClient _armClient;

        public RGService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<ResourceGroupExtended>> FetchAllResourceGroupsAsync(string? filter = null)
        {
            List<ResourceGroupExtended> rgs = new List<ResourceGroupExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching resource groups in {subscription.Data.DisplayName}...");
                            await foreach (var rg in subscription.GetResourceGroups().GetAllAsync())
                            {
                                if (string.IsNullOrEmpty(filter) || rg.Data.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                {
                                    rgs.Add(new ResourceGroupExtended
                                    {
                                        ResourceGroup = rg,
                                        SubscriptionName = subscription.Data.DisplayName
                                    });
                                }
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching resource groups: {ex.Message}[/]");
            }
            return rgs;
        }

        public async Task<List<ResourceGroupExtended>> FetchResourceGroupsBySubscriptionAsync(string subscriptionId, string? filter = null)
        {
            List<ResourceGroupExtended> rgs = new List<ResourceGroupExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);
                await AnsiConsole.Status()
                    .StartAsync($"Fetching resource groups in subscription {subscription.Data.DisplayName}...", async ctx =>
                    {
                        await foreach (var rg in subscription.GetResourceGroups().GetAllAsync())
                        {
                            if (string.IsNullOrEmpty(filter) || rg.Data.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                            {
                                rgs.Add(new ResourceGroupExtended
                                {
                                    ResourceGroup = rg,
                                    SubscriptionName = subscription.Data.DisplayName
                                });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching resource groups: {ex.Message}[/]");
            }
            return rgs;
        }

        public async Task<bool> CreateResourceGroupAsync(string subscriptionId, string resourceGroupName, string location, Dictionary<string, string>? tags = null)
        {
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);
                ResourceGroupData rgData = new ResourceGroupData(location);
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        rgData.Tags.Add(tag.Key, tag.Value);
                    }
                }
                await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, rgData);
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to create resource group: {ex.Message}[/]");
                return false;
            }
        }
    }
}
