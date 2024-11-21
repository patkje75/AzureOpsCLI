using Azure.Core;
using Azure.ResourceManager.Models;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using AzureOpsCLI.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Commands.apim
{
    public class APIMBackupSubscriptionCommand : AsyncCommand
    {
        private readonly IAPIManagementService _apiManagementService;
        private readonly IStorageService _storageService;
        private readonly ISubscritionService _subscriptionService;

        public APIMBackupSubscriptionCommand(IAPIManagementService apiManagementService, IStorageService storageService, ISubscritionService subscriptionService)
        {
            _apiManagementService = apiManagementService;
            _storageService = storageService;
            _subscriptionService = subscriptionService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();
            var selectedApimSubscription = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]subscription[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                    .AddChoices(subscriptionChoices));

            string apimSubscriptionId = selectedApimSubscription.Split('(').Last().TrimEnd(')');

            var apimServices = await _apiManagementService.FetchAPIMInSubscriptionsAsync(apimSubscriptionId);
            if (!apimServices.Any())
            {
                AnsiConsole.MarkupLine("[red]No API Management services found.[/]");
                return -1;
            }

           // API Managment
           var apimSelection = AnsiConsole.Prompt(
           new SelectionPrompt<string>()
               .Title("Select an API Management services to back up:")
               .PageSize(10)
               .AddChoices(apimServices.Select(apim => $"{apim.APIManagementService.Data.Name} ({apim.APIManagementService.Data.Location})")));

            var selectedAPIM = apimServices.First(apim => $"{apim.APIManagementService.Data.Name} ({apim.APIManagementService.Data.Location})" == apimSelection);

            // Managed Identities
            var selectedIdentity = new ManagedIdentity();
            bool managedIdentitySet = selectedAPIM.APIManagementService.Data.Identity?.ManagedServiceIdentityType.ToString() != null;

            if (!managedIdentitySet)
            {
                AnsiConsole.MarkupLine("[red]Error: No Managed Identity is associated with this API Management service. Please assign a System-Assigned or User-Assigned Managed Identity and try again.[/]");
                return -1;
            }

            //Managed Identity type
            var identityType = selectedAPIM.APIManagementService.Data.Identity.ManagedServiceIdentityType;

            if (identityType == "SystemAssigned, UserAssigned")
            {
                var identityTypeSelection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Both [green]System-Assigned[/] and [blue]User-Assigned[/] Managed Identities are available. Select one:")
                        .AddChoices("SystemAssigned", "UserAssigned")
                );

                if (identityTypeSelection == "SystemAssigned")
                {
                    selectedIdentity = new ManagedIdentity
                    {
                        Type = "SystemAssigned"
                    };
                }
                else
                {
                    selectedIdentity = HandleUserAssignedIdentity(selectedAPIM);
                    if (selectedIdentity == null)
                    {
                        return -1;
                    }
                }
            }
            else if (identityType == "SystemAssigned")
            {
                AnsiConsole.MarkupLine($"[green]Selected System-Assigned Managed Identity.[/]");
                selectedIdentity = new ManagedIdentity
                {
                    Type = "SystemAssigned"
                };
            }
            else if (identityType == "UserAssigned")
            {
                selectedIdentity = HandleUserAssignedIdentity(selectedAPIM);
                if (selectedIdentity == null)
                {
                    return -1;
                }
            }

            //Subscriptions
            var subscriptions = await _subscriptionService.FetchSubscriptionsAsync();
            if (!subscriptions.Any())
            {
                AnsiConsole.MarkupLine("[red]No subscriptions found.[/]");
                return 1;
            }

            var selectedSubscription = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Select a [green]subscription[/]:")
                                .PageSize(10)
                                .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                                .AddChoices(subscriptions));

            string subscriptionId = selectedSubscription.Split('(').Last().TrimEnd(')');

            var storageAccounts = await _storageService.FetchStorageAccountsInSubscriptionAsync(subscriptionId);
            if (!storageAccounts.Any())
            {
                AnsiConsole.MarkupLine("[red]No storage accounts found for the selected subscription.[/]");
                return 1;
            }

            //Storage
            var storageSelection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Select a storage account to back up to:")
                    .PageSize(10)
                    .AddChoices(storageAccounts.Select(storage => $"{storage.StorageAccountResource.Data.Name} ({storage.StorageAccountResource.Data.Location})")));

            var selectedStorage = storageAccounts.First(storage => $"{storage.StorageAccountResource.Data.Name} ({storage.StorageAccountResource.Data.Location})" == storageSelection);

            await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new SpinnerColumn(),
            })
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"Backing up [blue]{selectedAPIM.APIManagementService.Data.Name}[/] to [blue]{selectedStorage.StorageAccountResource.Data.Name}[/]");

                await _apiManagementService.BackupAPIMAsync(selectedAPIM, selectedStorage, selectedIdentity, "apim-backup")
                    .ContinueWith(t =>
                    {
                        if (t.Result.Success)
                        {
                            task.Description = $"Backup was [green]successful[/] for [blue]{selectedAPIM.APIManagementService.Data.Name}[/] to [blue]{selectedStorage.StorageAccountResource.Data.Name}[/]: {t.Result.Message}";
                            task.Increment(100);
                        }
                        else
                        {
                            task.Description = $"[red]Failed[/] to back up [blue]{selectedAPIM.APIManagementService.Data.Name}[/] to [blue]{selectedStorage.StorageAccountResource.Data.Name}[/]: {t.Result.Message}";
                            task.Increment(100);
                        }
                    });
            });

            return 0;
        }

        ManagedIdentity HandleUserAssignedIdentity(ApiManagementServiceResourceExtended apim)
        {
            var userAssignedIdentities = apim.APIManagementService.Data.Identity.UserAssignedIdentities;
            if (userAssignedIdentities == null || !userAssignedIdentities.Any())
            {
                AnsiConsole.MarkupLine("[red]Error: No User-Assigned Managed Identities found. Please assign a User-Assigned Managed Identity to this API Management service and try again.[/]");
                return null;
            }

            if (userAssignedIdentities.Count == 1)
            {
                var singleIdentity = userAssignedIdentities.First();
                AnsiConsole.MarkupLine($"[green]Selected User-Assigned Managed Identity: {singleIdentity.Key.Name}[/]");
                return new ManagedIdentity
                {
                    ClientId = singleIdentity.Value.ClientId.ToString(),
                    Type = "UserAssigned"
                };
            }

            var identitySelection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]User-Assigned Managed Identity[/]:")
                    .PageSize(10)
                    .AddChoices(userAssignedIdentities.Select(kvp => $"{kvp.Key.Name} > ClientId: {kvp.Value.ClientId}"))
            );

            var selectedIdentityKey = identitySelection.Split('>')[0].Trim();
            return new ManagedIdentity
            {
                ClientId = userAssignedIdentities.First(kvp => kvp.Key.Name == selectedIdentityKey).Value.ClientId.ToString(),
                Type = "UserAssigned"
            };
        }
    }
}
