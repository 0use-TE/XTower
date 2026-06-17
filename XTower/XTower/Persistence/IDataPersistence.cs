using System.Text.Json.Serialization.Metadata;

namespace XTower.Persistence
{
    internal interface IDataPersistence
    {
        void Save<T>(string fileName, T data, JsonTypeInfo<T> typeInfo) where T : class;

        T? Load<T>(string fileName, JsonTypeInfo<T> typeInfo) where T : class;

        string GetAppStoragePath(string fileName);

        string GetAppStorageBasePath();
    }
}
