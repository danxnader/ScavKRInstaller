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
    public class SetupVersions
    {
        [JsonPropertyName("SetupVersions")]
        public Dictionary<string, VersionInfo> Versions { get; set; } = new();
        public void FromDTO(SetupVersionsDTO dto)
        {
            foreach(Dictionary<string, VersionInfoDTO> versionSet in dto.Versions)
            {
                foreach(KeyValuePair<string, VersionInfoDTO> kvp in versionSet)
                {
                    string versionKey = kvp.Key;
                    VersionInfoDTO versionDTO = kvp.Value;
                    Dictionary<string, Extra> extras = new Dictionary<string, Extra>();
                    foreach(KeyValuePair<string, ExtraDTO> extraKvp in versionDTO.Extras)
                    {
                        ExtraDTO extraDTO = extraKvp.Value;
                        List<string> extraUrls = new List<string>();
                        if(dto.URLs.ExtensionData != null && dto.URLs.ExtensionData.TryGetValue(extraDTO.Name, out JsonElement categoryElement) && categoryElement.ValueKind == JsonValueKind.Object)
                        {
                            if(categoryElement.TryGetProperty(extraDTO.Version, out JsonElement urlsArray) && urlsArray.ValueKind == JsonValueKind.Array)
                            {
                                foreach(var item in urlsArray.EnumerateArray())
                                {
                                    if(item.ValueKind == JsonValueKind.String)
                                        extraUrls.Add(item.GetString()!);
                                }
                            }
                        }

                        extras[extraDTO.Name] = new Extra
                        {
                            Name = extraDTO.Name,
                            URLs = extraUrls
                        };
                    }
                    var gameUrls = dto.URLs.Game[versionDTO.Game];
                    var mpUrls = dto.URLs.MultiplayerMod[versionDTO.MultiplayerMod];
                    var bepinUrls = dto.URLs.Bepin[versionDTO.Bepin];
                    Versions[versionKey] = new VersionInfo
                    {
                        Name = versionDTO.Name,
                        Game = gameUrls,
                        MultiplayerMod = mpUrls,
                        Bepin = bepinUrls,
                        Extras = extras
                    };
                }
            }
        }

        public class VersionInfo
        {
            [JsonPropertyName("Name")]
            public required string Name { get; set; }

            [JsonPropertyName("Game")]
            public required List<string> Game { get; set; }

            [JsonPropertyName("MultiplayerMod")]
            public required List<string> MultiplayerMod { get; set; }

            [JsonPropertyName("Bepin")]
            public required List<string> Bepin { get; set; }

            [JsonPropertyName("Extras")]
            public required Dictionary<string, Extra> Extras { get; set; }
        }
        public class Extra
        {
            [JsonPropertyName("Name")]
            public required string Name { get; set; }
            [JsonPropertyName("Version")]
            public required List<string> URLs { get; set; }
        }
    }
}
    