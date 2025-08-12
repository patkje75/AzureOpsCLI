using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;
using System.Text.Json;

namespace AzureOpsCLI.Services
{
    public class TagService : ITagService
    {
        private readonly ArmClient _armClient;

        public TagService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<ResourceExtended>> FetchAllResourcesWithTagsAsync()
        {
            var resourceList = new List<ResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching all subscriptions and resources...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching resources in {subscription.Data.DisplayName}...");

                            await foreach (var resource in subscription.GetGenericResourcesAsync())
                            {
                                resourceList.Add(new ResourceExtended
                                {
                                    Resource = resource,
                                    SubscriptionName = subscription.Data.DisplayName
                                });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching resources: {ex.Message}[/]");
            }
            return resourceList;
        }

        public async Task<List<ResourceExtended>> FetchResourcesWithTagsInSubscriptionAsync(string subscriptionId)
        {
            var resourceList = new List<ResourceExtended>();
            try
            {
                var subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                    .StartAsync($"Fetching resources in subscription {subscription.Value.Data.DisplayName}...", async ctx =>
                    {
                        await foreach (var resource in subscription.Value.GetGenericResourcesAsync())
                        {
                            resourceList.Add(new ResourceExtended
                            {
                                Resource = resource,
                                SubscriptionName = subscription.Value.Data.DisplayName
                            });
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching resources for subscription {subscriptionId}: {ex.Message}[/]");
            }
            return resourceList;
        }

        public async Task<OperationResult> ApplyTagsAsync(GenericResource resource, Dictionary<string, string> tags)
        {
            if (resource == null)
            {
                return new OperationResult { Success = false, Message = "Resource must not be null." };
            }

            if (tags == null || !tags.Any())
            {
                return new OperationResult { Success = false, Message = "Tags dictionary must not be null or empty." };
            }

            try
            {
                var currentTags = resource.Data.Tags?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();
                
                // Merge new tags with existing ones
                foreach (var tag in tags)
                {
                    currentTags[tag.Key] = tag.Value;
                }

                await resource.SetTagsAsync(currentTags);
                return new OperationResult { Success = true, Message = $"Tags applied successfully to [blue]{resource.Data.Name}[/]." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage = ex.Message.Contains("Status:") ? 
                    ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim() : 
                    ex.Message;

                var message = ex.Status switch
                {
                    403 => "You do not have permission to modify tags on this resource.",
                    404 => "The specified resource could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> RemoveTagsAsync(GenericResource resource, List<string> tagKeys)
        {
            if (resource == null)
            {
                return new OperationResult { Success = false, Message = "Resource must not be null." };
            }

            if (tagKeys == null || !tagKeys.Any())
            {
                return new OperationResult { Success = false, Message = "Tag keys list must not be null or empty." };
            }

            try
            {
                var currentTags = resource.Data.Tags?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();
                
                // Remove specified tags
                foreach (var tagKey in tagKeys)
                {
                    currentTags.Remove(tagKey);
                }

                await resource.SetTagsAsync(currentTags);
                return new OperationResult { Success = true, Message = $"Tags removed successfully from [blue]{resource.Data.Name}[/]." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage = ex.Message.Contains("Status:") ? 
                    ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim() : 
                    ex.Message;

                var message = ex.Status switch
                {
                    403 => "You do not have permission to modify tags on this resource.",
                    404 => "The specified resource could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<string> ExportTagsToJsonAsync(List<ResourceExtended> resources)
        {
            var exportData = resources.Select(r => new
            {
                ResourceName = r.ResourceName,
                ResourceType = r.ResourceType,
                ResourceGroup = r.ResourceGroupName,
                Location = r.Location,
                Subscription = r.SubscriptionName,
                Tags = r.Tags
            }).ToList();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return await Task.FromResult(JsonSerializer.Serialize(exportData, options));
        }

        public async Task<string> ExportTagsToCsvAsync(List<ResourceExtended> resources)
        {
            var csv = new List<string>
            {
                "ResourceName,ResourceType,ResourceGroup,Location,Subscription,TagKey,TagValue"
            };

            foreach (var resource in resources)
            {
                if (resource.Tags.Any())
                {
                    foreach (var tag in resource.Tags)
                    {
                        csv.Add($"\"{resource.ResourceName}\",\"{resource.ResourceType}\",\"{resource.ResourceGroupName}\",\"{resource.Location}\",\"{resource.SubscriptionName}\",\"{tag.Key}\",\"{tag.Value}\"");
                    }
                }
                else
                {
                    csv.Add($"\"{resource.ResourceName}\",\"{resource.ResourceType}\",\"{resource.ResourceGroupName}\",\"{resource.Location}\",\"{resource.SubscriptionName}\",\"\",\"\"");
                }
            }

            return await Task.FromResult(string.Join(Environment.NewLine, csv));
        }
    }
}