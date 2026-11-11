using System.Collections.Generic;

namespace OpenDCP.Framework.ServiceDiscovery.Models
{
    public class RegisteredService
    {
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public List<string> Tags { get; set; }
        public ModuleMetaData MetaData { get; set; } = new ModuleMetaData();
    }
}
