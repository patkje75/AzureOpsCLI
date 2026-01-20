using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vmss
{
    public class VMSSListSubscriptionCommand : AsyncCommand
    {
        private readonly IVMSSService _vmssService;
        private readonly ISubscritionService _subscriptionService;

        public VMSSListSubscriptionCommand(IVMSSService vmssService, ISubscritionService subscritionService)
        {
            _vmssService = vmssService;
            _subscriptionService = subscritionService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {

            var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();
            var selectedSubscription = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]subscription[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                    .AddChoices(subscriptionChoices));

            string subscriptionId = selectedSubscription.Split('(').Last().TrimEnd(')');

            try
            {
                var vmscalesets = await _vmssService.FetchVMSSInSubscriptionAsync(subscriptionId);
                if (vmscalesets.Count > 0)
                {
                    var grid = new Grid();

                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(15));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(10));
                    grid.AddColumn(new GridColumn().Width(20));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(25));
                    grid.AddColumn(new GridColumn().Width(17));

                    grid.AddRow(
                        "[bold darkgreen]Name[/]",
                        "[bold darkgreen]Location[/]",
                        "[bold darkgreen]Subscription Name[/]",
                        "[bold darkgreen]Status[/]",
                        "[bold darkgreen]Instances[/]",
                        "[bold darkgreen]Image Name[/]",
                        "[bold darkgreen]Version[/]",
                        "[bold darkgreen]Marketplace Image[/]"
                        );

                    foreach (var vmss in vmscalesets)
                    {
                        var imageReference = vmss.VMSS.Data.VirtualMachineProfile?.StorageProfile?.ImageReference;
                        string imageReferenceId = imageReference?.Id;
                        string imageName = "No image name found";
                        string imageVersion = "No version specified";
                        bool marketplace = false;

                        if (!string.IsNullOrEmpty(imageReferenceId))
                        {
                            var parts = imageReferenceId.Split('/');
                            if (parts.Length >= 12)
                            {
                                imageName = parts[10];
                                imageVersion = parts[12];
                            }
                            else
                            {
                                imageName = parts[10];
                            }

                        }
                        else
                        {
                            imageName = imageReference.Offer;
                            imageVersion = imageReference.Version;
                            marketplace = true;
                        }

                        var statusColor = vmss.Status != "running" ? "red" : "green";
                        var marketplaceColor = marketplace != true ? "red" : "green";

                        grid.AddRow(
                            $"[blue]{vmss.VMSS.Data.Name}[/]",
                            $"[yellow]{vmss.VMSS.Data.Location}[/]",
                            $"[yellow]{vmss.SubscriptionName}[/]",
                            $"[{statusColor}]{vmss.Status}[/]",
                            $"[yellow]{vmss.numberOfInstances}[/]",
                            $"[yellow]{imageName}[/]",
                            $"[yellow]{imageVersion}[/]",
                            $"[{marketplaceColor}]{marketplace}[/]"
                        );
                    }

                    AnsiConsole.Write(grid);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]No virtual machine scale sets found.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                return -1;
            }

            return 0;

        }
    }
}
