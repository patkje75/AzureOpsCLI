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
                await AnsiConsole.Status()
                    .StartAsync("Fetching subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            subscriptionChoices.Add($"{subscription.Data.DisplayName} ({subscription.Data.SubscriptionId})");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching subscriptions: {ex.Message}[/]");
            }
            return subscriptionChoices;
        }
    }
}
