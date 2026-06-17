using System.Text.Json.Serialization;

namespace XTower.Models.Content
{
    [JsonSerializable(typeof(WorkspaceStorageModel))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class WorkspaceJsonContext : JsonSerializerContext;

    internal sealed class WorkspaceStorageModel
    {
        public string? RootPath { get; set; }
    }
}
