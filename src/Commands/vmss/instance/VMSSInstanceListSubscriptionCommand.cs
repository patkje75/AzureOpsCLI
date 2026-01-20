using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vmss.instance
{
    public class VMSSInstanceListSubscriptionCommand : AsyncCommand
    {
        private readonly IVMSSVMService _vmssvmService;
        private readonly ISubscritionService _subscriptionService;

        public VMSSInstanceListSubscriptionCommand(IVMSSVMService vmssvmService, ISubscritionService subscritionService)
        {
            _vmssvmService = vmssvmService;
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
                var vmscalesets = await _vmssvmService.FetchVMSSInstancesInSubscriptionAsync(subscriptionId);
                if (vmscalesets.Count > 0)
                {
                    var grid = new Grid();
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(15));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(15));
                    grid.AddColumn(new GridColumn().Width(10));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(25));
                    grid.AddColumn(new GridColumn().Width(17));

                    grid.AddRow(
                        "[bold darkgreen]Instance Name[/]",
                        "[bold darkgreen]Scale Set[/]",
                        "[bold darkgreen]Location[/]",
                        "[bold darkgreen]Subscription[/]",
                        "[bold darkgreen]Latest Model[/]",
                        "[bold darkgreen]Status[/]",
                        "[bold darkgreen]Image Name[/]",
                        "[bold darkgreen]Image Version[/]",
                        "[bold darkgreen]Marketplace Image[/]"
                    );

                    foreach (var vmssvms in vmscalesets)
                    {

                        var imageReference = vmssvms.VMSS.Data.VirtualMachineProfile?.StorageProfile?.ImageReference;
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

                        var latestModelColor = vmssvms.VMSSVm.Data.LatestModelApplied == true ? "green" : "red";
                        var statusColor = vmssvms.Status == "running" ? "green" : "red";
                        var marketplaceColor = marketplace != true ? "red" : "green";

                        grid.AddRow(
                            $"[blue]{vmssvms.VMSSVm.Data.Name}[/]",
                            $"[yellow]{vmssvms.VMSS.Data.Name}[/]",
                            $"[yellow]{vmssvms.VMSSVm.Data.Location}[/]",
                            $"[yellow]{vmssvms.SubscriptionName}[/]",
                            $"[{latestModelColor}]{vmssvms.VMSSVm.Data.LatestModelApplied}[/]",
                            $"[{statusColor}]{vmssvms.Status}[/]",
                            $"[yellow]{imageName}[/]",
                            $"[yellow]{imageVersion}[/]",
                            $"[{marketplaceColor}]{marketplace}[/]"
                        );
                    }

                    AnsiConsole.Write(grid);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]No virtual machine scale set instances found.[/]");
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
