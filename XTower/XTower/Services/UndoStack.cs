using System.Collections.Generic;

namespace XTower.Services
{
    internal sealed class UndoStack<T> where T : class
    {
        private readonly Stack<T> _undo = new();
        private readonly Stack<T> _redo = new();

        public void Push(T snapshot)
        {
            _undo.Push(snapshot);
            _redo.Clear();
        }

        public T? Undo(T current)
        {
            if (_undo.Count == 0)
                return null;

            _redo.Push(current);
            return _undo.Pop();
        }

        public T? Redo(T current)
        {
            if (_redo.Count == 0)
                return null;

            _undo.Push(current);
            return _redo.Pop();
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }

        public bool CanUndo => _undo.Count > 0;

        public bool CanRedo => _redo.Count > 0;
    }
}
