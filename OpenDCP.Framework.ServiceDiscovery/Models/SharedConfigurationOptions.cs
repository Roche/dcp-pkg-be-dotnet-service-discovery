namespace OpenDCP.Framework.ServiceDiscovery.Models
{
    public class SharedConfigurationOptions
    {
        public bool AddMessageBus { get; set; } = false;
        public bool AddSecretProvider { get; set; } = true;
        public bool AddHeaders { get; set; } = true;
        public bool AddMemoryCache { get; set; } = true;
        public bool AddIdP { get; set; } = true;
        public bool AddSupportInfo { get; set; } = false;
    }
}
