using System.Text.Json.Serialization;
using XTower.Persistence;

namespace XTower.Models.Editor
{
    [JsonSerializable(typeof(EditorStateModel))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class EditorStateJsonContext : JsonSerializerContext;

    internal sealed class EditorStateModel
    {
        public string? LastLevelId { get; set; }

        public string? LastPathId { get; set; }
    }
}
