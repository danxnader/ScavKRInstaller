using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScavKRInstaller
{
    public class SetupVersionsDTO
    {
        [JsonPropertyName("SetupVersions")]
        public List<Dictionary<string, VersionInfoDTO>> Versions { get; set; }

        [JsonPropertyName("URLs")]
        public UrlsDTO URLs { get; set; }
    }

    public class VersionInfoDTO
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Game")]
        public string Game { get; set; }

        [JsonPropertyName("MultiplayerMod")]
        public string MultiplayerMod { get; set; }

        [JsonPropertyName("Bepin")]
        public string Bepin { get; set; }

        [JsonPropertyName("Extras")]
        public Dictionary<string, ExtraDTO> Extras { get; set; }
    }

    public class UrlsDTO
    {
        [JsonPropertyName("Game")]
        public Dictionary<string, List<string>> Game { get; set; }

        [JsonPropertyName("MultiplayerMod")]
        public Dictionary<string, List<string>> MultiplayerMod { get; set; }

        [JsonPropertyName("Bepin")]
        public Dictionary<string, List<string>> Bepin { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
    public class ExtraDTO
    {
        [JsonPropertyName("Name")]
        public required string Name { get; set; }
        [JsonPropertyName("Version")]
        public required string Version { get; set; }
    }  
}
    