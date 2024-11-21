using AzureOpsCLI.Commands.aci;
using AzureOpsCLI.Commands.apim;
using AzureOpsCLI.Commands.imagegallery;
using AzureOpsCLI.Commands.Info;
using AzureOpsCLI.Commands.mg;
using AzureOpsCLI.Commands.vm;
using AzureOpsCLI.Commands.vmss;
using AzureOpsCLI.Commands.vmss.instance;
using AzureOpsCLI.DependencyInjection;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IVMService, VMService>();
        services.AddSingleton<IVMSSService, VMSSService>();
        services.AddSingleton<IVMSSVMService, VMSSVMService>();
        services.AddSingleton<ISubscritionService, SubscriptionService>();
        services.AddSingleton<IACIService, ACIService>();
        services.AddSingleton<IImageGalleryService, ImageGalleryService>();
        services.AddSingleton<IMGService, MGService>();
        services.AddSingleton<IAPIManagementService, APIManagementService>();
        services.AddSingleton<IStorageService, StorageService>();
        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {

            config.AddBranch("vm", vm =>
            {
                //Description
                vm.SetDescription("Virtual Machine commands");
                vm.AddBranch("list", list =>
                {
                    //Description
                    list.SetDescription("List commands associated with virtual machine resources");
                    //Commands
                    list.AddCommand<VMListAllCommand>("all")
                        .WithDescription("Get all virtual machines in all subscriptions.");
                    list.AddCommand<VMListSubscriptionCommand>("subscription")
                        .WithDescription("Get all virtual machines in a specific subscriptions.");
                });

                vm.AddBranch("start", start =>
                {
                    //Description
                    start.SetDescription("Start commands associated with virtual machine resources");
                    //Commands
                    start.AddCommand<VMStartAllCommand>("all")
                        .WithDescription("Get all deallocated virtual machines in all subscriptions and start the selected resources from a list.");
                    start.AddCommand<VMStartSubscriptionCommand>("subscription")
                        .WithDescription("Get all deallocated virtual machines for a specific subscriptions and start the selected resources from a list.");

                });

                vm.AddBranch("stop", stop =>
                {
                    //Description
                    stop.SetDescription("Stop commands associated with virtual machine resources");
                    //Commands
                    stop.AddCommand<VMStopAllCommand>("all")
                        .WithDescription("Get all running virtual machines in all subscriptions and stop the selected resources from a list.");
                    stop.AddCommand<VMStopSubscriptionCommand>("subscription")
                        .WithDescription("Get all running virtual machines for a specific subscriptions and stop the selected resources from a list.");
                });

                vm.AddBranch("restart", restart =>
                {
                    //Description
                    restart.SetDescription("Restart commands associated with virtual machine resources");
                    //Commands
                    restart.AddCommand<VMRestartAllCommand>("all")
                        .WithDescription("Get all running virtual machines in all subscriptions and restart the selected resources from a list.");
                    restart.AddCommand<VMRestartSubscriptionCommand>("subscription")
                        .WithDescription("Get all running virtual machines for a specific subscriptions and restart the selected resources from a list.");
                });
                vm.AddBranch("delete", delete =>
                {
                    //Description
                    delete.SetDescription("Delete commands associated with virtual machine resources");
                    //Commands
                    delete.AddCommand<VMDeleteAllCommand>("all")
                        .WithDescription("Get all virtual machines in all subscriptions and delete the selected resources from a list.");
                    delete.AddCommand<VMDeleteSubscriptionCommand>("subscription")
                        .WithDescription("Get all virtual machines for a specific subscriptions and delete the selected resources from a list.");
                });

            });

            config.AddBranch("vmss", vmss =>
            {
                //Description
                vmss.SetDescription("Virtual Machine Scale Set commands");
                vmss.AddBranch("list", list =>
                {
                    //Description
                    list.SetDescription("List commands associated with virtual machine scale sets");
                    //Commands
                    list.AddCommand<VMSSListAllCommand>("all")
                        .WithDescription("Get all virtual machine scale sets in all subscriptions.");
                    list.AddCommand<VMSSListScriptionCommand>("subscription")
                        .WithDescription("Get all virtual machine scale sets in a specific subscriptions.");
                });
                vmss.AddBranch("start", start =>
                {
                    //Description
                    start.SetDescription("Start commands associated with virtual machine scale sets");
                    //Commands
                    start.AddCommand<VMSSStartAllCommand>("all")
                        .WithDescription("Get all stopped virtual machine scale sets in all subscriptions and start selected resources from a list.");
                    start.AddCommand<VMSSStartSubscriptionCommand>("subscription")
                        .WithDescription("Get all stopped virtual machine scale sets in a specific subscriptions and start selected resources from a list.");
                });
                vmss.AddBranch("stop", stop =>
                {
                    //Description
                    stop.SetDescription("Stop commands associated with virtual machine scale sets");
                    //Commands
                    stop.AddCommand<VMSSStopAllCommand>("all")
                        .WithDescription("Get all running virtual machine scale sets in all subscriptions and stop selected resources from a list.");
                    stop.AddCommand<VMSSStopSubscriptionCommand>("subscription")
                        .WithDescription("Get all running virtual machine scale sets in a specific subscriptions and stop selected resources from a list.");
                });
                vmss.AddBranch("restart", restart =>
                {
                    //Description
                    restart.SetDescription("Restart commands associated with virtual machine scale sets");
                    //Commands
                    restart.AddCommand<VMSSRestartAllCommand>("all")
                        .WithDescription("Get all running virtual machine scale sets in all subscriptions and restart selected resources from a list.");
                    restart.AddCommand<VMSSRestartSubscriptionCommand>("subscription")
                        .WithDescription("Get all running virtual machine scale sets in a specific subscriptions and restart selected resources from a list.");
                });
                vmss.AddBranch("reimage", reimage =>
                {
                    //Description
                    reimage.SetDescription("Reimage commands associated with virtual machine scale sets");
                    //Commands
                    reimage.AddCommand<VMSSReimageAllCommand>("all")
                        .WithDescription("Get all virtual machine scale sets in all subscriptions and reimage selected resources from a list.");
                    reimage.AddCommand<VMSSReimageSubscriptionCommand>("subscription")
                        .WithDescription("Get all virtual machine scale sets in a specific subscriptions and reimage selected resources from a list.");
                });
                vmss.AddBranch("upgrade", upgrade =>
                {
                    //Description
                    upgrade.SetDescription("Upgrade commands associated with virtual machine scale sets");
                    //Commands
                    upgrade.AddCommand<VMSSUpgradeAllCommand>("all")
                        .WithDescription("Get all virtual machine scale sets in all subscriptions and upgrade selected resources from a list to the latest model.");
                    upgrade.AddCommand<VMSSUpgradeSubscriptionCommand>("subscription")
                        .WithDescription("Get all virtual machine scale sets in a specific subscriptions and upgrade selected resources from a list.");

                });
                vmss.AddBranch("changeimage", changeimage =>
                {
                    //Description
                    changeimage.SetDescription("Change the image referenced with a virtual machine scale set from an image gallery.");
                    //Commands
                    changeimage.AddCommand<VMSSChangeImageAllCommand>("all")
                        .WithDescription("Get all virtual machine scale sets in all subscriptions and update the image reference from the selected image gallery, to one or more virtual machine scale sets.");
                    changeimage.AddCommand<VMSSChangeImageSubscriptionCommand>("subscription")
                        .WithDescription("Get all virtual machine scale sets in a specific subscriptions and update the image reference from selected image gallery, to one or more virtual machine scale sets.");

                });
                vmss.AddBranch("delete", delete =>
                {
                    //Description
                    delete.SetDescription("Delete commands associated with virtual machine scale sets");
                    //Commands
                    delete.AddCommand<VMSSDeleteAllCommand>("all")
                        .WithDescription("Get all virtual machine scale sets in all subscriptions and delete the selected resources from a list.");
                    delete.AddCommand<VMSSDeleteSubscriptionCommand>("subscription")
                        .WithDescription("Get all virtual machine scale sets in a specific subscriptions and delete the selected resources from a list.");

                });

                // VMSS Instances
                vmss.AddBranch("instance", instance =>
                {
                    //Description
                    instance.SetDescription("Virtual Machine Scale Set instances commands");
                    instance.AddBranch("list", list =>
                    {
                        //Description
                        list.SetDescription("List commands associated with virtual machine scale set instances");
                        //Commands
                        list.AddCommand<VMSSInstanceListAllCommand>("all")
                            .WithDescription("Get all virtual machine scale set instances in all subscriptions.");
                        list.AddCommand<VMSSInstanceListScriptionCommand>("subscription")
                        .WithDescription("Get all virtual machine scale set instances in a specific subscriptions.");

                    });
                    instance.AddBranch("start", start =>
                    {
                        //Description
                        start.SetDescription("Start commands associated with virtual machine scale set instances");
                        //Commands
                        start.AddCommand<VMSSInstanceStartAllCommand>("all")
                            .WithDescription("Get all stopped virtual machine scale set instances in all subscriptions and start selected resources from a list.");
                        start.AddCommand<VMSSInstanceStartSubscriptionCommand>("subscription")
                            .WithDescription("Get all stopped virtual machine scale set instances in a specific subscriptions and start selected resources from a list.");
                    });
                    instance.AddBranch("stop", stop =>
                    {
                        //Description
                        stop.SetDescription("Start commands associated with virtual machine scale set instances");
                        //Commands
                        stop.AddCommand<VMSSInstanceStopAllCommand>("all")
                            .WithDescription("Get all running virtual machine scale set instances in all subscriptions and stop selected resources from a list.");
                        stop.AddCommand<VMSSInstanceStopSubscriptionCommand>("subscription")
                            .WithDescription("Get all running virtual machine scale set instances in a specific subscriptions and stop selected resources from a list.");
                    });
                    instance.AddBranch("restart", restart =>
                    {
                        //Description
                        restart.SetDescription("Restart commands associated with virtual machine scale set instances");
                        //Commands
                        restart.AddCommand<VMSSInstanceRestartAllCommand>("all")
                            .WithDescription("Get all running virtual machine scale set instances in all subscriptions and restart selected resources from a list.");
                        restart.AddCommand<VMSSInstanceRestartSubscriptionCommand>("subscription")
                            .WithDescription("Get all running virtual machine scale set instances in a specific subscriptions and restart selected resources from a list.");
                    });
                    instance.AddBranch("reimage", reimage =>
                    {
                        //Description
                        reimage.SetDescription("Reimage commands associated with virtual machine scale set instances");
                        //Commands
                        reimage.AddCommand<VMSSInstanceReimageAllCommand>("all")
                            .WithDescription("Get all virtual machine scale set instances in all subscriptions and reimage selected resources from a list.");
                        reimage.AddCommand<VMSSInstanceReimageSubscriptionCommand>("subscription")
                            .WithDescription("Get all virtual machine scale set instances in a specific subscriptions and reimage selected resources from a list.");
                    });
                    instance.AddBranch("upgrade", upgrade =>
                    {
                        //Description
                        upgrade.SetDescription("Upgrade commands associated with virtual machine scale set instances.");
                        //Commands
                        upgrade.AddCommand<VMSSInstanceUpgradeAllCommand>("all")
                            .WithDescription("Get all virtual machine scale set instances in all subscriptions and upgrade selected resources from a list to the latest model.");
                        upgrade.AddCommand<VMSSInstanceUpgradeSubscriptionCommand>("subscription")
                            .WithDescription("Get all virtual machine scale sets in a specific subscriptions and upgrade selected resources from a list to the latest model..");

                    });
                });
            });

            config.AddBranch("aci", aci =>
            {
                //Description
                aci.SetDescription("Container Instance commands");
                aci.AddBranch("list", list =>
                {
                    //Description
                    list.SetDescription("List commands associated with container instances");
                    //Commands
                    list.AddCommand<ACIListAllCommand>("all")
                        .WithDescription("Get all container instances in all subscriptions.");
                    list.AddCommand<ACIListSubscriptionCommand>("subscription")
                        .WithDescription("Get all container instances in a specific subscriptions.");
                });
                aci.AddBranch("start", start =>
                {
                    //Description
                    start.SetDescription("Start commands associated with container instances");
                    //Commands
                    start.AddCommand<ACIStartAllCommand>("all")
                        .WithDescription("Get all stopped container instances in all subscriptions and start selected resources from a list.");
                    start.AddCommand<ACIStartSubscriptionCommand>("subscription")
                        .WithDescription("Get all stopped container instances in a specific subscriptions and start selected resources from a list.");
                });
                aci.AddBranch("stop", stop =>
                {
                    //Description
                    stop.SetDescription("Stop commands associated with container instances");
                    //Commands
                    stop.AddCommand<ACIStopAllCommand>("all")
                        .WithDescription("Get all running container instances in all subscriptions and stop selected resources from a list.");
                    stop.AddCommand<ACIStopSubscriptionCommand>("subscription")
                        .WithDescription("Get all running container instances in a specific subscriptions and stop selected resources from a list.");
                });
                aci.AddBranch("restart", restart =>
                {
                    //Description
                    restart.SetDescription("Restart commands associated with container instances");
                    //Commands
                    restart.AddCommand<ACIRestartAllCommand>("all")
                        .WithDescription("Get all running container instances in all subscriptions and restart selected resources from a list.");
                    restart.AddCommand<ACIRestartSubscriptionCommand>("subscription")
                        .WithDescription("Get all running container instances in a specific subscriptions and restart selected resources from a list.");
                });
                aci.AddBranch("delete", delete =>
                {
                    //Description
                    delete.SetDescription("Delete commands associated with container instances");
                    //Commands
                    delete.AddCommand<ACIDeleteAllCommand>("all")
                        .WithDescription("Get all container instances in all subscriptions and delete selected resources from a list.");
                    delete.AddCommand<ACIDeleteSubscriptionCommand>("subscription")
                        .WithDescription("Get all container instances in a specific subscriptions and delete selected resources from a list.");
                });
                aci.AddBranch("getlogs", getlogs =>
                {
                    //Description
                    getlogs.SetDescription("Get logs commands for container instances");
                    //Commands
                    getlogs.AddCommand<ACIGetLogsAllCommand>("all")
                        .WithDescription("Get all container instances in all subscriptions and fetches logs for selected resources from a list.");
                    getlogs.AddCommand<ACIGetLogsSubscriptionCommand>("subscription")
                        .WithDescription("Get all container instances in a specific subscriptions and fetches logs for selected resources from a list.");
                });

            });

            config.AddBranch("imagegallery", imagegallery =>
            {
                //Description
                imagegallery.SetDescription("Image Gallery commands");
                imagegallery.AddBranch("list", list =>
                {
                    //Description
                    list.SetDescription("List commands associated with container instances");
                    //Commands
                    list.AddCommand<IGListAllCommand>("all")
                        .WithDescription("Get all image galleies in all subscriptions.");
                });
                imagegallery.AddBranch("images", images =>
                {
                    //Description
                    images.SetDescription("Image Gallery images commands");
                    images.AddBranch("list", list =>
                    {
                        //Description
                        list.SetDescription("List commands associated with image gallery images");
                        //Commands
                        list.AddCommand<IGImagesListAllCommand>("all")
                            .WithDescription("Get all images from a selected image gallery.");

                    });
                });
            });

            config.AddBranch("mg", mg =>
            {
                //Description
                mg.SetDescription("Management Group commands");
                mg.AddCommand<MGShowCommand>("show")
                    .WithDescription("Shows the Management Group hierarchy.");
            });

            config.AddBranch("apim", apim =>
            {
                //Description
                apim.SetDescription("API Mangagenment Service commands");
                apim.AddBranch("list", list =>
                {
                    //Description
                    list.SetDescription("List commands associated with API Management Services");
                    //Commands
                    list.AddCommand<APIManagementListCommand>("all")
                        .WithDescription("Get all API Management Services in all subscriptions.");
                    list.AddCommand<APIManagementListSubscriptionCommand>("subscription")
                        .WithDescription("Get all container instances in a specific subscriptions.");
                });
                apim.AddBranch("backup", backup =>
                {
                    //Description
                    backup.SetDescription("Backup commands associated with API Management Services");
                    //Commands
                    backup.AddCommand<APIMBackupAllCommand>("all")
                        .WithDescription("Get all API Management Services in all subscriptions and backup the selected one to a storage account.\n\n" +
                        "[red]NOTE[/]\n" +
                        "The API Managment resource needs a Managed Identity or a User Assigned identity with at least [yellow]Storage Blob Data Contributor[/] on the target storage account.\n" +
                        "The logged in user performig the backup must have at least [yellow]'Microsoft.ApiManagement/service/backup/action'[/] on the API Managment resource.\n\n" +
                        "[blue]INFO[/]\n" +
                        "A blob container with the name [green]apim-backup[/] will be created under the storage account and the backup file will have the name [green]'<APIM Resource Name> Backup yyyy-MM-dd HH:mm'[/]");
                    backup.AddCommand<APIMBackupSubscriptionCommand>("subscription")
                        .WithDescription("Get all API Management Services in a specific subscriptions and backup the selected one to a storage account.\n\n" +
                        "[red]NOTE[/]\n" +
                        "The API Managment resource needs a Managed Identity or a User Assigned identity with at least [yellow]Storage Blob Data Contributor[/] on the target storage account.\n" +
                        "The logged in user performig the backup must have at least [yellow]'Microsoft.ApiManagement/service/backup/action'[/] on the API Managment resource.\n\n" +
                        "[blue]INFO[/]\n" +
                        "A blob container with the name [green]apim-backup[/] will be created under the storage account and the backup file will have the name [green]'<APIM Resource Name> Backup yyyy-MM-dd HH:mm'[/]");
                });
            });

            ///Command
            config.AddCommand<InfoCommand>("info")
            .WithDescription("Show info about this neat little tool.");
        });

        return await app.RunAsync(args);
    }
}
