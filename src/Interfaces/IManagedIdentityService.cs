using Azure.ResourceManager.ManagedServiceIdentities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpsCLI.Interfaces
{
    internal interface IManagedIdentityService<T>
    {
        Task<List<SystemAssignedIdentityResource>> GetManagedIdentitiesAsync(T resource);
    }
}
