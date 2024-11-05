using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureOpsCLI.Commands.vmss
{
    public class VMSSStartSubscriptionCommand : AsyncCommand
    {
        private readonly IVMSSService _vmssService;
        private readonly ISubscritionService _subscriptionService;

        public VMSSStartSubscriptionCommand(IVMSSService vmssService, ISubscritionService subscritionService)
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
            var deallocatedVMSSs = await _vmssService.FetchStoppedVMSSInSubscriptionAsync(subscriptionId);

            if (!deallocatedVMSSs.Any())
            {
                AnsiConsole.MarkupLine("[red]No stopped Virtual Machine Scale Sets found in the selected subscription.[/]");
                return 0;
            }


            var vmNames = deallocatedVMSSs.Select(vm => $"{vm.VMSS.Data.Name} in {vm.VMSS.Data.Location} (Subscription {vm.SubscriptionName})").ToList();

            var selectedVMs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Virtual Machine Scale Sets to start:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more Virtual Machine Scales Sets)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a Scale Set, [green]<enter>[/] to start selected Scale Set)[/]")
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
                        var vmExtended = deallocatedVMSSs.First(v => $"{v.VMSS.Data.Name} in {v.VMSS.Data.Location} (Subscription {v.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Starting {vmExtended.VMSS.Data.Name}");

                        return _vmssService.StartVMSSAsync(vmExtended.VMSS)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Start was [green]successfull[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to start [blue]{vmExtended.VMSS.Data.Name}[/]: {t.Result.Message}";
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
