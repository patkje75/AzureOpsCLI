using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;
using AzureOpsCLI.Interfaces;
using AzureOpsCLI.Models;
using Spectre.Console;

namespace AzureOpsCLI.Services
{
    public class ImageGalleryService : IImageGalleryService
    {
        private ArmClient _armClient;

        public ImageGalleryService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }


        public async Task<List<GalleryResourceExtended>> FetchAllImageGalleriesAsync()
        {
            List<GalleryResourceExtended> galleryList = new List<GalleryResourceExtended>();
            try
            {
                await AnsiConsole.Status()
                   .StartAsync("Fetching all subscriptions...", async ctx =>
                   {
                       await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                       {
                           ctx.Status($"Fetching Image Galleries in {subscription.Data.DisplayName}...");

                           var galleries = subscription.GetGalleriesAsync();
                           await foreach (var gallery in galleries)
                           {
                               galleryList.Add(new GalleryResourceExtended
                               {
                                   gallery = gallery,
                                   SubscriptionName = subscription.Data.DisplayName
                               });
                           }
                       }
                   });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching image galleries: {ex.Message}[/]");
            }

            return galleryList;

        }

        public async Task<List<GalleryResourceExtended>> FetchImageGalleriesSubscriptionAsync(string subscriptionId)
        {
            List<GalleryResourceExtended> galleryList = new List<GalleryResourceExtended>();
            try
            {
                SubscriptionResource subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);

                await AnsiConsole.Status()
                .StartAsync($"Fetching all image galleries in subscription {subscription.Data.DisplayName}...", async ctx =>
                {
                    var galleries = subscription.GetGalleriesAsync();
                    await foreach (var gallery in galleries)
                    {
                        try
                        {
                            galleryList.Add(new GalleryResourceExtended
                            {
                                gallery = gallery,
                                SubscriptionName = subscription.Data.DisplayName
                            });
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Error fetching image gallery {gallery.Data.Name}: {ex.Message}[/]");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching image galleries for subscription {subscriptionId}: {ex.Message}[/]");
            }
            return galleryList;
        }

        public async Task<List<GalleryImageResource>> ListImagesInGalleryAsync(GalleryResourceExtended gallery)
        {
            List<GalleryImageResource> images = new List<GalleryImageResource>();

            try
            {
                await AnsiConsole.Status()
                   .StartAsync("Fetching gallery images...", async ctx =>
                   {
                       var galleryImages = gallery.gallery.GetGalleryImages();

                       await foreach (var image in galleryImages)
                       {
                           images.Add(image);
                       }
                   });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching images: {ex.Message}[/]");
            }

            return images;
        }

        public async Task<List<GalleryImageVersionResource>> ListImageVersionsAsync(GalleryImageResource image)
        {
            List<GalleryImageVersionResource> versions = new List<GalleryImageVersionResource>();

            try
            {
                await AnsiConsole.Status()
                  .StartAsync("Fetching image versions...", async ctx =>
                  {
                      await foreach (var version in image.GetGalleryImageVersions().GetAllAsync())
                      {
                          versions.Add(version);
                      }
                  });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching image versions: {ex.Message}");
            }

            return versions;
        }
    }
}
