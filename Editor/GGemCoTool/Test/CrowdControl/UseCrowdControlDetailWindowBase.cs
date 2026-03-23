using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class UseCrowdControlSelectionBridge
    {
        public static int PendingCrowdControlUid;
    }

    public abstract class UseCrowdControlDetailWindowBase<TDetailRow> : DefaultEditorWindow
        where TDetailRow : StruckTableCrowdControlDetailBase, new()
    {
        [Header("대상")]
        [SerializeField] private GameObject _target;
        [SerializeField] private GameObject _source;

        [Header("정의(테이블)")]
        [SerializeField] private int crowdControlUid;

        private TableCrowdControl _tableCrowdControl;
        private Dictionary<int, StruckTableCrowdControl> _crowdControlRows;
        private Dictionary<int, TDetailRow> _detailRows;
        private readonly List<SearchableDropdownUtility.Option<StruckTableCrowdControl>> _dropdownOptions = new();

        private StruckTableCrowdControl _selectedCommonRow;
        private TDetailRow _cachedRow;
        private TDetailRow _editingRow;
        private bool _editingDirty;
        private bool _foldRowEdit = true;
        private string _lastReloadMessage = string.Empty;
        private Vector2 _scroll;
        private Vector2 _previewScroll;

        protected abstract string WindowTitle { get; }
        protected abstract string DropdownLabel { get; }
        protected abstract string ReloadButtonLabel { get; }
        protected abstract CrowdControlConstants.Type SupportedType { get; }
        protected abstract string DetailTableKey { get; }
        protected abstract string DetailTableAssetPath { get; }
        protected abstract IReadOnlyList<TableRowEditorUtility.TableRowEditorField> RowEditorFields { get; }

        protected override void OnEnable()
        {
            base.OnEnable();
            ReloadAllTables(preserveSelection: true);
            TryConsumePendingSelection();
            CacheSelectedRows();
        }

        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scrollScope.scrollPosition;
                EditorGUILayout.Space(6);

                DrawTargetSection();
                EditorGUILayout.Space(6);

                DrawTableSection();
                EditorGUILayout.Space(6);

                DrawCommonSummarySection();
                EditorGUILayout.Space(6);

                DrawRowEditorSection();
                EditorGUILayout.Space(6);

                DrawPreviewSection();
                EditorGUILayout.Space(8);

                DrawBottomButtons();
                EditorGUILayout.Space(6);

                DrawReloadSection();
                EditorGUILayout.Space(20);
            }
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("대상 선택", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _target = (GameObject)EditorGUILayout.ObjectField("Target", _target, typeof(GameObject), true);
                _source = (GameObject)EditorGUILayout.ObjectField("Source", _source, typeof(GameObject), true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Selection → Target", GUILayout.Height(22)))
                        _target = Selection.activeGameObject;

                    if (GUILayout.Button("Selection → Source", GUILayout.Height(22)))
                        _source = Selection.activeGameObject;
                }

                if (_target == null)
                    EditorGUILayout.HelpBox("Target이 비어있습니다. Hierarchy에서 캐릭터를 선택 후 지정하세요.", MessageType.Warning);
            }
        }

        private void DrawTableSection()
        {
            EditorGUILayout.LabelField("테이블 선택", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_dropdownOptions.Count <= 0)
                {
                    EditorGUILayout.HelpBox($"{SupportedType} 타입의 crowd_control Row를 찾지 못했습니다.", MessageType.Warning);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(DropdownLabel);
                    string currentText = _selectedCommonRow != null ? BuildDropdownValue(_selectedCommonRow) : "선택...";
                    int selectedIndex = _selectedCommonRow?.Uid ?? 0;

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _dropdownOptions,
                        selectedIndex: selectedIndex,
                        onSelected: (_, option) =>
                        {
                            crowdControlUid = option.Data?.Uid ?? 0;
                            CacheSelectedRows();
                            Repaint();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                EditorGUI.BeginChangeCheck();
                int newUid = EditorGUILayout.IntField("CrowdControlUid", crowdControlUid);
                if (EditorGUI.EndChangeCheck())
                {
                    crowdControlUid = Mathf.Max(0, newUid);
                    CacheSelectedRows();
                }
            }
        }

        private void DrawCommonSummarySection()
        {
            EditorGUILayout.LabelField("공통 Row 요약", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_selectedCommonRow == null)
                {
                    EditorGUILayout.HelpBox("선택된 공통 CrowdControl Row가 없습니다.", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField($"Uid: {_selectedCommonRow.Uid}");
                EditorGUILayout.LabelField($"Name: {_selectedCommonRow.Name}");
                EditorGUILayout.LabelField($"Type: {_selectedCommonRow.Type}");
                EditorGUILayout.LabelField($"DirectionType: {_selectedCommonRow.DirectionType}");
                EditorGUILayout.LabelField($"Distance / Duration: {_selectedCommonRow.Distance} / {_selectedCommonRow.Duration}");
                EditorGUILayout.LabelField($"Status: Knockback={_selectedCommonRow.IsUseKnockbackStatus}, DontControl={_selectedCommonRow.IsUseDontControlStatus}");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("공통 사용툴 열기", GUILayout.Height(22)))
                        UseCrowdControl.OpenAndSelect(_selectedCommonRow.Uid);

                    if (GUILayout.Button("TableEditor(공통) 열기", GUILayout.Height(22)))
                        TableEditorWindow.OpenAndFocusRowByIntKey(ConfigAddressableTable.CrowdControl, "Uid", _selectedCommonRow.Uid);
                }
            }
        }

        private void DrawRowEditorSection()
        {
            EditorGUILayout.LabelField("상세 Row 편집", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_editingRow == null)
                {
                    EditorGUILayout.HelpBox("편집할 상세 Row가 없습니다.", MessageType.Info);
                    return;
                }

                if (_detailRows != null && _detailRows.ContainsKey(crowdControlUid))
                    EditorGUILayout.HelpBox("기존 상세 Row를 편집중입니다.", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("상세 Row가 없어 공통 테이블 fallback 값으로 임시 생성했습니다. 저장 시 상세 테이블에 새 Row가 추가됩니다.", MessageType.Warning);

                _foldRowEdit = EditorGUILayout.Foldout(_foldRowEdit, "상세 Row 편집", true);
                if (!_foldRowEdit)
                    return;

                var result = TableRowEditorUtility.DrawObjectEditor(_editingRow, RowEditorFields, NormalizeEditingFieldValue);
                if (result.Changed)
                    _editingDirty = true;

                EditorGUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_cachedRow == null))
                    {
                        if (GUILayout.Button("편집값 되돌리기", GUILayout.Height(24)))
                            CacheSelectedRows();
                    }

                    using (new EditorGUI.DisabledScope(_editingRow == null))
                    {
                        if (GUILayout.Button("편집값 적용", GUILayout.Height(24)))
                        {
                            CommitEditingIfNeeded();
                        }
                    }
                }
            }
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_selectedCommonRow == null || _editingRow == null)
                {
                    EditorGUILayout.HelpBox("Row를 선택하면 미리보기를 확인할 수 있습니다.", MessageType.Info);
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[{SupportedType}] {_selectedCommonRow.Uid} - {_selectedCommonRow.Name}");
                sb.AppendLine($"- DirectionType: {_selectedCommonRow.DirectionType}");
                sb.AppendLine($"- Distance: {_selectedCommonRow.Distance}");
                sb.AppendLine($"- EaseType: {_selectedCommonRow.EaseType}");
                sb.AppendLine($"- Duration: {_selectedCommonRow.Duration}");
                sb.AppendLine($"- Status: Knockback={_selectedCommonRow.IsUseKnockbackStatus}, DontControl={_selectedCommonRow.IsUseDontControlStatus}");
                sb.AppendLine();
                AppendDetailPreview(sb, _editingRow);

                _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.MinHeight(120f));
                EditorGUILayout.TextArea(sb.ToString());
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawBottomButtons()
        {
            EditorGUILayout.LabelField("실행 / 저장", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_cachedRow == null))
                    {
                        if (GUILayout.Button("테이블 파일 저장", GUILayout.Height(24)))
                        {
                            CommitEditingIfNeeded();
                            TrySaveTable();
                        }
                    }

                    using (new EditorGUI.DisabledScope(!Application.isPlaying || _cachedRow == null))
                    {
                        if (GUILayout.Button("인게임 테이블 적용", GUILayout.Height(24)))
                        {
                            CommitEditingIfNeeded();
                            ApplyRowToRuntime(_cachedRow);
                        }
                    }
                }

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_selectedCommonRow == null))
                    {
                        if (GUILayout.Button("TableEditor(상세) 열기", GUILayout.Height(22)))
                            TableEditorWindow.OpenAndFocusRowByIntKey(DetailTableKey, "CrowdControlUid", _selectedCommonRow.Uid);
                    }

                    using (new EditorGUI.DisabledScope(!Application.isPlaying || _selectedCommonRow == null))
                    {
                        if (GUILayout.Button("CrowdControl 적용", GUILayout.Height(22)))
                        {
                            CommitEditingIfNeeded();
                            ApplyCrowdControlToTarget();
                        }
                    }
                }
            }
        }

        private void DrawReloadSection()
        {
            DrawTableReloadSection(_lastReloadMessage, ReloadButtonLabel, () => ReloadAllTables(preserveSelection: true));
        }

        private void ReloadAllTables(bool preserveSelection)
        {
            int previousUid = preserveSelection ? crowdControlUid : 0;
            try
            {
                _tableCrowdControl = TableLoaderManager.LoadCrowdControlTable(forceReload: true);
                _crowdControlRows = _tableCrowdControl != null
                    ? _tableCrowdControl.GetDatas()
                    : new Dictionary<int, StruckTableCrowdControl>();

                _detailRows = LoadDetailRows();
                RebuildDropdown();

                crowdControlUid = previousUid > 0 ? previousUid : FindFirstUid();
                _lastReloadMessage = $"테이블 재로딩 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _lastReloadMessage = $"테이블 재로딩 실패: {e.GetType().Name} - {e.Message}";
            }

            Repaint();
        }

        private void RebuildDropdown()
        {
            RebuildDropdownOptions(
                source: _crowdControlRows?.Values,
                targetOptions: _dropdownOptions,
                isValidRow: row => row != null && row.Uid > 0,
                keySelector: row => row.Uid.ToString(CultureInfo.InvariantCulture),
                valueSelector: BuildDropdownValue,
                assignSelected: row => _selectedCommonRow = row,
                filter: row => row.Type == SupportedType);
        }

        private int FindFirstUid()
        {
            if (_dropdownOptions.Count <= 0)
                return 0;

            return _dropdownOptions[0].Data?.Uid ?? 0;
        }

        private void TryConsumePendingSelection()
        {
            int pendingUid = UseCrowdControlSelectionBridge.PendingCrowdControlUid;
            if (pendingUid <= 0)
                return;

            if (_crowdControlRows != null && _crowdControlRows.TryGetValue(pendingUid, out StruckTableCrowdControl row) && row != null && row.Type == SupportedType)
                crowdControlUid = pendingUid;

            UseCrowdControlSelectionBridge.PendingCrowdControlUid = 0;
        }

        private void CacheSelectedRows()
        {
            _selectedCommonRow = FindCommonRowByUid(crowdControlUid);
            _cachedRow = BuildWorkingDetailRow(_selectedCommonRow);
            _editingRow = CloneDetailRow(_cachedRow);
            NormalizeRow(_editingRow);
            _editingDirty = false;
        }

        private StruckTableCrowdControl FindCommonRowByUid(int uid)
        {
            if (uid <= 0 || _crowdControlRows == null || !_crowdControlRows.TryGetValue(uid, out StruckTableCrowdControl row))
                return null;

            return row != null && row.Type == SupportedType ? row : null;
        }

        private TDetailRow BuildWorkingDetailRow(StruckTableCrowdControl commonRow)
        {
            if (commonRow == null)
                return null;

            if (_detailRows != null && _detailRows.TryGetValue(commonRow.Uid, out TDetailRow detailRow) && detailRow != null)
                return CloneDetailRow(detailRow);

            return CreateDetailRowFromCommon(commonRow);
        }

        private bool ApplyEditingToCachedRow()
        {
            if (_cachedRow == null || _editingRow == null)
                return false;

            TableRowEditorUtility.CopyMembers(_editingRow, _cachedRow, RowEditorFields);
            NormalizeRow(_cachedRow);
            return true;
        }

        private void CommitEditingIfNeeded()
        {
            if (!_editingDirty)
                return;

            if (ApplyEditingToCachedRow())
                _editingDirty = false;
        }

        private void ApplyCrowdControlToTarget()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(WindowTitle, "플레이 모드에서만 적용 가능합니다.", "OK");
                return;
            }

            if (_target == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Target이 비어있습니다.", "OK");
                return;
            }

            CharacterCrowdControlController controller = _target.GetComponent<CharacterCrowdControlController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Target에 CharacterCrowdControlController가 없습니다.", "OK");
                return;
            }

            ApplyRowToRuntime(_cachedRow);
            controller.ApplyCrowdControlByUid(crowdControlUid, _source);
        }

        private void TrySaveTable()
        {
            if (!TrySaveTableFile(_cachedRow, out string error))
            {
                EditorUtility.DisplayDialog(WindowTitle, error, "OK");
                return;
            }

            ApplyRowToEditorCache(_cachedRow);
            _lastReloadMessage = $"테이블 저장 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            ReloadAllTables(preserveSelection: true);
            CacheSelectedRows();
        }

        protected virtual void NormalizeEditingFieldValue(object target, string memberName)
        {
        }

        private void NormalizeRow(TDetailRow row)
        {
            if (row == null)
                return;

            foreach (var field in RowEditorFields)
                NormalizeEditingFieldValue(row, field.MemberName);
        }

        private string BuildDropdownValue(StruckTableCrowdControl row)
        {
            return row == null
                ? string.Empty
                : $"[{row.Type}] {row.Uid} - {row.Name}";
        }

        private void AppendDetailPreview(StringBuilder sb, TDetailRow row)
        {
            if (row == null)
                return;

            sb.AppendLine("[Detail]");
            sb.AppendLine($"- CrowdControlUid: {row.CrowdControlUid}");
            AppendSpecificPreview(sb, row);
            sb.AppendLine($"- EndYMode: {row.EndYMode}");
            sb.AppendLine($"- EndYOffset: {row.EndYOffset}");
            sb.AppendLine($"- EndYAbsolute: {row.EndYAbsolute}");
            sb.AppendLine($"- RecoverTime: {row.RecoverTime}");
            sb.AppendLine($"- IsStopOnWall: {row.IsStopOnWall}");
            sb.AppendLine($"- IsGroundOnly: {row.IsGroundOnly}");
            sb.AppendLine($"- IsAirOnly: {row.IsAirOnly}");
        }

        private void ApplyRowToEditorCache(TDetailRow row)
        {
            if (row == null)
                return;

            _detailRows ??= new Dictionary<int, TDetailRow>();
            _detailRows[row.CrowdControlUid] = CloneDetailRow(row);
        }

        private void ApplyRowToRuntime(TDetailRow row)
        {
            if (row == null || !Application.isPlaying || !GGemCo2DCore.TableLoaderManager.Instance)
                return;

            Dictionary<int, TDetailRow> runtimeRows = GetRuntimeRows(GGemCo2DCore.TableLoaderManager.Instance);
            if (runtimeRows == null)
                return;

            if (runtimeRows.TryGetValue(row.CrowdControlUid, out TDetailRow runtimeRow) && runtimeRow != null)
                TableRowEditorUtility.CopyMembers(row, runtimeRow, RowEditorFields);
            else
                runtimeRows[row.CrowdControlUid] = CloneDetailRow(row);
        }

        private bool TrySaveTableFile(TDetailRow row, out string error)
        {
            error = null;
            if (row == null)
            {
                error = "저장할 상세 Row가 없습니다.";
                return false;
            }

            if (!TableTextRowPatchUtility.TryPatchRowByIntKey(
                    DetailTableAssetPath,
                    "CrowdControlUid",
                    row.CrowdControlUid,
                    row,
                    SerializeRow,
                    out error))
            {
                error = $"상세 테이블 저장 중 오류: {error}";
                return false;
            }

            TableLoaderManagerBase.Unload(DetailTableAssetPath);
            return true;
        }

        private string SerializeRow(TDetailRow row, IReadOnlyList<string> headers)
        {
            string[] values = new string[headers.Count];
            Type rowType = row?.GetType();

            for (int i = 0; i < headers.Count; i++)
            {
                object memberValue = TableEditorReflectionUtility.TryGetMemberValue(row, rowType, headers[i]);
                values[i] = SerializeValue(memberValue);
            }

            return string.Join("\t", values);
        }

        private static string SerializeValue(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is string text)
                return text;
            if (value is bool boolean)
                return MathHelper.FormatBool(boolean);
            if (value is float floatValue)
                return MathHelper.FormatFloat(floatValue);
            if (value is double doubleValue)
                return MathHelper.FormatFloat((float)doubleValue);
            if (value is int intValue)
                return intValue.ToString(CultureInfo.InvariantCulture);
            if (value is long longValue)
                return longValue.ToString(CultureInfo.InvariantCulture);
            if (value is Enum enumValue)
                return enumValue.ToString();

            return value.ToString() ?? string.Empty;
        }

        protected abstract Dictionary<int, TDetailRow> LoadDetailRows();
        protected abstract TDetailRow CloneDetailRow(TDetailRow row);
        protected abstract TDetailRow CreateDetailRowFromCommon(StruckTableCrowdControl commonRow);
        protected abstract void AppendSpecificPreview(StringBuilder sb, TDetailRow row);
        protected abstract Dictionary<int, TDetailRow> GetRuntimeRows(GGemCo2DCore.TableLoaderManager runtimeLoader);
    }
}
