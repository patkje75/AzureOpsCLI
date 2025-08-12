using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;

namespace AzureOpsCLI.Commands.Info
{
    internal class InfoCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            string azureAscii =
            "[blue] █████╗ ███████╗██╗   ██╗██████╗ ███████╗[/]     [green]██████╗ ██████╗ ███████╗[/]  [blue]   ██████╗██╗     ██╗[/]\n" +
            "[blue]██╔══██╗╚══███╔╝██║   ██║██╔══██╗██╔════╝[/]    [green]██╔═══██╗██╔══██╗██╔════╝[/]  [blue]  ██╔════╝██║     ██║[/]\n" +
            "[blue]███████║  ███╔╝ ██║   ██║██████╔╝█████╗  [/]    [green]██║   ██║██████╔╝███████╗[/]  [blue]  ██║     ██║     ██║[/]\n" +
            "[blue]██╔══██║ ███╔╝  ██║   ██║██╔══██╗██╔══╝  [/]    [green]██║   ██║██╔═══╝ ╚════██║[/]  [blue]  ██║     ██║     ██║[/]\n" +
            "[blue]██║  ██║███████╗╚██████╔╝██║  ██║███████╗[/]    [green]╚██████╔╝██║     ███████║[/]  [blue]  ╚██████╗███████╗██║[/]\n" +
            "[blue]╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚═╝  ╚═╝╚══════╝[/]     [green]╚═════╝ ╚═╝     ╚══════╝[/]  [blue]   ╚═════╝╚══════╝╚═╝[/]\n" +

            $"[green] Release {Assembly.GetExecutingAssembly().GetName().Version}[/]";

            AnsiConsole.MarkupLine(azureAscii);
            var root = new Tree("[yellow]Command Hierarchy[/]")
                .Style("yellow");

            var aci = root.AddNode("[blue]aci[/]");
            aci.AddNode("[green]delete[/] [lightseagreen]all | subscription[/]");
            aci.AddNode("[green]getlogs[/]  [lightseagreen]all | subscription[/] ([purple4]--console / --file[/])");
            aci.AddNode("[green]list[/]  [lightseagreen]all | subscription[/]");
            aci.AddNode("[green]restart[/]  [lightseagreen]all | subscription[/]");
            aci.AddNode("[green]start[/]  [lightseagreen]all | subscription[/]");
            aci.AddNode("[green]stop[/]  [lightseagreen]all | subscription[/]");

            var imageGallery = root.AddNode("[blue]imagegallery[/]");
            imageGallery.AddNode("[green]list[/] [lightseagreen]all | subscription[/]");

            var imageGalleryImages = imageGallery.AddNode("[blue]images[/]");
            imageGalleryImages.AddNode("[green]list[/] [lightseagreen]all | subscription[/]");

            var rg = root.AddNode("[blue]rg[/]");
            rg.AddNode("[green]create[/] [lightseagreen]subscription[/] ([purple4]--name --location[/])");
            rg.AddNode("[green]list[/] [lightseagreen]all | subscription[/]");

            var tags = root.AddNode("[blue]tags[/]");
            tags.AddNode("[green]apply[/] [lightseagreen]all | subscription[/]");
            tags.AddNode("[green]export[/] [lightseagreen]all | subscription[/]");
            tags.AddNode("[green]list[/] [lightseagreen]all | subscription[/]");
            tags.AddNode("[green]remove[/] [lightseagreen]all | subscription[/]");

            var vm = root.AddNode("[blue]vm[/] [lightseagreen]all | subscription[/]");
            vm.AddNode("[green]delete[/] [lightseagreen]all | subscription[/]");
            vm.AddNode("[green]list[/] [lightseagreen]all | subscription[/]");
            vm.AddNode("[green]restart[/] [lightseagreen]all | subscription[/]");
            vm.AddNode("[green]start[/] [lightseagreen]all | subscription[/]");
            vm.AddNode("[green]stop[/] [lightseagreen]all | subscription[/]");

            var vmss = root.AddNode("[blue]vmss[/]");
            vmss.AddNode("[green]changeimage[/] [lightseagreen]all | subscription[/] ([purple4]--upgrade[/])");
            vmss.AddNode("[green]delete[/] [lightseagreen]all | subscription[/]");
            vmss.AddNode("[green]list[/] [lightseagreen]all | subscription[/]");
            vmss.AddNode("[green]reimage[/] [lightseagreen]all | subscription[/]");
            vmss.AddNode("[green]restart[/] [lightseagreen]all | subscription[/]");
            vmss.AddNode("[green]start[/] [lightseagreen]all | subscription[/]");
            vmss.AddNode("[green]stop[/] [lightseagreen]all | subscription[/]");
            vmss.AddNode("[green]upgrade[/] [lightseagreen]all | subscription[/]");

            var vmssInstance = vmss.AddNode("[blue]instance[/]");
            vmssInstance.AddNode("[green]list[/] [lightseagreen]all | subscription[/]");
            vmssInstance.AddNode("[green]reimage[/] [lightseagreen]all | subscription[/]");
            vmssInstance.AddNode("[green]restart[/] [lightseagreen]all | subscription[/]");
            vmssInstance.AddNode("[green]start[/] [lightseagreen]all | subscription[/]");
            vmssInstance.AddNode("[green]stop[/] [lightseagreen]all | subscription[/]");
            vmssInstance.AddNode("[green]upgrade[/] [lightseagreen]all | subscription[/]");

            var mg = root.AddNode("[blue]mg[/]");
            mg.AddNode("[green]show[/] ([purple4]--console / --exportmermaid[/])");

            var apim = root.AddNode("[blue]apim[/]");
            apim.AddNode("[green]list[/]  [lightseagreen]all | subscription[/]");
            apim.AddNode("[green]backup[/]  [lightseagreen]all | subscription[/]");

            AnsiConsole.Write(root);

            return 0;
        }
    }
}
