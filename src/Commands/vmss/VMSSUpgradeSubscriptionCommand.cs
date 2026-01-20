using AzureOpsCLI.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

public class VMSSUpgradeSubscriptionCommand : AsyncCommand
{
    private readonly IVMSSService _vmssService;
    private readonly IImageGalleryService _imageGalleryService;
    private readonly ISubscritionService _subscriptionService;

    public VMSSUpgradeSubscriptionCommand(IVMSSService vmssService, IImageGalleryService imageGalleryService, ISubscritionService subscriptionService)
    {
        _vmssService = vmssService;
        _imageGalleryService = imageGalleryService;
        _subscriptionService = subscriptionService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {

            var subscriptionChoices = await _subscriptionService.FetchSubscriptionsAsync();
            var selectedSubscription = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]subscription[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more subscriptions)[/]")
                    .AddChoices(subscriptionChoices));

            string subscriptionId = selectedSubscription.Split('(').Last().TrimEnd(')');
            var vmssList = await _vmssService.FetchVMSSInSubscriptionAsync(subscriptionId);

            if (!vmssList.Any())
            {
                AnsiConsole.MarkupLine("[red]No Virtual Machine Scale Sets found in the selected subscription.[/]");
                return 0;
            }

            var vmssNames = vmssList.Select(vm => $"{vm.VMSS.Data.Name} in {vm.VMSS.Data.Location} (Subscription {vm.SubscriptionName})").ToList();
            var selectedVMSSs = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select Scale Set(s) to upgrade to the latest model:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more Virtual Machine Scales Sets)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a Scale Set, [green]<enter>[/] to upgrade selected Scale Set)[/]")
                    .AddChoices(vmssNames));

            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var tasks = selectedVMSSs.Select(vmName =>
                    {
                        var vmExtended = vmssList.First(v => $"{v.VMSS.Data.Name} in {v.VMSS.Data.Location} (Subscription {v.SubscriptionName})" == vmName);
                        var task = ctx.AddTask($"Upgrading {vmExtended.VMSS.Data.Name} to the latest model");

                        return _vmssService.UpgradeVMSSToLatestModelAsync(vmExtended.VMSS)
                            .ContinueWith(t =>
                            {
                                if (t.Result.Success)
                                {
                                    task.Description = $"Upgrade was [green]successful[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                                else
                                {
                                    task.Description = $"[red]Failed[/] to upgrade [blue]{vmExtended.VMSS.Data.Name}[/]: {t.Result.Message}";
                                    task.Increment(100);
                                }
                            });
                    }).ToList();

                    await Task.WhenAll(tasks);
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
