using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ContainerInstance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Models
{
    public class ApiManagementServiceResourceExtended
    {
        public ApiManagementServiceResource APIManagementService { get; set; }
        public string SubscriptionName { get; set; }
    }
}
