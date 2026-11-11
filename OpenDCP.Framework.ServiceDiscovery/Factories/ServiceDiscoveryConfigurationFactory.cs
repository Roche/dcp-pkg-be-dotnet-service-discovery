using OpenDCP.Framework.ServiceDiscovery.Services;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using System;

namespace OpenDCP.Framework.ServiceDiscovery.Factories
{
    public class ServiceDiscoveryConfigurationFactory
    {
        public IServiceDiscoveryConfiguration Load(string driver)
        {
            switch (driver.ToLower())
            {
                case "hashicorpconsul":
                    return new ConsulServiceDiscoveryConfiguration();
                case "file":
                    return new FileServiceDiscoveryConfiguration();
                default:
                    throw new Exception($"The driver {driver} is not known for service discovery");
            }
        }
    }
}
