using System;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorUndoController : IDisposable
    {
        private readonly Action<string, TableEditorDocument> _onUndoRedoApplied;
        private readonly TableEditorUndoState _state;

        public TableEditorUndoController(Action<string, TableEditorDocument> onUndoRedoApplied)
        {
            _onUndoRedoApplied = onUndoRedoApplied;
            _state = ScriptableObject.CreateInstance<TableEditorUndoState>();
            _state.hideFlags = HideFlags.HideAndDontSave;
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
        }

        /// <summary>
        /// 선택 테이블의 현재 문서를 Undo 기준 스냅샷으로 초기화합니다.
        /// 테이블 로드 직후에는 이후 일반 셀 편집을 이 스냅샷 기준의 경량 편집 기록으로 누적합니다.
        /// </summary>
        /// <param name="tableKey">테이블 식별자입니다.</param>
        /// <param name="document">기준으로 사용할 현재 문서입니다.</param>
        public void Initialize(string tableKey, TableEditorDocument document)
        {
            CommitSnapshotInternal(tableKey, document);
        }

        /// <summary>
        /// Unity Undo 시스템에 현재 상태 전체를 기록합니다.
        /// 이 호출 이후 상태 객체에 변경 내용을 반영하면 Undo/Redo 시 해당 상태로 복원됩니다.
        /// </summary>
        /// <param name="actionName">Undo 메뉴에 표시될 작업 이름입니다.</param>
        public void BeginRecord(string actionName)
        {
            if (_state == null)
                return;

            Undo.RegisterCompleteObjectUndo(_state, actionName);
        }

        /// <summary>
        /// 현재 문서 전체를 기준 스냅샷으로 커밋합니다.
        /// 행 추가/삭제처럼 구조가 바뀌는 작업은 셀 편집 기록만으로 안전하게 복원하기 어렵기 때문에 전체 스냅샷을 갱신합니다.
        /// </summary>
        /// <param name="tableKey">테이블 식별자입니다.</param>
        /// <param name="document">스냅샷으로 저장할 문서입니다.</param>
        public void CommitSnapshot(string tableKey, TableEditorDocument document)
        {
            CommitSnapshotInternal(tableKey, document);
        }

        /// <summary>
        /// 일반 셀 편집 결과를 경량 Undo 기록으로 커밋합니다.
        /// 전체 문서를 JSON으로 직렬화하지 않고 기준 스냅샷 이후의 셀 값 변경만 누적합니다.
        /// </summary>
        /// <param name="tableKey">테이블 식별자입니다.</param>
        /// <param name="rowStableId">편집된 행의 안정 식별자입니다.</param>
        /// <param name="headerName">편집된 컬럼 헤더입니다.</param>
        /// <param name="rawValue">편집 후 원본 문자열 값입니다.</param>
        public void CommitCellEdit(string tableKey, int rowStableId, string headerName, string rawValue)
        {
            if (_state == null || rowStableId <= 0 || string.IsNullOrWhiteSpace(headerName))
                return;

            _state.TableKey = tableKey ?? string.Empty;
            _state.AddCellEdit(rowStableId, headerName, rawValue);
            EditorUtility.SetDirty(_state);
        }

        /// <summary>
        /// 전체 문서 스냅샷을 내부 상태에 저장하고 누적 셀 편집 기록을 초기화합니다.
        /// </summary>
        /// <param name="tableKey">테이블 식별자입니다.</param>
        /// <param name="document">스냅샷으로 저장할 문서입니다.</param>
        private void CommitSnapshotInternal(string tableKey, TableEditorDocument document)
        {
            if (_state == null || document == null)
                return;

            _state.TableKey = tableKey ?? string.Empty;
            _state.SnapshotJson = document.ToSnapshotJson();
            _state.ClearCellEdits();
            EditorUtility.SetDirty(_state);
        }

        /// <summary>
        /// Unity Undo/Redo 이벤트가 발생했을 때 기준 스냅샷과 누적 셀 편집 기록으로 문서를 복원합니다.
        /// </summary>
        private void HandleUndoRedoPerformed()
        {
            TableEditorDocument restored = BuildDocumentFromState();
            _onUndoRedoApplied?.Invoke(_state.TableKey, restored);
        }

        /// <summary>
        /// Undo 상태 객체에 저장된 기준 스냅샷과 셀 편집 기록을 조합하여 실제 편집 문서를 생성합니다.
        /// </summary>
        /// <returns>복원된 문서입니다. 기준 스냅샷이 없으면 null을 반환합니다.</returns>
        private TableEditorDocument BuildDocumentFromState()
        {
            if (_state == null)
                return null;

            TableEditorDocument document = TableEditorDocument.FromSnapshotJson(_state.SnapshotJson);
            if (document == null)
                return null;

            if (_state.CellEdits == null || _state.CellEdits.Count == 0)
                return document;

            for (int i = 0; i < _state.CellEdits.Count; i++)
            {
                TableEditorUndoCellEdit edit = _state.CellEdits[i];
                if (edit == null || edit.RowStableId <= 0 || string.IsNullOrWhiteSpace(edit.HeaderName))
                    continue;

                TableEditorDocumentRow row = FindRowByStableId(document, edit.RowStableId);
                if (row == null)
                    continue;

                document.SetCellValue(row, edit.HeaderName, edit.RawValue);
            }

            return document;
        }

        /// <summary>
        /// 안정 식별자로 문서 행을 찾습니다.
        /// Undo 기준 스냅샷에서 복원된 행 객체는 현재 창의 행 참조와 다르므로 stableId로 다시 연결합니다.
        /// </summary>
        /// <param name="document">검색할 문서입니다.</param>
        /// <param name="stableId">찾을 행의 안정 식별자입니다.</param>
        /// <returns>찾은 행입니다. 없으면 null입니다.</returns>
        private static TableEditorDocumentRow FindRowByStableId(TableEditorDocument document, int stableId)
        {
            if (document == null || stableId <= 0)
                return null;

            foreach (TableEditorDocumentRow row in document.GetRows())
            {
                if (row != null && row.stableId == stableId)
                    return row;
            }

            return null;
        }

        public void Dispose()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            if (_state != null)
                UnityEngine.Object.DestroyImmediate(_state);
        }
    }
}
