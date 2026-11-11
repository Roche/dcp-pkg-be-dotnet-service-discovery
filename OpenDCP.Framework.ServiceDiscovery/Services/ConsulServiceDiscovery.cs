using Consul;
using OpenDCP.Framework.Configuration.Model.Entities.ModuleSettings;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenDCP.Framework.ServiceDiscovery.Services
{
    public class ConsulServiceDiscovery : IServiceDiscovery
    {
        private readonly IConsulClient _consulClient;

        public ConsulServiceDiscovery(IConsulClient consul)
        {
            _consulClient = consul;
        }

        public async Task<List<RegisteredService>> QueryActiveServices(List<string> tags, string env, bool? health = null)
        {
            if (string.IsNullOrEmpty(env))
            {
                throw new ArgumentNullException("An environment is required.");
            }
            else
            {
                tags.Add(env);
            }

            var services = await _consulClient.Agent.Services();

            var candidates = services.Response.Values
                .Where(x => tags.All(t => x.Tags.Contains(t)))
                .Select(x => new RegisteredService
                {
                    Id = x.ID,
                    Host = x.Address,
                    Port = x.Port,
                    ServiceName = x.Service,
                    Tags = x.Tags.ToList(),
                    MetaData = new ModuleMetaData
                    {
                        CalculationNodeLibraries = DeserializeOrDefault<string>(x.Meta, "CalculationNodeLibraries"),
                        ModuleName = x.Meta.ContainsKey("ModuleName") ? x.Meta["ModuleName"] : null,
                        DataTypes = DeserializeOrDefault<ModuleDataTypeConfigurationDTO>(x.Meta, "DataTypes"),
                        Resources = DeserializeOrDefault<ModuleResourceDTO>(x.Meta, "Resources")
                    }
                }).ToList();

            if (health == null) return candidates;

            var healthyIds = new HashSet<string>();

            foreach (var serviceName in candidates.Select(x => x.ServiceName).Distinct())
            {
                var healthResult = await _consulClient.Health.Service(
                    serviceName,
                    tag: null,
                    passingOnly: false);

                foreach (var entry in healthResult.Response)
                {
                    bool isPassing = entry.Checks.All(c => c.Status == HealthStatus.Passing);

                    if (isPassing) healthyIds.Add(entry.Service.ID);
                }
            }

            return candidates
                .Where(x => health == true ? healthyIds.Contains(x.Id) : !healthyIds.Contains(x.Id))
                .ToList();
        }

        private static List<T> DeserializeOrDefault<T>(IDictionary<string, string> meta, string key)
        {
            if (meta.TryGetValue(key, out var value))
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        Converters =
                        {
                            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
                        }
                    };

                    return JsonSerializer.Deserialize<List<T>>(value, options);
                }
                catch
                {
                    // Return an empty list if deserialization fails
                    return new List<T>();
                }
            }
            return new List<T>();
        }

    }
}
