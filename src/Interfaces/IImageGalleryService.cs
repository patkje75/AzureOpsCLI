using Azure.ResourceManager.Compute;
using AzureOpsCLI.Models;

namespace AzureOpsCLI.Interfaces
{
    public interface IImageGalleryService
    {
        Task<List<GalleryResourceExtended>> FetchAllImageGalleriesAsync();
        Task<List<GalleryResourceExtended>> FetchImageGalleriesSubscriptionAsync(string subscriptionId);

        Task<List<GalleryImageResource>> ListImagesInGalleryAsync(GalleryResourceExtended gallery);

        Task<List<GalleryImageVersionResource>> ListImageVersionsAsync(GalleryImageResource image);

    }
}
