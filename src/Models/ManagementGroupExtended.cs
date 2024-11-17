using Azure.ResourceManager.ManagementGroups.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Models
{
    public class ManagementGroupExtended
    {
        public string DisplayName { get; set; }
        public string Parent { get; set; }
        public List<ManagementGroupChildInfo> Children { get; set; }
    }
}
