using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    [Serializable]
    internal sealed class TableEditorUndoCellEdit
    {
        [SerializeField] private int _rowStableId;
        [SerializeField] private string _headerName;
        [SerializeField] private string _rawValue;

        public int RowStableId
        {
            get => _rowStableId;
            set => _rowStableId = value;
        }

        public string HeaderName
        {
            get => _headerName;
            set => _headerName = value;
        }

        public string RawValue
        {
            get => _rawValue;
            set => _rawValue = value;
        }
    }

    internal sealed class TableEditorUndoState : ScriptableObject
    {
        [SerializeField] private string _tableKey;
        [SerializeField] private string _snapshotJson;
        [SerializeField] private List<TableEditorUndoCellEdit> _cellEdits = new List<TableEditorUndoCellEdit>();

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

        public List<TableEditorUndoCellEdit> CellEdits => _cellEdits;

        /// <summary>
        /// 기준 스냅샷 이후 누적된 셀 편집 기록을 모두 제거합니다.
        /// 구조 변경이나 저장 이후에는 기준 스냅샷 자체가 최신 상태가 되므로 셀 편집 기록을 비웁니다.
        /// </summary>
        public void ClearCellEdits()
        {
            _cellEdits ??= new List<TableEditorUndoCellEdit>();
            _cellEdits.Clear();
        }

        /// <summary>
        /// 기준 스냅샷 위에 재생할 셀 편집 기록을 추가합니다.
        /// 전체 문서 JSON을 매번 다시 만들지 않고 일반 셀 편집 Undo/Redo 비용을 줄이기 위한 경량 기록입니다.
        /// </summary>
        /// <param name="rowStableId">편집된 행의 안정 식별자입니다.</param>
        /// <param name="headerName">편집된 컬럼 헤더입니다.</param>
        /// <param name="rawValue">편집 후 원본 문자열 값입니다.</param>
        public void AddCellEdit(int rowStableId, string headerName, string rawValue)
        {
            _cellEdits ??= new List<TableEditorUndoCellEdit>();
            _cellEdits.Add(new TableEditorUndoCellEdit
            {
                RowStableId = rowStableId,
                HeaderName = headerName ?? string.Empty,
                RawValue = rawValue ?? string.Empty,
            });
        }
    }
}
