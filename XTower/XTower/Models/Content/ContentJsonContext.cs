using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace XTower.Models.Content
{
    [JsonSerializable(typeof(ProjectConfig))]
    [JsonSerializable(typeof(LevelDefinition))]
    [JsonSerializable(typeof(LevelIndex))]
    [JsonSerializable(typeof(GridConfig))]
    [JsonSerializable(typeof(GridPoint))]
    [JsonSerializable(typeof(PathDefinition))]
    [JsonSerializable(typeof(List<PathDefinition>))]
    [JsonSerializable(typeof(PathConfig))]
    [JsonSerializable(typeof(List<GridPoint>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class ContentJsonContext : JsonSerializerContext;
}
