using Azure.ResourceManager.Compute;
using AzureOpsCLI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
