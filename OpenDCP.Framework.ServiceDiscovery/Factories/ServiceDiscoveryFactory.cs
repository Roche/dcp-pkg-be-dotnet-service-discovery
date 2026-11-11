using Consul;
using OpenDCP.Framework.ServiceDiscovery.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using System;

namespace OpenDCP.Framework.ServiceDiscovery.Factories
{
    public class ServiceDiscoveryFactory
    {
        public IServiceDiscovery Load(string driver, IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetService<IConfiguration>();

            switch (driver.ToLower())
            {
                case "hashicorpconsul":
                    var host = configuration.GetValue<string>("ServiceDiscoveryProvider:Settings:Address");
                    var port = int.Parse(configuration.GetValue<string>("ServiceDiscoveryProvider:Settings:Port"));
                    var client = new ConsulClient(opt =>
                    {
                        opt.Address = new Uri(host + ":" + port);
                        opt.Token = configuration["ServiceDiscoveryProvider:Settings:Token"];
                    });
                    return new ConsulServiceDiscovery(client);
                case "file":
                    return new FileServiceDiscovery(configuration);
                default:
                    throw new Exception($"The driver {driver} is not known for service discovery");
            }
        }
    }
}
