using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;

namespace OpenDCP.Framework.ServiceDiscovery.Services
{
    public class FileServiceDiscoveryConfiguration : IServiceDiscoveryConfiguration
    {
        public IConfiguration AddSharedConfiguration(IServiceCollection services, IConfiguration configuration, SharedConfigurationOptions options)
        {
            throw new System.NotImplementedException();
        }

        public IServiceCollection ConfigureServiceDiscovery(IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public IApplicationBuilder UseServiceDiscovery(IApplicationBuilder app, IConfiguration configuration, ModuleMetaData moduleMetaData)
        {

            var path = configuration.GetValue<string>("ServiceDiscoveryProvider:Settings:Path");

            var servicePort = int.Parse(configuration.GetValue<string>("ServiceDiscoveryProvider:ServiceConfiguration:Port"));
            var serviceName = configuration.GetValue<string>("ServiceDiscoveryProvider:ServiceConfiguration:ServiceName");
            var serviceAddress = configuration.GetValue<string>("ServiceDiscoveryProvider:ServiceConfiguration:Address");

            var serviceId = serviceName + "-" + GenerateServiceId(serviceName, servicePort);

            var configFile = File.Exists(path) ? File.ReadAllText(path) : "[]";
            var serviceConfiguration = JArray.Parse(configFile);

            var serviceRegistration = new RegisteredService
            {
                Id = serviceId,
                ServiceName = serviceName,
                Host = serviceAddress,
                Tags = configuration.GetSection("ServiceDiscoveryProvider:ServiceConfiguration:Tags").Get<string[]>().ToList(),
                MetaData = new ModuleMetaData(),
                Port = servicePort
            };

            serviceConfiguration = JArray.FromObject(serviceConfiguration.Where(x => (string)x["ServiceName"] != serviceName).ToList());

            serviceConfiguration.Add(serviceRegistration);
            File.WriteAllText(path, serviceConfiguration.ToString());

            return app;
        }

        private string GenerateServiceId(string address, int port)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(address + port);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
