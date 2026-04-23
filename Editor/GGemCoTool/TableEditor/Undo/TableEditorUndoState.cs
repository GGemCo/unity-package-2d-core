using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorUndoState : ScriptableObject
    {
        [SerializeField] private string _tableKey;
        [SerializeField] private string _snapshotJson;

        public string TableKey
        {
            get => _tableKey;
            set => _tableKey = value;
        }

        public string SnapshotJson
        {
            get => _snapshotJson;
            set => _snapshotJson = value;
        }
    }
}
