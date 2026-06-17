using XTower.Models.Editor;
using XTower.Persistence;

namespace XTower.Services
{
    internal interface IEditorStateService
    {
        string? LastLevelId { get; }

        string? LastPathId { get; }

        void RememberLevel(string levelId);

        void RememberPath(string pathId);
    }

    internal sealed class EditorStateService : IEditorStateService
    {
        private const string StorageName = "editor-state.json";

        private readonly IDataPersistence _dataPersistence;
        private EditorStateModel _state;

        public EditorStateService(IDataPersistence dataPersistence)
        {
            _dataPersistence = dataPersistence;
            _state = _dataPersistence.Load(StorageName, EditorStateJsonContext.Default.EditorStateModel)
                ?? new EditorStateModel();
        }

        public string? LastLevelId => _state.LastLevelId;

        public string? LastPathId => _state.LastPathId;

        public void RememberLevel(string levelId)
        {
            _state.LastLevelId = levelId;
            Save();
        }

        public void RememberPath(string pathId)
        {
            _state.LastPathId = pathId;
            Save();
        }

        private void Save() =>
            _dataPersistence.Save(StorageName, _state, EditorStateJsonContext.Default.EditorStateModel);
    }
}
