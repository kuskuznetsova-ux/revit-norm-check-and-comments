using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace RevitNormCheck.Settings
{
    [DataContract]
    public class PluginSettings
    {
        [DataMember] public string NormsFolder { get; set; } = "";
        [DataMember] public string CompanyStandardsFolder { get; set; } = "";
        [DataMember] public string DocumentationFolder { get; set; } = "";
        [DataMember] public List<string> EnabledCheckIds { get; set; } = new List<string>();

        // "CurrentView", "Selected" или "WholeModel".
        [DataMember] public string ScopeMode { get; set; } = "CurrentView";
        [DataMember] public List<string> ScopeUniqueIds { get; set; } = new List<string>();

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RevitNormCheck", "settings.json");

        public static PluginSettings Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new PluginSettings();
                using (var stream = File.OpenRead(FilePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(PluginSettings));
                    return (PluginSettings)serializer.ReadObject(stream) ?? new PluginSettings();
                }
            }
            catch
            {
                return new PluginSettings();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            using (var stream = File.Create(FilePath))
            {
                new DataContractJsonSerializer(typeof(PluginSettings)).WriteObject(stream, this);
            }
        }
    }
}
