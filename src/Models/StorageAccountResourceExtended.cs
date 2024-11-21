using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Models
{
    public class StorageAccountResourceExtended
    {
        public StorageAccountResource StorageAccountResource { get; set; }
        public string SubscriptionName { get; set; }
    }
}
