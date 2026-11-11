using OpenDCP.Framework.Configuration.Model.Entities.ModuleSettings;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenDCP.Framework.ServiceDiscovery.Models
{
    public class ModuleMetaData
    {
        public string ModuleName { get; set; }
        public List<string> CalculationNodeLibraries { get; set; } = new List<string>();
        public List<ModuleDataTypeConfigurationDTO> DataTypes { get; set; } = new List<ModuleDataTypeConfigurationDTO>();
        public List<ModuleResourceDTO> Resources { get; set; } = new List<ModuleResourceDTO>();
        public List<string> Permissions { get; set; } = new List<string>();

        // Method to serialize the class to Dictionary<string, string>
        public Dictionary<string, string> ToMetaDataDictionary()
        {
            var metaData = new Dictionary<string, string>();

            // Convert RPackages to a single JSON string and add to dictionary
            if (CalculationNodeLibraries != null && CalculationNodeLibraries.Count > 0)
            {
                metaData.Add("CalculationNodeLibraries", JsonConvert.SerializeObject(CalculationNodeLibraries));
            }

            // Convert DataTypes to a JSON string and add to dictionary
            if (DataTypes != null && DataTypes.Count > 0)
            {
                metaData.Add("DataTypes", JsonConvert.SerializeObject(DataTypes));
            }

            // Convert Resources to a JSON string and add to dictionary
            if (Resources != null && Resources.Count > 0)
            {
                metaData.Add("Resources", JsonConvert.SerializeObject(Resources));
            }

            // Convert Permissions to a single JSON string and add to dictionary
            if (Permissions != null && Permissions.Count > 0)
            {
                metaData.Add("Permissions", JsonConvert.SerializeObject(Permissions));
            }

            return metaData;
        }
    }
}
