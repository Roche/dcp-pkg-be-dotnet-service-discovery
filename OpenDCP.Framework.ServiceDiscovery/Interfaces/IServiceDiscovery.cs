using OpenDCP.Framework.ServiceDiscovery.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Framework.ServiceDiscovery.Interfaces
{
    public interface IServiceDiscovery
    {
        Task<List<RegisteredService>> QueryActiveServices(List<string> tags, string env, bool? health = null);
    }
}
