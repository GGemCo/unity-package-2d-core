using UnityEditor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGemCo2DCoreEditor
{
    public sealed class TableEditorWindow : EditorWindow
    {
        private const string Title = "데이터 테이블 에디터";
        
        private IReadOnlyList<TableEditorTableDefinition> _tables;
        private TableEditorTableDefinition _selectedTable;
        private TableEditorDocument _document;
        private IReadOnlyList<TableEditorColumnDefinition> _columns;
        private TableEditorDocumentRow _selectedRow;
        private readonly List<TableEditorValidationMessage> _validationMessages = new List<TableEditorValidationMessage>();
        private readonly List<TableEditorDocumentRow> _visibleRows = new List<TableEditorDocumentRow>();
        private readonly List<TableEditorTableDefinition> _filteredTables = new List<TableEditorTableDefinition>();
        private readonly List<string> _packageChoices = new List<string>();

        private string _tableSearch = string.Empty;
        private string _packageFilter = "All";
        private string _rowSearch = string.Empty;
        private bool _showOnlyValidationRows;
        private bool _showOnlySelectedValidation;

        private ToolbarSearchField _tableSearchField;
        private PopupField<string> _packagePopup;
        private ListView _tableListView;
        private ToolbarSearchField _rowSearchField;
        private MultiColumnListView _rowListView;
        private VisualElement _inspectorHost;
        private VisualElement _validationHost;
        private Label _pathLabel;
        private Label _statusLabel;
        private Toggle _showOnlyValidationToggle;
        private Toggle _showOnlySelectedValidationToggle;

        private TableEditorUndoController _undoController;

        [MenuItem(ConfigEditor.NameToolTableEditor, false, (int)ConfigEditor.ToolOrdering.TableEditor)]
        public static void OpenWindow()
        {
            TableEditorWindow window = GetWindow<TableEditorWindow>();
            window.titleContent = new GUIContent("Table Editor");
            window.minSize = new Vector2(1400f, 760f);
            window.Show();
        }

        private void OnEnable()
        {
            _tables = TableEditorRegistry.GetAll();
            BuildPackageChoices();
            _undoController ??= new TableEditorUndoController(HandleUndoRedoRestore);
        }

        private void OnDisable()
        {
            _undoController?.Dispose();
            _undoController = null;
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            BuildToolbar();
            BuildBody();

            RefreshTableList();
            if (_selectedTable == null && _tables != null && _tables.Count > 0)
                LoadTable(_tables[0]);
            else
                RefreshAllViews();
        }

        private void BuildToolbar()
        {
            Toolbar toolbar = new Toolbar();

            toolbar.Add(CreateToolbarButton("Reload", ReloadCurrentTable));
            toolbar.Add(CreateToolbarButton("Save", SaveCurrentTable));
            toolbar.Add(CreateToolbarButton("Validate", ValidateCurrentTable));
            toolbar.Add(CreateToolbarButton("Add Row", AddRow));
            toolbar.Add(CreateToolbarButton("Duplicate", DuplicateRow));
            toolbar.Add(CreateToolbarButton("Delete", DeleteSelectedRow));

            toolbar.Add(new ToolbarSpacer { style = { width = 10f } });
            _pathLabel = new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, flexGrow = 1f } };
            toolbar.Add(_pathLabel);
            _statusLabel = new Label { style = { minWidth = 90f, unityTextAlign = TextAnchor.MiddleRight } };
            toolbar.Add(_statusLabel);

            rootVisualElement.Add(toolbar);
        }

        private void BuildBody()
        {
            TwoPaneSplitView horizontal = new TwoPaneSplitView(0, 260, TwoPaneSplitViewOrientation.Horizontal)
            {
                style = { flexGrow = 1f }
            };
            rootVisualElement.Add(horizontal);

            VisualElement leftPanel = BuildTableListPanel();
            horizontal.Add(leftPanel);

            TwoPaneSplitView rightSplit = new TwoPaneSplitView(1, 430, TwoPaneSplitViewOrientation.Horizontal)
            {
                style = { flexGrow = 1f }
            };
            horizontal.Add(rightSplit);

            TwoPaneSplitView centerVertical = new TwoPaneSplitView(0, 480, TwoPaneSplitViewOrientation.Vertical)
            {
                style = { flexGrow = 1f }
            };
            rightSplit.Add(centerVertical);

            VisualElement gridPanel = BuildGridPanel();
            centerVertical.Add(gridPanel);

            VisualElement validationPanel = BuildValidationPanel();
            centerVertical.Add(validationPanel);

            VisualElement inspectorPanel = BuildInspectorPanel();
            rightSplit.Add(inspectorPanel);
        }

        private VisualElement BuildTableListPanel()
        {
            VisualElement panel = new VisualElement
            {
                style =
                {
                    flexGrow = 1f,
                    paddingLeft = 6f,
                    paddingRight = 6f,
                    paddingTop = 6f,
                    paddingBottom = 6f,
                }
            };

            panel.Add(new Label("Tables") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4f } });
            _packagePopup = new PopupField<string>(_packageChoices, Mathf.Max(0, _packageChoices.IndexOf(_packageFilter)))
            {
                label = "Package"
            };
            _packagePopup.RegisterValueChangedCallback(evt =>
            {
                _packageFilter = string.IsNullOrWhiteSpace(evt.newValue) ? "All" : evt.newValue;
                RefreshTableList();
            });
            panel.Add(_packagePopup);

            _tableSearchField = new ToolbarSearchField();
            _tableSearchField.RegisterValueChangedCallback(evt =>
            {
                _tableSearch = evt.newValue ?? string.Empty;
                RefreshTableList();
            });
            panel.Add(_tableSearchField);

            _tableListView = new ListView
            {
                style = { flexGrow = 1f, marginTop = 4f },
                selectionType = SelectionType.Single,
                fixedItemHeight = 22f,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                itemsSource = _filteredTables,
                makeItem = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft } },
                bindItem = (element, index) =>
                {
                    if (index < 0 || index >= _filteredTables.Count)
                        return;
                    TableEditorTableDefinition definition = _filteredTables[index];
                    ((Label)element).text = $"[{definition.PackageName}] {definition.DisplayName}";
                }
            };

            _tableListView.selectionChanged += selection =>
            {
                TableEditorTableDefinition next = selection.OfType<TableEditorTableDefinition>().FirstOrDefault();
                if (next != null && next != _selectedTable && TryConfirmDiscardChanges())
                    LoadTable(next);
            };
            panel.Add(_tableListView);
            return panel;
        }

        private VisualElement BuildGridPanel()
        {
            VisualElement panel = new VisualElement
            {
                style =
                {
                    flexGrow = 1f,
                    paddingLeft = 6f,
                    paddingRight = 6f,
                    paddingTop = 6f,
                    paddingBottom = 6f,
                }
            };

            panel.Add(new Label("Rows") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4f } });
            VisualElement filterRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4f } };
            _rowSearchField = new ToolbarSearchField { style = { flexGrow = 1f } };
            _rowSearchField.RegisterValueChangedCallback(evt =>
            {
                _rowSearch = evt.newValue ?? string.Empty;
                RefreshVisibleRows();
            });
            filterRow.Add(_rowSearchField);

            _showOnlyValidationToggle = new Toggle("Errors only") { style = { marginLeft = 6f } };
            _showOnlyValidationToggle.RegisterValueChangedCallback(evt =>
            {
                _showOnlyValidationRows = evt.newValue;
                RefreshVisibleRows();
            });
            filterRow.Add(_showOnlyValidationToggle);
            panel.Add(filterRow);

            _rowListView = new MultiColumnListView
            {
                style = { flexGrow = 1f },
                fixedItemHeight = 22f,
                itemsSource = _visibleRows,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                selectionType = SelectionType.Single,
                sortingMode = ColumnSortingMode.Default,
                showBorder = true,
                horizontalScrollingEnabled = true,
            };
            _rowListView.selectionChanged += selection =>
            {
                _selectedRow = selection.OfType<TableEditorDocumentRow>().FirstOrDefault();
                RebuildInspector();
                RebuildValidation();
            };
            panel.Add(_rowListView);
            return panel;
        }

        private VisualElement BuildInspectorPanel()
        {
            VisualElement panel = new VisualElement
            {
                style =
                {
                    flexGrow = 1f,
                    paddingLeft = 6f,
                    paddingRight = 6f,
                    paddingTop = 6f,
                    paddingBottom = 6f,
                }
            };
            panel.Add(new Label("Inspector") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4f } });
            _inspectorHost = new VisualElement { style = { flexGrow = 1f } };
            panel.Add(_inspectorHost);
            return panel;
        }

        private VisualElement BuildValidationPanel()
        {
            VisualElement panel = new VisualElement
            {
                style =
                {
                    flexGrow = 1f,
                    paddingLeft = 6f,
                    paddingRight = 6f,
                    paddingTop = 6f,
                    paddingBottom = 6f,
                }
            };
            VisualElement header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4f } };
            header.Add(new Label("Validation") { style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1f } });
            _showOnlySelectedValidationToggle = new Toggle("Selected row only");
            _showOnlySelectedValidationToggle.RegisterValueChangedCallback(evt =>
            {
                _showOnlySelectedValidation = evt.newValue;
                RebuildValidation();
            });
            header.Add(_showOnlySelectedValidationToggle);
            panel.Add(header);

            _validationHost = new VisualElement { style = { flexGrow = 1f } };
            panel.Add(_validationHost);
            return panel;
        }

        private Button CreateToolbarButton(string text, Action onClick)
        {
            return new Button(onClick) { text = text };
        }

        private void RefreshTableList()
        {
            _filteredTables.Clear();
            if (_tables != null)
            {
                for (int i = 0; i < _tables.Count; i++)
                {
                    TableEditorTableDefinition table = _tables[i];
                    if (!string.Equals(_packageFilter, "All", StringComparison.OrdinalIgnoreCase) && !string.Equals(table.PackageName, _packageFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!string.IsNullOrWhiteSpace(_tableSearch)
                        && table.DisplayName.IndexOf(_tableSearch, StringComparison.OrdinalIgnoreCase) < 0
                        && table.PackageName.IndexOf(_tableSearch, StringComparison.OrdinalIgnoreCase) < 0
                        && table.TableKey.IndexOf(_tableSearch, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    _filteredTables.Add(table);
                }
            }

            _tableListView?.Rebuild();
            if (_selectedTable != null)
            {
                int index = _filteredTables.IndexOf(_selectedTable);
                if (index >= 0)
                    _tableListView?.SetSelectionWithoutNotify(new[] { index });
            }
        }

        private void BuildPackageChoices()
        {
            _packageChoices.Clear();
            _packageChoices.Add("All");
            IReadOnlyList<string> packages = TableEditorRegistry.GetPackages();
            for (int i = 0; i < packages.Count; i++)
                _packageChoices.Add(packages[i]);

            if (!_packageChoices.Contains(_packageFilter))
                _packageFilter = "All";
        }

        private void LoadTable(TableEditorTableDefinition table)
        {
            if (table == null)
                return;

            try
            {
                _selectedTable = table;
                _document = TableEditorDocument.Load(table.AssetPath);
                _columns = table.BuildColumns(_document.Headers);
                _document.MergeHeaders(_columns);
                _selectedRow = _document.GetRows().FirstOrDefault();
                _validationMessages.Clear();
                _undoController?.Initialize(_selectedTable.TableKey, _document);
                TableEditorReferenceCache.Invalidate(_selectedTable);
                RefreshAllViews();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Table Editor", ex.Message, "OK");
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

            try
            {
                _document.Save();
                _selectedTable?.ReloadAction?.Invoke();
                TableEditorReferenceCache.Invalidate(_selectedTable);
                ValidateCurrentTable();
                _undoController?.Commit(_selectedTable.TableKey, _document);
                RefreshStatus();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Table Editor", ex.Message, "OK");
            }
        }

        private void ValidateCurrentTable()
        {
            _validationMessages.Clear();
            if (_selectedTable != null && _document != null)
                _validationMessages.AddRange(TableEditorValidator.Validate(_selectedTable, _document));
            RebuildValidation();
            RefreshVisibleRows();
        }

        private void AddRow()
        {
            if (_document == null)
                return;

            ApplyDocumentMutation("Table Add Row", () =>
            {
                _selectedRow = _document.AddRow();
                AssignAutoUidIfNeeded(_selectedRow);
            });
        }

        private void DuplicateRow()
        {
            if (_document == null || _selectedRow == null)
                return;

            ApplyDocumentMutation("Table Duplicate Row", () =>
            {
                _selectedRow = _document.DuplicateRow(_selectedRow);
                AssignAutoUidIfNeeded(_selectedRow);
            });
        }

        private void DeleteSelectedRow()
        {
            if (_document == null || _selectedRow == null)
                return;

            TableEditorDocumentRow rowToDelete = _selectedRow;
            ApplyDocumentMutation("Table Delete Row", () =>
            {
                _document.RemoveRow(rowToDelete);
                _selectedRow = _document.GetRows().FirstOrDefault();
            });
        }

        private void HandleCellValueChanged(string headerName, string nextValue)
        {
            if (_document == null || _selectedRow == null)
                return;

            if (string.Equals(_selectedRow.Values.TryGetValue(headerName, out string current) ? current : string.Empty, nextValue ?? string.Empty, StringComparison.Ordinal))
                return;

            ITableEditorTableRuleProvider ruleProvider = TableEditorRuleProviderRegistry.GetProvider(_selectedTable);
            bool inspectorLayoutChanged = string.Equals(headerName, "Kind", StringComparison.OrdinalIgnoreCase);
            ApplyDocumentMutation($"Edit {headerName}", () =>
            {
                _document.SetCellValue(_selectedRow, headerName, nextValue);
                ruleProvider?.OnBeforeCellValueChanged(_document, _selectedRow, headerName, nextValue);
            }, !inspectorLayoutChanged);
        }

        private void JumpToReference(TableEditorTableDefinition table, int uid)
        {
            if (table == null)
                return;

            if (!TryConfirmDiscardChanges())
                return;

            LoadTable(table);
            if (_document == null)
                return;

            _selectedRow = _document.GetRows().FirstOrDefault(r =>
                int.TryParse(TableEditorValueUtility.GetRowUidRaw(r), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value == uid);
            RefreshAllViews();
            if (_selectedRow != null)
            {
                int rowIndex = _visibleRows.IndexOf(_selectedRow);
                if (rowIndex >= 0)
                {
                    _rowListView.SetSelectionWithoutNotify(new[] { rowIndex });
                    _rowListView.ScrollToItem(rowIndex);
                }
            }
        }

        private void ApplyDocumentMutation(string undoName, Action mutation, bool keepInspectorState = false)
        {
            if (_selectedTable == null || _document == null || mutation == null)
                return;

            _undoController?.BeginRecord(undoName);
            mutation();
            _undoController?.Commit(_selectedTable.TableKey, _document);
            _selectedTable?.ReloadAction?.Invoke();
            TableEditorReferenceCache.Invalidate(_selectedTable);
            if (keepInspectorState)
                RefreshViewsWithoutInspectorRebuild();
            else
                RefreshAllViews();
        }

        private void HandleUndoRedoRestore(string tableKey, string snapshotJson)
        {
            if (_selectedTable == null || !string.Equals(_selectedTable.TableKey, tableKey, StringComparison.OrdinalIgnoreCase))
                return;

            TableEditorDocument restored = TableEditorDocument.FromSnapshotJson(snapshotJson);
            if (restored == null)
                return;

            int selectedStableId = _selectedRow?.stableId ?? -1;
            _document = restored;
            _columns = _selectedTable.BuildColumns(_document.Headers);
            _document.MergeHeaders(_columns);
            _selectedRow = _document.GetRows().FirstOrDefault(r => r.stableId == selectedStableId) ?? _document.GetRows().FirstOrDefault();
            RefreshAllViews();
        }

        private void RefreshAllViews()
        {
            RefreshStatus();
            RebuildColumns();
            RefreshVisibleRows();
            RebuildInspector();
            RebuildValidation();
            RefreshTableList();
        }

        private void RefreshViewsWithoutInspectorRebuild()
        {
            RefreshStatus();
            RebuildColumns();
            RefreshVisibleRows();
            RebuildValidation();
            RefreshTableList();
        }

        private void RefreshStatus()
        {
            if (_pathLabel != null)
                _pathLabel.text = _selectedTable != null ? _selectedTable.AssetPath : string.Empty;
            if (_statusLabel != null)
                _statusLabel.text = _document != null && _document.IsDirty ? "Modified" : string.Empty;
        }

        private void RefreshVisibleRows()
        {
            _visibleRows.Clear();
            if (_document != null)
            {
                IEnumerable<TableEditorDocumentRow> rows = _document.GetRows();
                HashSet<int> invalidRowIds = BuildInvalidRowIdSet();

                foreach (TableEditorDocumentRow row in rows)
                {
                    if (!string.IsNullOrWhiteSpace(_rowSearch) && !MatchesSearch(row, _rowSearch))
                        continue;
                    if (_showOnlyValidationRows && !invalidRowIds.Contains(row.stableId))
                        continue;
                    _visibleRows.Add(row);
                }
            }

            _rowListView?.Rebuild();
            if (_selectedRow != null)
            {
                int index = _visibleRows.IndexOf(_selectedRow);
                if (index >= 0)
                    _rowListView?.SetSelectionWithoutNotify(new[] { index });
            }
        }

        private void RebuildColumns()
        {
            if (_rowListView == null)
                return;

            _rowListView.columns.Clear();
            if (_document == null || _columns == null)
                return;

            Column indexColumn = new Column
            {
                name = "rowIndex",
                title = "Row",
                width = 56,
                stretchable = false,
                makeCell = () => new Label(),
                bindCell = (element, index) =>
                {
                    ((Label)element).text = (index + 1).ToString(CultureInfo.InvariantCulture);
                }
            };
            _rowListView.columns.Add(indexColumn);

            for (int i = 0; i < _columns.Count; i++)
            {
                TableEditorColumnDefinition column = _columns[i];
                Column uiColumn = new Column
                {
                    name = column.HeaderName,
                    title = column.HeaderName,
                    width = column.IsUidColumn ? 80 : 150,
                    minWidth = 70,
                    stretchable = true,
                    sortable = true,
                    makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft } },
                    bindCell = (element, index) =>
                    {
                        if (index < 0 || index >= _visibleRows.Count)
                            return;
                        Label label = (Label)element;
                        TableEditorDocumentRow row = _visibleRows[index];
                        row.Values.TryGetValue(column.HeaderName, out string value);
                        label.text = BuildGridCellText(column, row, value);
                        label.tooltip = value ?? string.Empty;
                    },
                    comparison = (firstIndex, secondIndex) =>
                    {
                        string left = GetColumnRaw(_visibleRows[firstIndex], column.HeaderName);
                        string right = GetColumnRaw(_visibleRows[secondIndex], column.HeaderName);
                        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
                    }
                };
                _rowListView.columns.Add(uiColumn);
            }
        }

        private void RebuildInspector()
        {
            if (_inspectorHost == null)
                return;
            _inspectorHost.Clear();
            _inspectorHost.Add(TableEditorGui.BuildInspector(this, _selectedTable, _columns ?? Array.Empty<TableEditorColumnDefinition>(), _selectedRow, HandleCellValueChanged, JumpToReference));
        }

        private void RebuildValidation()
        {
            if (_validationHost == null)
                return;
            _validationHost.Clear();
            _validationHost.Add(TableEditorGui.BuildValidationView(_validationMessages, _selectedRow?.stableId ?? -1, _showOnlySelectedValidation));
        }

        private bool TryConfirmDiscardChanges()
        {
            return _document == null || !_document.IsDirty || EditorUtility.DisplayDialog("Table Editor", "저장하지 않은 변경사항이 있습니다. 버리고 진행할까요?", "Discard", "Cancel");
        }

        private void AssignAutoUidIfNeeded(TableEditorDocumentRow row)
        {
            if (row == null || !_document.Headers.Contains("Uid"))
                return;

            if (int.TryParse(TableEditorValueUtility.GetRowUidRaw(row), out int existing) && existing > 0)
                return;

            int nextUid = 1;
            foreach (TableEditorDocumentRow item in _document.GetRows())
            {
                if (item == row)
                    continue;
                if (int.TryParse(TableEditorValueUtility.GetRowUidRaw(item), out int uid))
                    nextUid = Math.Max(nextUid, uid + 1);
            }

            _document.SetCellValue(row, "Uid", nextUid.ToString(CultureInfo.InvariantCulture));
        }

        private HashSet<int> BuildInvalidRowIdSet()
        {
            HashSet<int> result = new HashSet<int>();
            for (int i = 0; i < _validationMessages.Count; i++)
            {
                TableEditorValidationMessage message = _validationMessages[i];
                if (message.RowStableId > 0)
                    result.Add(message.RowStableId);
            }
            return result;
        }

        private static bool MatchesSearch(TableEditorDocumentRow row, string search)
        {
            if (row == null || string.IsNullOrWhiteSpace(search))
                return true;

            foreach (KeyValuePair<string, string> pair in row.Values)
            {
                if ((pair.Value ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string GetColumnRaw(TableEditorDocumentRow row, string header)
        {
            return row != null && row.Values.TryGetValue(header, out string value) ? value ?? string.Empty : string.Empty;
        }

        private static string BuildGridCellText(TableEditorColumnDefinition column, TableEditorDocumentRow row, string raw)
        {
            raw ??= string.Empty;
            if (column != null && column.HasReferenceCandidate)
            {
                TableEditorReferenceRule rule = column.ResolveReferenceRule(row);
                TableEditorTableDefinition referenceTable = column.GetReferenceTable(rule);
                if (rule != null && rule.ValueKind == TableEditorReferenceValueKind.StringId && !string.IsNullOrWhiteSpace(raw))
                {
                    TableEditorReferenceItem item = TableEditorReferenceCache.FindItem(referenceTable, raw);
                    if (item != null)
                        raw = $"{raw} ({item.DisplayName})";
                }
                else if (column.IsReferenceCandidate && int.TryParse(raw, out int uid) && uid > 0)
                {
                    if (TableEditorVfxReferenceUtility.IsTabbedVfxReference(column))
                    {
                        raw = TableEditorVfxReferenceUtility.BuildCellText(uid);
                    }
                    else
                    {
                        TableEditorReferenceItem item = TableEditorReferenceCache.FindItem(referenceTable, uid);
                        if (item != null)
                            raw = $"{uid} ({item.DisplayName})";
                    }
                }
                else if (column.IsMultiReferenceCandidate && !string.IsNullOrWhiteSpace(raw))
                {
                    string[] tokens = raw.Split(',');
                    List<string> labels = new List<string>(tokens.Length);
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string token = tokens[i]?.Trim();
                        if (string.IsNullOrWhiteSpace(token))
                            continue;

                        if (int.TryParse(token, out int referenceUid) && referenceUid > 0)
                        {
                            TableEditorReferenceItem item = TableEditorReferenceCache.FindItem(referenceTable, referenceUid);
                            labels.Add(item != null ? $"{referenceUid} ({item.DisplayName})" : referenceUid.ToString());
                        }
                    }

                    if (labels.Count > 0)
                        raw = string.Join(", ", labels);
                }
            }

            if (raw.Length > 48)
                return raw.Substring(0, 48) + "…";
            return raw;
        }
    }
}
