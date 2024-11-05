﻿using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vm
{

    public class VMListSubscriptionCommand : AsyncCommand
    {
        private readonly IVMService _computeService;
        private readonly ISubscritionService _subscriptionService;

        public VMListSubscriptionCommand(IVMService computeService, ISubscritionService subscriptionService)
        {
            _computeService = computeService;
            _subscriptionService = subscriptionService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            try
            {
                var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();

                if (subscriptionChoices == null || !subscriptionChoices.Any())
                {
                    AnsiConsole.MarkupLine("[red]No subscriptions available or unable to fetch subscriptions.[/]");
                    return -1;
                }

                var selectedSubscription = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]subscription[/]:")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                        .AddChoices(subscriptionChoices));

                string subscriptionName = selectedSubscription.Split('(').Last().TrimEnd(')');

                var vms = await _computeService.FetchVMInSubscriptionAsync(subscriptionName);
                if (vms != null && vms.Any())
                {
                    var grid = new Grid();

                    grid.AddColumn(new GridColumn().Width(35));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(20));
                    grid.AddColumn(new GridColumn().Width(30));
                    grid.AddColumn(new GridColumn().Width(20));
                    grid.AddColumn(new GridColumn().Width(17));

                    grid.AddRow(
                        "[bold darkgreen]VM Name[/]",
                        "[bold darkgreen]Location[/]",
                        "[bold darkgreen]Subscription[/]",
                        "[bold darkgreen]Status[/]",
                        "[bold darkgreen]Image Name[/]",
                        "[bold darkgreen]Version[/]",
                        "[bold darkgreen]Marketplace Image[/]"
                         );

                    foreach (var vm in vms)
                    {
                        string status = vm.Status?.ToString() ?? "unknown";
                        var imageReference = vm.VM.Data.StorageProfile?.ImageReference;
                        string imageReferenceId = imageReference?.Id;
                        string imageName = "No image name found";
                        string version = "No version specified";
                        bool marketplace = false;

                        if (!string.IsNullOrEmpty(imageReferenceId))
                        {
                            var parts = imageReferenceId.Split('/');
                            if (parts.Length >= 10)
                            {
                                imageName = parts[10];
                                version = imageReference.ExactVersion;
                            }

                        }
                        else
                        {
                            imageName = imageReference.Sku;
                            version = imageReference.Version;
                            marketplace = true;
                        }


                        var marketplaceColor = marketplace != true ? "red" : "green";
                        var statusColor = status.Equals("running", StringComparison.OrdinalIgnoreCase) ? "green" : "red";

                        grid.AddRow(
                            $"[blue]{vm.VM.Data.Name}[/]",
                            $"[yellow]{vm.VM.Data.Location}[/]",
                            $"[yellow]{vm.SubscriptionName}[/]",
                            $"[{statusColor}]{status}[/]",
                            $"[yellow]{imageName}[/]",
                            $"[yellow]{version}[/]"
                        );
                    }

                    AnsiConsole.Write(grid);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]No virtual machines found.[/]");
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
