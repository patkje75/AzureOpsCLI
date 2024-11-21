using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ApiManagement.Models;
using Azure.ResourceManager.ManagedServiceIdentities;
using Azure.ResourceManager.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs.Models;
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
    public class APIManagementService : IAPIManagementService
    {
        private ArmClient _armClient;

        public APIManagementService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }


        public async Task<List<ApiManagementServiceResourceExtended>> FetchAllAPIMAsync()
        {
            var apiManagementServices = new List<ApiManagementServiceResourceExtended>();

            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching all API Management services in {subscription.Data.DisplayName}...");
                            var apims = subscription.GetApiManagementServicesAsync();
                            await foreach (var apim in apims)
                            {

                                    apiManagementServices.Add(new ApiManagementServiceResourceExtended
                                    {
                                        APIManagementService = apim,
                                        SubscriptionName = subscription.Data.DisplayName
                                    }
                                );
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching API Management services: {ex.Message}[/]");
            }

            return apiManagementServices;
        }

        public async Task<List<ApiManagementServiceResourceExtended>> FetchAPIMInSubscriptionsAsync(string subscriptionId)
        {
            var apiManagementServices = new List<ApiManagementServiceResourceExtended>();
            SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

            try
            {
                await AnsiConsole.Status()
                    .StartAsync($"Fetching all API Managment Services in {subscription.Data.DisplayName}...", async ctx =>
                    {                        
                        var apims = subscription.GetApiManagementServicesAsync();
                        await foreach (var apim in apims)
                        {
                            ctx.Status($"Found API Managment Service {apim.Data.Name}...");

                            apiManagementServices.Add(new ApiManagementServiceResourceExtended
                            {
                                APIManagementService = apim,
                                SubscriptionName = subscription.Data.DisplayName
                            }
                            );
                        }
                    });
           
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching API Management services: {ex.Message}[/]");
            }

            return apiManagementServices;
        }

        public async Task<OperationResult> BackupAPIMAsync(ApiManagementServiceResourceExtended apim, StorageAccountResourceExtended storageAccount, ManagedIdentity identity, string containerName)
        {
            if (apim == null)
            {
                return new OperationResult { Success = false, Message = "API Management resource must not be null." };
            }

            if (storageAccount == null)
            {
                return new OperationResult { Success = false, Message = "Storage resource must not be null." };
            }

            if (identity.Type == null)
            {
                return new OperationResult { Success = false, Message = "Managed Identity must not be null." };
            }

            try
            {                
                await CreateBlobContainerAsync(storageAccount.StorageAccountResource, containerName);
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "You do not have permission to create a blob container. Please check your RBAC roles."
                };
            }
            catch (Exception ex)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Failed to create blob container: {ex.Message}"
                };
            }

            var accessType = identity.Type == "SystemAssigned" ? "SystemAssignedManagedIdentity" : "UserAssignedManagedIdentity";

            if (accessType == "UserAssignedManagedIdentity" && string.IsNullOrEmpty(identity.ClientId))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "The ClientId for the User Assigned Managed Identity is missing or invalid."
                };
            }

            var backupContent = new ApiManagementServiceBackupRestoreContent(storageAccount.StorageAccountResource.Data.Name, containerName, $"{apim.APIManagementService.Data.Name} Backup {DateTime.Now:yyyy-MM-dd HH:mm}")
            {
                AccessType = accessType               
            };
                        
            if (accessType == "UserAssignedManagedIdentity")
            {
                backupContent.ClientId = identity.ClientId;
            }

            try
            {
                await apim.APIManagementService.BackupAsync(WaitUntil.Completed, backupContent);

                return new OperationResult
                {
                    Success = true,
                    Message = $"API Management Service [blue]{apim.APIManagementService.Data.Name}[/] backed up successfully."
                };
            }
            catch (RequestFailedException ex)
            {
                var errorMessage = ex.Message.Split("\r\n").FirstOrDefault()?.Trim() ?? ex.Message;

                var message = ex.Status switch
                {
                    403 => "You do not have permission to perform the backup. Make sure you have at least 'Microsoft.ApiManagement/service/backup/action' on the API Management resource.",
                    404 => "The specified API Management service could not be found.",
                    400 => $"{errorMessage} Possible causes include invalid API Managment resource configurations or insufficient permissions for the Managed Service Identity.",
                    _ => $"{errorMessage}"
                };

                return new OperationResult { Success = false, Message = message };
            }
        }

        private async Task CreateBlobContainerAsync(StorageAccountResource storageAccountResource, string containerName)
        {          
            BlobServiceResource blobService = storageAccountResource.GetBlobService();
            BlobContainerCollection containerCollection = blobService.GetBlobContainers();          

            if (!containerCollection.Exists(containerName).Value)
            {

                await containerCollection.CreateOrUpdateAsync(
                    WaitUntil.Completed,
                    containerName,
                    new BlobContainerData()
                    {
                        PublicAccess = Azure.ResourceManager.Storage.Models.StoragePublicAccessType.None
                    }
                );

            }

        }      

    }
}
