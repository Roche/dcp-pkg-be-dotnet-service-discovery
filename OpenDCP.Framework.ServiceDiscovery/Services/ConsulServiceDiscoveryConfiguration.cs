using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;

namespace OpenDCP.Framework.ServiceDiscovery.Services
{
    public class ConsulServiceDiscoveryConfiguration : IServiceDiscoveryConfiguration
    {
        public IServiceCollection ConfigureServiceDiscovery(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConsulClient, ConsulClient>(p => GetConsulClient(configuration));
            return services;
        }

        public IConfiguration AddSharedConfiguration(IServiceCollection services, IConfiguration configuration, SharedConfigurationOptions options)
        {
            var consulClient = GetConsulClient(configuration);

            if (options.AddHeaders)
            {
                configuration = AddSharedConfigurationSection(
                    configuration: configuration,
                    path: configuration["ServiceDiscoveryProvider:Settings:SharedConfig:Paths:Headers"],
                    sectionKey: "Headers",
                    consulClient: consulClient
                 );
            }

            if (options.AddMemoryCache)
            {
                configuration = AddSharedConfigurationSection(
                    configuration: configuration,
                    path: configuration["ServiceDiscoveryProvider:Settings:SharedConfig:Paths:MemoryCache"],
                    sectionKey: "MemoryCache",
                    consulClient: consulClient
                 );
            }

            if (options.AddMessageBus)
            {
                configuration = AddSharedConfigurationSection(
                    configuration: configuration,
                    path: configuration["ServiceDiscoveryProvider:Settings:SharedConfig:Paths:MessageBus"],
                    sectionKey: "MessageBusProvider",
                    consulClient: consulClient
                 );
            }

            if (options.AddSecretProvider)
            {
                configuration = AddSharedConfigurationSection(
                    configuration: configuration,
                    path: configuration["ServiceDiscoveryProvider:Settings:SharedConfig:Paths:CredentialStore"],
                    sectionKey: "SecretConfigurationProvider",
                    consulClient: consulClient
                 );
            }

            if (options.AddIdP)
            {
                configuration = AddSharedConfigurationSection(
                    configuration: configuration,
                    path: configuration["ServiceDiscoveryProvider:Settings:SharedConfig:Paths:IdP"],
                    sectionKey: "IdP",
                    consulClient: consulClient
                 );
            }

            if (options.AddSupportInfo)
            {
                configuration = AddSharedConfigurationSection(
                    configuration: configuration,
                    path: configuration["ServiceDiscoveryProvider:Settings:SharedConfig:Paths:SupportInfo"],
                    sectionKey: "SupportInfo",
                    consulClient: consulClient
                 );
            }

            return configuration;
        }

        public IApplicationBuilder UseServiceDiscovery(IApplicationBuilder app, IConfiguration configuration, ModuleMetaData moduleMetaData)
        {
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
            var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("AppExtensions");
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

            /*if (!(app.Properties["server.Features"] is FeatureCollection features))
            {
                return app;
            }*/

            var servicePort = int.Parse(configuration.GetValue<string>("ServiceDiscoveryProvider:ServiceConfiguration:Port"));
            var serviceName = configuration.GetValue<string>("ServiceDiscoveryProvider:ServiceConfiguration:ServiceName");
            var serviceAddress = configuration.GetValue<string>("ServiceDiscoveryProvider:ServiceConfiguration:Address");

            var serviceId = serviceName + "-" + GenerateServiceId(serviceName, servicePort);

            var registration = new AgentServiceRegistration()
            {
                ID = serviceId,
                Name = serviceName,
                Address = serviceAddress,
                Tags = configuration.GetSection("ServiceDiscoveryProvider:ServiceConfiguration:Tags").Get<string[]>(),
                Meta = moduleMetaData.ToMetaDataDictionary(),
                Port = servicePort,
                Check = new AgentCheckRegistration()
                {
                    HTTP = $"https://{serviceAddress}:{servicePort}/health",
                    Interval = TimeSpan.FromSeconds(30),

                    // In Consul 0.7 and later, checks that are associated with a service may also contain an optional deregister_critical_service_after field,
                    // which is a timeout in the same Go time format as interval and ttl.
                    //If a check is in the critical state for more than this configured value, then its associated service(and all of its associated checks) will automatically be deregistered.
                    //The minimum timeout is 1 minute, and the process that reaps critical services runs every 30 seconds, so it may take slightly longer than the configured timeout to trigger the deregistration.
                    //This should generally be configured with a timeout that's much, much longer than any expected recoverable outage for the given service.
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(90)
                }
            };

            consulClient.Agent.ServiceRegister(registration).ConfigureAwait(true);

            //
            // This is ONLY called when the app get graceful shutdown (not receive any requests for given time (default for IIS 20 min))
            // Stopping IIS site, stopping IIS pool or error from which the application cannot recover is consider as terminate shutdown and this
            // method is NOT been called
            // For additional defense we put DeregisterCriticalServiceAfter in order to guarantee that app is been deregister from Consul
            //
            lifetime.ApplicationStopping.Register(() =>
            {
                consulClient.Agent.ServiceDeregister(registration.ID).ConfigureAwait(true);
            });

            return app;
        }

        private static ConsulClient GetConsulClient(IConfiguration configuration)
        {
            return new ConsulClient(config =>
            {
                var host = configuration.GetValue<string>("ServiceDiscoveryProvider:Settings:Address");
                var port = configuration.GetValue<int>("ServiceDiscoveryProvider:Settings:Port");
                var consulNamespace = configuration.GetValue<string>("ServiceDiscoveryProvider:Settings:Namespace", null);
                var consulToken = configuration.GetValue<string>("ServiceDiscoveryProvider:Settings:Token", null);

                config.Address = new Uri($"{host}:{port}");

                if (!string.IsNullOrEmpty(consulToken))
                {
                    config.Token = consulToken;
                }

                if (!string.IsNullOrEmpty(consulNamespace))
                {
                    config.Namespace = consulNamespace;
                }
            });
        }

        private IConfiguration AddSharedConfigurationSection(IConfiguration configuration, string path, string sectionKey, ConsulClient consulClient)
        {
            if (path == null)
            {
                throw new Exception("Shared configuration not found");
            }

            var kv = consulClient.KV.Get(path).Result;
            var configString = Encoding.UTF8.GetString(kv.Response.Value);
            var config = JsonDocument.Parse(configString).RootElement;

            configuration = AddKey2Configuration(config, sectionKey, configuration);

            return configuration;
        }

        private IConfiguration AddKey2Configuration(JsonElement el, string rootPath, IConfiguration configuration)
        {
            foreach (var child in el.EnumerateObject())
            {
                if (child.Value.ValueKind == JsonValueKind.String
                    || child.Value.ValueKind == JsonValueKind.Number)
                {
                    configuration[$"{rootPath}:{child.Name}"] = child.Value.ToString();
                }

                if (child.Value.ValueKind == JsonValueKind.Object)
                {
                    var path = $"{rootPath}:{child.Name}";
                    configuration = AddKey2Configuration(child.Value, path, configuration);
                }
            }

            return configuration;
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
