using System;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorUndoController : IDisposable
    {
        private readonly Action<string, string> _onUndoRedoApplied;
        private readonly TableEditorUndoState _state;

        public TableEditorUndoController(Action<string, string> onUndoRedoApplied)
        {
            _onUndoRedoApplied = onUndoRedoApplied;
            _state = ScriptableObject.CreateInstance<TableEditorUndoState>();
            _state.hideFlags = HideFlags.HideAndDontSave;
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
        }

        public void Initialize(string tableKey, TableEditorDocument document)
        {
            if (document == null)
                return;

            _state.TableKey = tableKey ?? string.Empty;
            _state.SnapshotJson = document.ToSnapshotJson();
            EditorUtility.SetDirty(_state);
        }

        public void BeginRecord(string actionName)
        {
            if (_state == null)
                return;

            Undo.RegisterCompleteObjectUndo(_state, actionName);
        }

        public void Commit(string tableKey, TableEditorDocument document)
        {
            if (_state == null || document == null)
                return;

            _state.TableKey = tableKey ?? string.Empty;
            _state.SnapshotJson = document.ToSnapshotJson();
            EditorUtility.SetDirty(_state);
        }

        private void HandleUndoRedoPerformed()
        {
            _onUndoRedoApplied?.Invoke(_state.TableKey, _state.SnapshotJson);
        }

        public void Dispose()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            if (_state != null)
                UnityEngine.Object.DestroyImmediate(_state);
        }
    }
}
