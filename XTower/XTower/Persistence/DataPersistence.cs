using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace XTower.Persistence
{
    internal sealed class DataPersistence : IDataPersistence
    {
        private readonly ILogger<DataPersistence> _logger;
        private readonly string _rootDirectory;

        public DataPersistence(ILogger<DataPersistence> logger)
        {
            _logger = logger;
            _rootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "XTower",
                "LocalData");

            Directory.CreateDirectory(_rootDirectory);
        }

        public string GetAppStorageBasePath() => _rootDirectory;

        public string GetAppStoragePath(string fileName) => Path.Combine(_rootDirectory, fileName);

        public T? Load<T>(string fileName, JsonTypeInfo<T> typeInfo) where T : class
        {
            var path = GetAppStoragePath(fileName);

            if (!File.Exists(path))
                return null;

            try
            {
                if (path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var line in File.ReadLines(path, Encoding.UTF8))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        return JsonSerializer.Deserialize(line, typeInfo);
                    }

                    return null;
                }

                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize(json, typeInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load {FileName}", fileName);
                return null;
            }
        }

        public void Save<T>(string fileName, T data, JsonTypeInfo<T> typeInfo) where T : class
        {
            var path = GetAppStoragePath(fileName);

            try
            {
                Directory.CreateDirectory(_rootDirectory);

                var json = JsonSerializer.Serialize(data, typeInfo);

                if (path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                    File.WriteAllText(path, json + Environment.NewLine, Encoding.UTF8);
                else
                    File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save {FileName}", fileName);
            }
        }
    }
}
