using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public sealed class TableEditorWindow : EditorWindow
    {
        private IReadOnlyList<TableEditorTableDefinition> _tables;
        private TableEditorTableDefinition _selectedTable;
        private TableEditorDocument _document;
        private IReadOnlyList<TableEditorColumnDefinition> _columns;
        private TableEditorDocumentRow _selectedRow;
        private List<TableEditorValidationMessage> _validationMessages = new List<TableEditorValidationMessage>();

        private string _tableSearch = string.Empty;
        private string _rowSearch = string.Empty;
        private Vector2 _leftScroll;
        private Vector2 _centerScroll;
        private Vector2 _rightScroll;
        private Vector2 _bottomScroll;
        private bool _showOnlyValidationRows;

        public static void OpenWindow()
        {
            TableEditorWindow window = GetWindow<TableEditorWindow>();
            window.titleContent = new GUIContent("Table Editor");
            window.minSize = new Vector2(1400f, 700f);
            window.Show();
        }

        private void OnEnable()
        {
            _tables = TableEditorRegistry.GetAll();
            if (_selectedTable == null && _tables.Count > 0)
                LoadTable(_tables[0]);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_selectedTable == null)
            {
                EditorGUILayout.HelpBox("표시할 테이블이 없습니다.", MessageType.Info);
                return;
            }

            Rect contentRect = GUILayoutUtility.GetRect(position.width, position.height - 70f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            float leftWidth = Mathf.Max(240f, contentRect.width * 0.18f);
            float rightWidth = Mathf.Max(380f, contentRect.width * 0.32f);
            float centerWidth = Mathf.Max(300f, contentRect.width - leftWidth - rightWidth - 8f);

            Rect leftRect = new Rect(contentRect.x, contentRect.y, leftWidth, contentRect.height * 0.72f);
            Rect centerRect = new Rect(leftRect.xMax + 4f, contentRect.y, centerWidth, contentRect.height * 0.72f);
            Rect rightRect = new Rect(centerRect.xMax + 4f, contentRect.y, rightWidth, contentRect.height * 0.72f);
            Rect bottomRect = new Rect(contentRect.x, leftRect.yMax + 4f, contentRect.width, contentRect.height * 0.28f - 4f);

            GUILayout.BeginArea(leftRect, EditorStyles.helpBox);
            DrawTableListPanel();
            GUILayout.EndArea();

            GUILayout.BeginArea(centerRect, EditorStyles.helpBox);
            DrawGridPanel();
            GUILayout.EndArea();

            GUILayout.BeginArea(rightRect, EditorStyles.helpBox);
            DrawInspectorPanel();
            GUILayout.EndArea();

            GUILayout.BeginArea(bottomRect, EditorStyles.helpBox);
            DrawValidationPanel();
            GUILayout.EndArea();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                ReloadCurrentTable();
            }

            using (new EditorGUI.DisabledScope(_document == null || !_document.IsDirty))
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    SaveCurrentTable();
                }
            }

            using (new EditorGUI.DisabledScope(_document == null))
            {
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    ValidateCurrentTable();
                }

                if (GUILayout.Button("Add Row", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    AddRow();
                }

                using (new EditorGUI.DisabledScope(_selectedRow == null))
                {
                    if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    {
                        DuplicateRow();
                    }

                    if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    {
                        DeleteSelectedRow();
                    }
                }
            }

            GUILayout.Space(12f);
            GUILayout.Label(_selectedTable != null ? _selectedTable.AssetPath : string.Empty, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (_document != null && _document.IsDirty)
                GUILayout.Label("Modified", EditorStyles.toolbarButton, GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableListPanel()
        {
            EditorGUILayout.LabelField("Tables", EditorStyles.boldLabel);
            _tableSearch = EditorGUILayout.TextField("Search", _tableSearch);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            foreach (TableEditorTableDefinition table in _tables)
            {
                if (!string.IsNullOrWhiteSpace(_tableSearch) && table.DisplayName.IndexOf(_tableSearch, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                GUIStyle style = table == _selectedTable ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                if (GUILayout.Button(table.DisplayName, style))
                {
                    if (TryConfirmDiscardChanges())
                        LoadTable(table);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGridPanel()
        {
            EditorGUILayout.LabelField($"Rows - {_selectedTable.DisplayName}", EditorStyles.boldLabel);
            _rowSearch = EditorGUILayout.TextField("Filter", _rowSearch);
            _centerScroll = EditorGUILayout.BeginScrollView(_centerScroll);

            if (_document == null)
            {
                EditorGUILayout.HelpBox("문서를 불러오지 못했습니다.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            List<TableEditorDocumentRow> rows = GetVisibleRows();
            DrawGridHeader();

            for (int i = 0; i < rows.Count; i++)
            {
                TableEditorDocumentRow row = rows[i];
                EditorGUILayout.BeginHorizontal(row == _selectedRow ? EditorStyles.helpBox : GUIStyle.none);

                if (GUILayout.Button($"#{i + 1}", GUILayout.Width(44f)))
                    _selectedRow = row;

                for (int c = 0; c < _columns.Count; c++)
                {
                    TableEditorColumnDefinition column = _columns[c];
                    row.Values.TryGetValue(column.HeaderName, out string value);
                    string display = value ?? string.Empty;
                    if (display.Length > 24)
                        display = display.Substring(0, 24) + "…";

                    if (GUILayout.Button(display, EditorStyles.miniButton, GUILayout.Width(120f)))
                        _selectedRow = row;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGridHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Row", EditorStyles.miniBoldLabel, GUILayout.Width(44f));
            for (int i = 0; i < _columns.Count; i++)
                GUILayout.Label(_columns[i].HeaderName, EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInspectorPanel()
        {
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
            if (_selectedRow == null)
            {
                EditorGUILayout.HelpBox("행을 선택해주세요.", MessageType.Info);
                return;
            }

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
            object previewObject = TableEditorValueUtility.BuildRowObject(_selectedTable, _selectedRow, out List<string> fieldErrors);
            _selectedRow.CachedDisplayName = TableEditorReflectionUtility.GetDisplayName(previewObject, 0);
            EditorGUILayout.LabelField(_selectedRow.CachedDisplayName, EditorStyles.boldLabel);
            EditorGUILayout.Space(6f);

            for (int i = 0; i < _columns.Count; i++)
            {
                bool changed = TableEditorGui.DrawCellEditor(_columns[i], _selectedRow, JumpToReference, _selectedTable);
                if (changed)
                {
                    _document.IsDirty = true;
                    GUI.changed = true;
                }
            }

            if (fieldErrors.Count > 0)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Field Errors", EditorStyles.boldLabel);
                for (int i = 0; i < fieldErrors.Count; i++)
                    EditorGUILayout.HelpBox(fieldErrors[i], MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawValidationPanel()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            _showOnlyValidationRows = EditorGUILayout.ToggleLeft("Selected Row Only", _showOnlyValidationRows, GUILayout.Width(150f));
            EditorGUILayout.EndHorizontal();

            _bottomScroll = EditorGUILayout.BeginScrollView(_bottomScroll);
            int rowStableId = _showOnlyValidationRows && _selectedRow != null ? _selectedRow.StableId : -1;
            TableEditorGui.DrawValidationSummary(_validationMessages, rowStableId);
            EditorGUILayout.EndScrollView();
        }

        private void LoadTable(TableEditorTableDefinition table)
        {
            _selectedTable = table;
            try
            {
                _document = TableEditorDocument.Load(table.AssetPath);
                _columns = table.BuildColumns(_document.Headers);
                _document.MergeHeaders(_columns);
                _columns = table.BuildColumns(_document.Headers);
                _selectedRow = _document.GetRows().FirstOrDefault();
                _validationMessages.Clear();
            }
            catch (Exception ex)
            {
                _document = null;
                _columns = Array.Empty<TableEditorColumnDefinition>();
                _selectedRow = null;
                _validationMessages = new List<TableEditorValidationMessage>
                {
                    new TableEditorValidationMessage
                    {
                        Severity = TableEditorValidationSeverity.Error,
                        Message = ex.Message,
                        RowStableId = -1,
                    }
                };
            }
        }

        private void ReloadCurrentTable()
        {
            if (_selectedTable == null)
                return;

            if (!TryConfirmDiscardChanges())
                return;

            LoadTable(_selectedTable);
        }

        private void SaveCurrentTable()
        {
            if (_document == null)
                return;

            ValidateCurrentTable();
            bool hasError = _validationMessages.Any(static m => m.Severity == TableEditorValidationSeverity.Error);
            if (hasError)
            {
                bool forceSave = EditorUtility.DisplayDialog("Table Editor", "검증 에러가 있습니다. 그래도 저장하시겠습니까?", "저장", "취소");
                if (!forceSave)
                    return;
            }

            _document.Save();
            TableLoaderManagerBase.Unload(_selectedTable.AssetPath);
            LoadTable(_selectedTable);
        }

        private void ValidateCurrentTable()
        {
            if (_selectedTable == null || _document == null)
                return;

            _validationMessages = TableEditorValidator.Validate(_selectedTable, _document);
        }

        private void AddRow()
        {
            if (_document == null)
                return;

            TableEditorDocumentRow row = _document.AddRow();
            int nextUid = GetNextUid();
            if (row.Values.ContainsKey("Uid"))
                row.Values["Uid"] = nextUid > 0 ? nextUid.ToString() : string.Empty;

            _selectedRow = row;
        }

        private void DuplicateRow()
        {
            if (_document == null || _selectedRow == null)
                return;

            TableEditorDocumentRow row = _document.DuplicateRow(_selectedRow);
            if (row.Values.ContainsKey("Uid"))
                row.Values["Uid"] = GetNextUid().ToString();

            _selectedRow = row;
        }

        private void DeleteSelectedRow()
        {
            if (_document == null || _selectedRow == null)
                return;

            if (!EditorUtility.DisplayDialog("Table Editor", "선택한 행을 삭제하시겠습니까?", "삭제", "취소"))
                return;

            TableEditorDocumentRow current = _selectedRow;
            _document.RemoveRow(current);
            _selectedRow = _document.GetRows().FirstOrDefault();
        }

        private void JumpToReference(TableEditorTableDefinition targetTable, int uid)
        {
            if (targetTable == null)
                return;

            if (_selectedTable != targetTable)
            {
                if (!TryConfirmDiscardChanges())
                    return;

                LoadTable(targetTable);
            }

            if (_document == null)
                return;

            _selectedRow = _document.GetRows().FirstOrDefault(row =>
                row.Values.TryGetValue("Uid", out string rawUid) &&
                int.TryParse(rawUid, out int rowUid) &&
                rowUid == uid);
        }

        private List<TableEditorDocumentRow> GetVisibleRows()
        {
            IEnumerable<TableEditorDocumentRow> query = _document.GetRows();
            if (!string.IsNullOrWhiteSpace(_rowSearch))
            {
                query = query.Where(row => row.Values.Values.Any(v => !string.IsNullOrEmpty(v) && v.IndexOf(_rowSearch, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            return query.ToList();
        }

        private int GetNextUid()
        {
            int maxUid = 0;
            foreach (TableEditorDocumentRow row in _document.GetRows())
            {
                if (!row.Values.TryGetValue("Uid", out string rawUid))
                    continue;

                if (int.TryParse(rawUid, out int uid))
                    maxUid = Math.Max(maxUid, uid);
            }

            return maxUid + 1;
        }

        private bool TryConfirmDiscardChanges()
        {
            if (_document == null || !_document.IsDirty)
                return true;

            return EditorUtility.DisplayDialog("Table Editor", "저장하지 않은 변경사항이 있습니다. 버리고 진행하시겠습니까?", "진행", "취소");
        }
    }
}
