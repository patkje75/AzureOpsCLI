using AzureOpsCLI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Interfaces
{
    public interface IMGService
    {
        Task<List<ManagementGroupExtended>> FetchManagementGroupsAsync();
    }
}
