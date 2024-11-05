using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.Resources;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;
using System.Text;

namespace AzureOpsCLI.Services
{
    public class ACIService : IACIService
    {
        private ArmClient _armClient;

        public ACIService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<ContainerGroupResourceExtended>> FetchAllACIAsync()
        {
            List<ContainerGroupResourceExtended> aciList = new List<ContainerGroupResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            ctx.Status($"Fetching container instances in {subscription.Data.DisplayName}...");

                            var aciGroups = subscription.GetContainerGroups();

                            foreach (var aciGroup in aciGroups)
                            {

                                var container = aciGroup.GetAsync();
                                string containerStatus = container.Result.Value.Data.Containers.First().InstanceView?.CurrentState?.State ?? "unknown";

                                aciList.Add(new ContainerGroupResourceExtended
                                {
                                    ContainerGroup = aciGroup,
                                    Status = containerStatus.ToLower(),
                                    SubscriptionName = subscription.Data.DisplayName
                                });
                            }
                        }

                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching container instances: {ex.Message}[/]");
            }
            return aciList;
        }

        public async Task<List<ContainerGroupResourceExtended>> FetchACIInSubscriptionAsync(string subscriptionId)
        {
            List<ContainerGroupResourceExtended> aciList = new List<ContainerGroupResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                .StartAsync("Fetching Container Instances...", async ctx =>
                {
                    var aciGroups = subscription.GetContainerGroups();
                    foreach (var aciGroup in aciGroups)
                    {
                        ctx.Status($"Fetching container instances in {subscription.Data.DisplayName}...");
                        var container = aciGroup.GetAsync();
                        string containerStatus = container.Result.Value.Data.Containers.First().InstanceView?.CurrentState?.State ?? "unknown";

                        aciList.Add(new ContainerGroupResourceExtended
                        {
                            ContainerGroup = aciGroup,
                            Status = containerStatus.ToLower(),
                            SubscriptionName = subscription.Data.DisplayName
                        });
                    }

                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching container instances for subscription {subscriptionId}: {ex.Message}[/]");
            }
            return aciList;
        }


        public async Task<List<ContainerGroupResourceExtended>> FetchRunningACIAsync()
        {
            List<ContainerGroupResourceExtended> aciList = new List<ContainerGroupResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {

                            var aciGroups = subscription.GetContainerGroups();
                            ctx.Status($"Fetching container instances in {subscription.Data.DisplayName}...");
                            foreach (var aciGroup in aciGroups)
                            {

                                var container = aciGroup.GetAsync();
                                string containerStatus = container.Result.Value.Data.Containers.First().InstanceView?.CurrentState?.State ?? "unknown";

                                if (containerStatus.ToLower().Equals("running"))
                                    aciList.Add(new ContainerGroupResourceExtended
                                    {
                                        ContainerGroup = aciGroup,
                                        Status = containerStatus.ToLower(),
                                        SubscriptionName = subscription.Data.DisplayName
                                    });
                            }
                        }

                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching container instances: {ex.Message}[/]");
            }
            return aciList;
        }

        public async Task<List<ContainerGroupResourceExtended>> FetchStoppedACIAsync()
        {
            List<ContainerGroupResourceExtended> aciList = new List<ContainerGroupResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Fetching subscriptions...", async ctx =>
                    {
                        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                        {
                            var aciGroups = subscription.GetContainerGroups();
                            ctx.Status($"Fetching container instances in {subscription.Data.DisplayName}...");

                            foreach (var aciGroup in aciGroups)
                            {

                                var container = aciGroup.GetAsync();
                                string containerStatus = container.Result.Value.Data.Containers.First().InstanceView?.CurrentState?.State ?? "unknown";

                                if (containerStatus.ToLower().Equals("terminated"))
                                    aciList.Add(new ContainerGroupResourceExtended
                                    {
                                        ContainerGroup = aciGroup,
                                        Status = containerStatus.ToLower(),
                                        SubscriptionName = subscription.Data.DisplayName
                                    });
                            }
                        }

                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching container instances: {ex.Message}[/]");
            }
            return aciList;
        }

        public async Task<List<ContainerGroupResourceExtended>> FetchRunningACIInSubscriptionAsync(string subscriptionId)
        {
            List<ContainerGroupResourceExtended> aciList = new List<ContainerGroupResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                .StartAsync("Fetching Container Instances...", async ctx =>
                {
                    var aciGroups = subscription.GetContainerGroups();
                    foreach (var aciGroup in aciGroups)
                    {

                        var container = aciGroup.GetAsync();
                        string containerStatus = container.Result.Value.Data.Containers.First().InstanceView?.CurrentState?.State ?? "unknown";

                        if (containerStatus.ToLower().Equals("running"))
                            aciList.Add(new ContainerGroupResourceExtended
                            {
                                ContainerGroup = aciGroup,
                                Status = containerStatus.ToLower(),
                                SubscriptionName = subscription.Data.DisplayName
                            });
                    }

                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching container instances for subscription {subscriptionId}: {ex.Message}[/]");
            }
            return aciList;
        }

        public async Task<List<ContainerGroupResourceExtended>> FetchStoppedACIInSubscriptionAsync(string subscriptionId)
        {
            List<ContainerGroupResourceExtended> aciList = new List<ContainerGroupResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                .StartAsync("Fetching Container Instances...", async ctx =>
                {
                    var aciGroups = subscription.GetContainerGroups();
                    foreach (var aciGroup in aciGroups)
                    {

                        var container = aciGroup.GetAsync();
                        string containerStatus = container.Result.Value.Data.Containers.First().InstanceView?.CurrentState?.State ?? "unknown";

                        if (containerStatus.ToLower().Equals("terminated"))
                            aciList.Add(new ContainerGroupResourceExtended
                            {
                                ContainerGroup = aciGroup,
                                Status = containerStatus.ToLower(),
                                SubscriptionName = subscription.Data.DisplayName
                            });
                    }

                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching container instances for subscription [yellow]{subscriptionId}[/]: {ex.Message}[/]");
            }
            return aciList;
        }

        public async Task<OperationResult> StartACIAsync(ContainerGroupResource aciResource)
        {
            if (aciResource == null)
            {
                return new OperationResult { Success = false, Message = "Container Instance resource must not be null." };
            }

            try
            {
                await aciResource.StartAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Container Instance [blue]{aciResource.Data.Name}[/] started." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to start this container instance.",
                    404 => "The specified container instance could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> StopACIAsync(ContainerGroupResource aciResource)
        {
            if (aciResource == null)
            {
                return new OperationResult { Success = false, Message = "Container Instance resource must not be null." };
            }

            try
            {
                await aciResource.StopAsync(CancellationToken.None);
                return new OperationResult { Success = true, Message = $"Container Instance [blue]{aciResource.Data.Name}[/] stopped." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to stop this container instance.",
                    404 => "The specified container instance could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> RestartACIAsync(ContainerGroupResource aciResource)
        {
            if (aciResource == null)
            {
                return new OperationResult { Success = false, Message = "Container Instance resource must not be null." };
            }

            try
            {
                await aciResource.RestartAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Container Instance [blue]{aciResource.Data.Name}[/] restarted." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to restart this container instance.",
                    404 => "The specified container instance could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public async Task<OperationResult> DeleteACIAsync(ContainerGroupResource aciResource)
        {
            if (aciResource == null)
            {
                return new OperationResult { Success = false, Message = "Container Instance resource must not be null." };
            }

            try
            {
                await aciResource.DeleteAsync(WaitUntil.Completed);
                return new OperationResult { Success = true, Message = $"Container Instance [blue]{aciResource.Data.Name}[/] deleted." };
            }
            catch (RequestFailedException ex)
            {
                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to delete this container instance.",
                    404 => "The specified container instance could not be found.",
                    _ => $"{errorMessage}"
                };
                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }


        public async Task<OperationResult> GetContainerLogsAsync(ContainerGroupResource containerGroupResource)
        {
            if (containerGroupResource == null)
            {
                return new OperationResult { Success = false, Message = "Container Group resource must not be null." };
            }

            var logBuilder = new StringBuilder();

            try
            {
                foreach (var container in containerGroupResource.Data.Containers)
                {
                    try
                    {
                        var logs = await containerGroupResource.GetContainerLogsAsync(container.Name);
                        logBuilder.AppendLine($"------ Logs for container {container.Name} in group {containerGroupResource.Data.Name} \"------ :\n");
                        logBuilder.AppendLine(logs.Value.Content);
                    }
                    catch (RequestFailedException ex)
                    {
                        string errorMessage;
                        if (ex.Message.Contains("Status:"))
                        {
                            errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                        }
                        else
                        {
                            errorMessage = ex.Message;
                        }

                        var message = ex.Status switch
                        {
                            403 => $"You do not have permission to access logs for container {container.Name}.",
                            404 => $"The specified container {container.Name} could not be found in container group {containerGroupResource.Data.Name}.",
                            _ => $"{errorMessage}"
                        };
                        logBuilder.AppendLine($"Error: {message}");
                    }
                }

                return new OperationResult { Success = true, Message = logBuilder.ToString() };
            }
            catch (RequestFailedException ex)
            {

                string errorMessage;
                if (ex.Message.Contains("Status:"))
                {
                    errorMessage = ex.Message.Split(new[] { "Status:" }, StringSplitOptions.None)[0].Trim();
                }
                else
                {
                    errorMessage = ex.Message;
                }

                var message = ex.Status switch
                {
                    403 => "You do not have permission to retrieve logs for this container group.",
                    404 => $"The specified container group {containerGroupResource.Data.Name} could not be found.",
                    _ => $"{errorMessage}"
                };

                return new OperationResult { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public Task<OperationResult> ExecuteContainerCommandAsync(ContainerGroupResource aciResource, string command)
        {
            throw new NotImplementedException();
        }
    }
}

