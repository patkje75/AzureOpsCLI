using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vm
{

    public class VMStartSubscriptionCommand : AsyncCommand
    {
        private readonly IVMService _computeService;
        private readonly ISubscritionService _subscriptionService;

        public VMStartSubscriptionCommand(IVMService computeService, ISubscritionService subscritionService)
        {
            _computeService = computeService;
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

            var deallocatedVMs = await _computeService.FetchDeallocatedVMInSubscriptionAsync(subscriptionId);

            if (!deallocatedVMs.Any())
            {
                AnsiConsole.MarkupLine("[red]No deallocated Virtual Machine found in the selected subscription.[/]");
                return 0;
            }

            var vmNames = deallocatedVMs.Select(vm => $"{vm.VM.Data.Name} in {vm.VM.Data.Location} (Subscription {vm.SubscriptionName})").ToList();

            var selectedVMs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select virtual machines to start:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more VMs)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a VM, [green]<enter>[/] to start selected VMs)[/]")
                    .AddChoices(vmNames));

            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
            new TaskDescriptionColumn(),
            new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var tasks = selectedVMs.Select(vmName =>
                    {
                        var vmExtended = deallocatedVMs.First(v => $"{v.VM.Data.Name} in {v.VM.Data.Location} (Subscription {v.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Starting {vmExtended.VM.Data.Name}");

                        return _computeService.StartVMAsync(vmExtended.VM)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Start was [green]successfull[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to start [blue]{vmExtended.VM.Data.Name}[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                            });
                    }).ToList();

                    await Task.WhenAll(tasks);
                });


            return 0;
        }
    }

}
