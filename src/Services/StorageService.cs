using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
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
    public class StorageService : IStorageService
    {
        private ArmClient _armClient;
        public StorageService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<StorageAccountResourceExtended>> FetchAllStorageAccountsAsync()
        {
            var storageAccounts = new List<StorageAccountResourceExtended>();

            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            var resources = subscription.GetStorageAccounts();
                            ctx.Status($"Fetching all Storage accounts in {subscription.Data.DisplayName}...");

                            foreach (var storage in resources)
                            {
                                storageAccounts.Add(new StorageAccountResourceExtended
                                {
                                    StorageAccountResource = storage,
                                    SubscriptionName = subscription.Data.DisplayName
                                });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching Storage accounts: {ex.Message}[/]");
            }

            return storageAccounts;
        }

        public async Task<List<StorageAccountResourceExtended>> FetchStorageAccountsInSubscriptionAsync(string subscriptionId)
        {
            var storageAccounts = new List<StorageAccountResourceExtended>();

            try
            {
                var subscription = AnsiConsole.Status()
                   .StartAsync($"Fetching subscription...", async ctx =>
                   {
                       return await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                   });


                await AnsiConsole.Status()
                    .StartAsync($"Fetching all Storage accounts in {subscription.Result.Value.Data.DisplayName}...", async ctx =>
                    {
                        var resources = subscription.Result.Value.GetStorageAccounts();

                        foreach (var resource in resources)
                        {
                            ctx.Status($"Found Storage account {resource.Data.Name}...");

                            storageAccounts.Add(new StorageAccountResourceExtended
                            {
                                StorageAccountResource = resource,
                                SubscriptionName = subscription.Result.Value.Data.DisplayName
                            });
                        }
                    });

            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching Storage accounts: {ex.Message}[/]");
            }

            return storageAccounts;
        }
    }
}
