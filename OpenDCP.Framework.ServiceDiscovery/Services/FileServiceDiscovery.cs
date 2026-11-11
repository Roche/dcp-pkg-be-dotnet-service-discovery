using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenDCP.Framework.ServiceDiscovery.Services
{
    public class FileServiceDiscovery : IServiceDiscovery
    {
        private readonly IConfiguration _configuration;

        public FileServiceDiscovery(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<List<RegisteredService>> QueryActiveServices(List<string> tags, string env, bool? health = null)
        {
            var path = _configuration["ServiceDiscoveryProvider:Settings:Path"];

            var configFile = File.Exists(path) ? File.ReadAllText(path) : "[]";
            var serviceConfiguration = JArray.Parse(configFile);

            var result = serviceConfiguration.Select(x => new RegisteredService
            {
                Host = (string)x["Address"],
                Port = (int)x["Port"],
                ServiceName = (string)x["ServiceName"],
                Tags = x["Tags"].ToObject<List<string>>()
            }).ToList();

            if (tags.Any())
            {
                result = result.Where(x => tags.All(t => x.Tags.Contains(t))).ToList();
            }

            return Task.FromResult(result);
        }
    }
}
