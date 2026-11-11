using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenDCP.Framework.ServiceDiscovery.Models;

namespace OpenDCP.Framework.ServiceDiscovery.Interfaces
{
    public interface IServiceDiscoveryConfiguration
    {
        IServiceCollection ConfigureServiceDiscovery(IServiceCollection services, IConfiguration configuration);
        IConfiguration AddSharedConfiguration(IServiceCollection services, IConfiguration configuration, SharedConfigurationOptions options);
        IApplicationBuilder UseServiceDiscovery(IApplicationBuilder app, IConfiguration configuration, ModuleMetaData moduleMetaData);
    }
}
