using UnityEditor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GGemCo2DCore;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGemCo2DCoreEditor
{
    public sealed class TableEditorWindow : EditorWindow
    {
        private const string Title = "데이터 테이블 에디터";

        private sealed class PendingOpenRequest
        {
            public string TableKey;
            public string HeaderName;
            public string RawValue;
        }

        private static PendingOpenRequest _pendingOpenRequest;
        
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
        private bool _isValidationStale;

        private ToolbarSearchField _tableSearchField;
        private PopupField<string> _packagePopup;
        private ListView _tableListView;
        private ToolbarSearchField _rowSearchField;
        private MultiColumnListView _rowListView;
        private VisualElement _inspectorHost;
        private VisualElement _validationHost;
        private Label _pathLabel;
        private Label _statusLabel;
        private Button _shopProbabilityButton;
        private Toggle _showOnlyValidationToggle;
        private Toggle _showOnlySelectedValidationToggle;

        private TableEditorUndoController _undoController;

        [MenuItem(ConfigEditor.NameToolTableEditor, false, (int)ConfigEditor.ToolOrdering.TableEditor)]
        public static void OpenWindow()
        {
            GetOrCreateWindow().Show();
        }

        public static void OpenAndFocusRowByIntKey(string tableKey, string headerName, int keyValue)
        {
            if (string.IsNullOrWhiteSpace(tableKey) || string.IsNullOrWhiteSpace(headerName) || keyValue <= 0)
                return;

            _pendingOpenRequest = new PendingOpenRequest
            {
                TableKey = tableKey,
                HeaderName = headerName,
                RawValue = keyValue.ToString(CultureInfo.InvariantCulture),
            };

            TableEditorWindow window = GetOrCreateWindow();
            window.Show();
            window.TryApplyPendingOpenRequest();
        }

        private static TableEditorWindow GetOrCreateWindow()
        {
            TableEditorWindow window = GetWindow<TableEditorWindow>();
            window.titleContent = new GUIContent("Table Editor");
            window.minSize = new Vector2(1400f, 760f);
            return window;
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

            TryApplyPendingOpenRequest();
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
            _shopProbabilityButton = CreateToolbarButton("Shop Rates", OpenShopProbabilityWindow);
            toolbar.Add(_shopProbabilityButton);

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
                _isValidationStale = false;
                _undoController?.Initialize(_selectedTable.TableKey, _document);
                TableEditorReferenceCache.Invalidate(_selectedTable);
                RefreshAllViews();
                TryApplyPendingOpenRequest();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Table Editor", BuildSafeDialogMessage(ex), "OK");
            }
        }

        /// <summary>
        /// Table Editor 예외를 대화상자에 안전하게 표시할 수 있는 문자열로 변환합니다.
        /// 예외 메시지가 비어 있을 때는 전체 예외 문자열을 fallback 으로 사용합니다.
        /// </summary>
        /// <param name="exception">변환할 예외입니다.</param>
        /// <returns>대화상자에 표시 가능한 안전한 메시지입니다.</returns>
        private static string BuildSafeDialogMessage(Exception exception)
        {
            if (exception == null)
                return "알 수 없는 오류가 발생했습니다.";

            return string.IsNullOrWhiteSpace(exception.Message)
                ? exception.ToString()
                : exception.Message;
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
            if (_document == null || _selectedTable == null)
                return;

            try
            {
                TableEditorSaveContext context = new TableEditorSaveContext
                {
                    TableDefinition = _selectedTable,
                    HasDocumentChanges = _document.IsDirty,
                    // 저장 직전의 편집 행 스냅샷을 전달해 SaveProcessor가 안전하게 검증할 수 있게 합니다.
                    Rows = _document.GetRows().ToList(),
                };

                IReadOnlyList<ITableEditorSaveProcessor> processors = TableEditorSaveProcessorRegistry.GetAll();
                for (int i = 0; i < processors.Count; i++)
                {
                    ITableEditorSaveProcessor processor = processors[i];
                    if (processor.CanProcess(context))
                        processor.BeforeSave(context);
                }

                _document.SaveToDisk();
                _document.ReimportAsset();
                _selectedTable.ReloadAction?.Invoke();

                for (int i = 0; i < processors.Count; i++)
                {
                    ITableEditorSaveProcessor processor = processors[i];
                    if (processor.CanProcess(context))
                        processor.AfterSave(context);
                }

                TableEditorReferenceCache.Invalidate(_selectedTable);
                ValidateCurrentTable();
                _undoController?.CommitSnapshot(_selectedTable.TableKey, _document);
                RefreshStatus();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Table Editor", BuildSafeDialogMessage(ex), "OK");
            }
        }

        /// <summary>
        /// 현재 테이블 전체를 다시 검증하고 검증 UI와 행 필터를 갱신합니다.
        /// 편집 중에는 검증 결과를 오래된 상태로 표시하고, 이 함수가 호출될 때만 실제 검증 결과를 새로 계산합니다.
        /// </summary>
        private void ValidateCurrentTable()
        {
            _validationMessages.Clear();
            if (_selectedTable != null && _document != null)
                _validationMessages.AddRange(TableEditorValidator.Validate(_selectedTable, _document));
            _isValidationStale = false;
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
                ApplyDefaultRawValuesToRow(_selectedRow);
                AssignAutoUidIfNeeded(_selectedRow);
            });
        }

        private void ApplyDefaultRawValuesToRow(TableEditorDocumentRow row)
        {
            if (row == null || _document == null || _columns == null)
                return;

            for (int i = 0; i < _columns.Count; i++)
            {
                TableEditorColumnDefinition column = _columns[i];
                if (column == null || string.IsNullOrWhiteSpace(column.HeaderName) || column.IsUidColumn)
                    continue;

                if (!row.Values.TryGetValue(column.HeaderName, out string currentRaw) || !string.IsNullOrWhiteSpace(currentRaw))
                    continue;

                if (!TableEditorDefaultRawUtility.TryGetDefaultRaw(column, out string defaultRaw))
                    continue;

                _document.SetCellValue(row, column.HeaderName, defaultRaw);
            }
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

        /// <summary>
        /// Inspector 입력 필드의 셀 값 변경을 처리합니다.
        /// 일반 셀 편집은 전체 문서 스냅샷, 컬럼 재구성, 검증 목록 전체 재생성을 피하는 경량 경로로 처리합니다.
        /// </summary>
        /// <param name="headerName">변경된 컬럼 헤더입니다.</param>
        /// <param name="nextValue">변경 후 원본 문자열 값입니다.</param>
        private void HandleCellValueChanged(string headerName, string nextValue)
        {
            if (_document == null || _selectedRow == null)
                return;

            if (string.Equals(_selectedRow.Values.TryGetValue(headerName, out string current) ? current : string.Empty, nextValue ?? string.Empty, StringComparison.Ordinal))
                return;

            ITableEditorTableRuleProvider ruleProvider = TableEditorRuleProviderRegistry.GetProvider(_selectedTable);
            bool inspectorLayoutChanged = string.Equals(headerName, "Kind", StringComparison.OrdinalIgnoreCase);
            if (inspectorLayoutChanged)
            {
                ApplyDocumentMutation($"Edit {headerName}", () =>
                {
                    _document.SetCellValue(_selectedRow, headerName, nextValue);
                    ruleProvider?.OnBeforeCellValueChanged(_document, _selectedRow, headerName, nextValue);
                }, false);
                return;
            }

            ApplyCellValueMutation(headerName, nextValue);
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

        private void TryApplyPendingOpenRequest()
        {
            PendingOpenRequest request = _pendingOpenRequest;
            if (request == null || _tables == null || _tables.Count == 0)
                return;

            TableEditorTableDefinition requestTable = TableEditorRegistry.FindByKey(request.TableKey);
            if (requestTable == null)
            {
                _pendingOpenRequest = null;
                return;
            }

            if (_selectedTable != requestTable)
            {
                LoadTable(requestTable);
                return;
            }

            if (_document == null)
                return;

            _pendingOpenRequest = null;
            _selectedRow = _document.GetRows().FirstOrDefault(row =>
                row != null
                && row.Values.TryGetValue(request.HeaderName, out string currentValue)
                && string.Equals(currentValue ?? string.Empty, request.RawValue ?? string.Empty, StringComparison.Ordinal));

            RefreshAllViews();
            if (_selectedRow == null)
                return;

            int rowIndex = _visibleRows.IndexOf(_selectedRow);
            if (rowIndex >= 0)
            {
                _rowListView?.SetSelectionWithoutNotify(new[] { rowIndex });
                _rowListView?.ScrollToItem(rowIndex);
            }
        }

        /// <summary>
        /// 행 추가/삭제/복제처럼 문서 구조가 바뀌는 작업을 적용합니다.
        /// 구조 변경은 경량 셀 편집 기록만으로 복원하기 어렵기 때문에 전체 문서 스냅샷을 Undo 기준으로 사용합니다.
        /// </summary>
        /// <param name="undoName">Undo 메뉴에 표시될 작업 이름입니다.</param>
        /// <param name="mutation">실제로 적용할 문서 변경 로직입니다.</param>
        /// <param name="keepInspectorState">Inspector 재구성을 생략할지 여부입니다.</param>
        private void ApplyDocumentMutation(string undoName, Action mutation, bool keepInspectorState = false)
        {
            if (_selectedTable == null || _document == null || mutation == null)
                return;

            _undoController?.BeginRecord(undoName);
            mutation();
            _undoController?.CommitSnapshot(_selectedTable.TableKey, _document);
            MarkValidationStale();
            if (keepInspectorState)
                RefreshViewsWithoutInspectorRebuild();
            else
                RefreshAllViews();
        }

        /// <summary>
        /// 일반 셀 값 변경을 경량 경로로 적용합니다.
        /// 전체 문서 JSON 직렬화, 테이블 ReloadAction, 컬럼 재생성을 생략하여 입력 지연을 줄입니다.
        /// </summary>
        /// <param name="headerName">변경된 컬럼 헤더입니다.</param>
        /// <param name="nextValue">변경 후 원본 문자열 값입니다.</param>
        private void ApplyCellValueMutation(string headerName, string nextValue)
        {
            if (_selectedTable == null || _document == null || _selectedRow == null)
                return;

            int rowStableId = _selectedRow.stableId;
            _undoController?.BeginRecord($"Edit {headerName}");
            if (!_document.SetCellValue(_selectedRow, headerName, nextValue))
                return;

            _undoController?.CommitCellEdit(_selectedTable.TableKey, rowStableId, headerName, nextValue);
            MarkValidationStale();
            RefreshViewsAfterCellValueChanged(headerName);
        }

        /// <summary>
        /// Undo/Redo 상태에서 복원된 문서를 현재 창에 반영합니다.
        /// UndoController가 기준 스냅샷과 셀 편집 기록을 조합한 문서를 전달하므로 창은 선택 행만 다시 연결합니다.
        /// </summary>
        /// <param name="tableKey">복원 대상 테이블 식별자입니다.</param>
        /// <param name="restored">복원된 문서입니다.</param>
        private void HandleUndoRedoRestore(string tableKey, TableEditorDocument restored)
        {
            if (_selectedTable == null || !string.Equals(_selectedTable.TableKey, tableKey, StringComparison.OrdinalIgnoreCase))
                return;

            if (restored == null)
                return;

            int selectedStableId = _selectedRow?.stableId ?? -1;
            _document = restored;
            _columns = _selectedTable.BuildColumns(_document.Headers);
            _document.MergeHeaders(_columns);
            _selectedRow = _document.GetRows().FirstOrDefault(r => r.stableId == selectedStableId) ?? _document.GetRows().FirstOrDefault();
            MarkValidationStale();
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

        /// <summary>
        /// Inspector 상태를 유지한 상태로 필요한 뷰만 갱신합니다.
        /// 컬럼 구조는 변경되지 않았다고 가정하므로 컬럼 재생성을 수행하지 않습니다.
        /// </summary>
        private void RefreshViewsWithoutInspectorRebuild()
        {
            RefreshStatus();
            RefreshVisibleRows();
            RebuildValidation();
        }

        /// <summary>
        /// 셀 값 변경 후 그리드와 상태 표시만 가볍게 갱신합니다.
        /// 검색/검증 필터가 켜져 있을 때만 보이는 행 목록을 다시 계산하고, 일반 상황에서는 표시 중인 항목만 새로 바인딩합니다.
        /// </summary>
        /// <param name="changedHeaderName">변경된 컬럼 헤더입니다.</param>
        private void RefreshViewsAfterCellValueChanged(string changedHeaderName)
        {
            RefreshStatus();

            if (ShouldRebuildVisibleRowsAfterCellValueChanged(changedHeaderName))
                RefreshVisibleRows();
            else
                RefreshRowListItems();

            if (_isValidationStale)
                RebuildValidation();
        }

        /// <summary>
        /// 셀 변경이 현재 행 필터 결과에 영향을 줄 수 있는지 판단합니다.
        /// 검색 문자열이나 검증 행 필터가 활성화되어 있으면 보이는 행 목록 자체를 다시 계산해야 합니다.
        /// </summary>
        /// <param name="changedHeaderName">변경된 컬럼 헤더입니다.</param>
        /// <returns>행 목록 재계산이 필요하면 true입니다.</returns>
        private bool ShouldRebuildVisibleRowsAfterCellValueChanged(string changedHeaderName)
        {
            return !string.IsNullOrWhiteSpace(_rowSearch) || _showOnlyValidationRows;
        }

        /// <summary>
        /// 행 목록을 재계산하지 않고 현재 MultiColumnListView 항목만 다시 바인딩합니다.
        /// 컬럼 재구성과 전체 Rebuild보다 비용이 작아 일반 셀 편집에 적합합니다.
        /// </summary>
        private void RefreshRowListItems()
        {
            _rowListView?.RefreshItems();
            if (_selectedRow == null)
                return;

            int index = _visibleRows.IndexOf(_selectedRow);
            if (index >= 0)
                _rowListView?.SetSelectionWithoutNotify(new[] { index });
        }

        private void RefreshStatus()
        {
            if (_pathLabel != null)
                _pathLabel.text = _selectedTable != null ? _selectedTable.AssetPath : string.Empty;
            if (_statusLabel != null)
                _statusLabel.text = _document != null && _document.IsDirty ? "Modified" : string.Empty;
            if (_shopProbabilityButton != null)
                _shopProbabilityButton.style.display = IsShopTableSelected() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private bool IsShopTableSelected()
        {
            return _selectedTable != null
                   && string.Equals(_selectedTable.TableKey, ConfigAddressableTable.ShopItem, StringComparison.OrdinalIgnoreCase);
        }

        private void OpenShopProbabilityWindow()
        {
            if (!IsShopTableSelected() || _document == null)
                return;

            ShopProbabilityResultWindow.Open(_document);
        }

        private void RefreshVisibleRows()
        {
            _visibleRows.Clear();
            if (_document != null)
            {
                IEnumerable<TableEditorDocumentRow> rows = _document.GetRows();
                HashSet<int> invalidRowIds = _showOnlyValidationRows ? BuildInvalidRowIdSet() : null;

                foreach (TableEditorDocumentRow row in rows)
                {
                    if (!string.IsNullOrWhiteSpace(_rowSearch) && !MatchesSearch(row, _rowSearch))
                        continue;
                    if (_showOnlyValidationRows && (invalidRowIds == null || !invalidRowIds.Contains(row.stableId)))
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
            _validationHost.Add(TableEditorGui.BuildValidationView(_validationMessages, _selectedRow?.stableId ?? -1, _showOnlySelectedValidation, _isValidationStale));
        }

        /// <summary>
        /// 기존 검증 결과를 오래된 상태로 표시합니다.
        /// 셀 편집 때마다 전체 검증을 다시 실행하지 않기 위해 Validate 버튼으로 재검증하도록 유도합니다.
        /// </summary>
        private void MarkValidationStale()
        {
            if (_validationMessages.Count > 0)
                _isValidationStale = true;
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
