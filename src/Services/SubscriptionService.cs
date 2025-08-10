using Azure.Identity;
using Azure.ResourceManager;
using AzureOpsCLI.Interfaces;
using Spectre.Console;

namespace AzureOpsCLI.Services
{
    public class SubscriptionService : ISubscritionService
    {
        private ArmClient _armClient;

        public SubscriptionService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<string>> FetchSubscriptionsAsync()
        {
            List<string> subscriptionChoices = new List<string>();
            try
            {
                var tenantFilter = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ??
                                    Environment.GetEnvironmentVariable("ARM_TENANT_ID");

                await AnsiConsole.Status()
                    .StartAsync("Fetching subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            if (string.IsNullOrEmpty(tenantFilter) || subscription.Data.TenantId == tenantFilter)
                            {
                                subscriptionChoices.Add($"{subscription.Data.DisplayName} ({subscription.Data.SubscriptionId})");
                            }
                        }
                    });

                if (tenantFilter != null && !subscriptionChoices.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]No subscriptions found for tenant {tenantFilter}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching subscriptions: {ex.Message}[/]");
            }
            return subscriptionChoices;
        }
    }
}
