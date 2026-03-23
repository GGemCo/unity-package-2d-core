using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// CrowdControl 공통 테이블 테스트/편집 EditorWindow.
    /// - crowd_control 공통 Row를 직접 편집하고,
    ///   (1) 플레이 모드에서 즉시 적용/테스트
    ///   (2) 테이블 파일(crowd_control.txt)에 저장
    /// 할 수 있습니다.
    /// - KnockBack / KnockDown / KnockUp 상세 값은 타입별 전용 툴에서 편집합니다.
    /// </summary>
    public sealed class UseCrowdControl : DefaultEditorWindow
    {
        private const string Title = "CrowdControl 사용툴";

        [MenuItem(ConfigEditor.NameToolUseCrowdControl, false, (int)ConfigEditor.ToolOrdering.UseCrowdControl)]
        public static void ShowWindow() => GetWindow<UseCrowdControl>(Title);

        public static void OpenAndSelect(int uid)
        {
            UseCrowdControlSelectionBridge.PendingCrowdControlUid = uid;
            GetWindow<UseCrowdControl>(Title).Show();
        }

        [Header("대상")]
        [SerializeField] private GameObject _target;
        [SerializeField] private GameObject _source;

        [Header("정의(테이블)")]
        [SerializeField] private int crowdControlUid;

        private TableCrowdControl _tableCrowdControl;
        private Dictionary<int, StruckTableCrowdControl> _tableDictionary;
        private readonly List<SearchableDropdownUtility.Option<StruckTableCrowdControl>> _dropDownOptions = new();

        private bool _foldRowEdit = true;
        private StruckTableCrowdControl _cachedRow;
        private StruckTableCrowdControl _editingRow;
        private bool _editingDirty;
        private string _lastReloadMessage = string.Empty;
        private Vector2 _scroll;

        private static readonly HashSet<string> LegacyDetailMembers = new(StringComparer.Ordinal)
        {
            nameof(StruckTableCrowdControl.Height),
            nameof(StruckTableCrowdControl.EndYMode),
            nameof(StruckTableCrowdControl.EndYOffset),
            nameof(StruckTableCrowdControl.EndYAbsolute),
            nameof(StruckTableCrowdControl.DownWaitTime),
            nameof(StruckTableCrowdControl.RecoverTime),
            nameof(StruckTableCrowdControl.IsStopOnWall),
            nameof(StruckTableCrowdControl.IsGroundOnly),
            nameof(StruckTableCrowdControl.IsAirOnly),
        };

        private static readonly TableRowEditorUtility.TableRowEditorField[] RowEditorFields = BuildCommonRowFields();

        protected override void OnEnable()
        {
            base.OnEnable();
            ReloadTable(preserveSelection: true);
            TryConsumePendingSelection();
            CacheRow();
        }

        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scrollScope.scrollPosition;
                EditorGUILayout.Space(6);

                DrawTargetSection();
                EditorGUILayout.Space(6);

                DrawTableSelectionSection();
                EditorGUILayout.Space(6);

                DrawCommonRowEditorSection();
                EditorGUILayout.Space(6);

                DrawDetailToolSection();
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

        private void DrawTableSelectionSection()
        {
            EditorGUILayout.LabelField("공통 테이블 선택", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_dropDownOptions.Count <= 0)
                {
                    EditorGUILayout.HelpBox("crowd_control 테이블 Row를 불러오지 못했습니다.", MessageType.Error);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("CrowdControl");
                    string currentText = _cachedRow != null ? BuildDropdownValue(_cachedRow) : "선택...";
                    int selectedIndex = _cachedRow?.Uid ?? 0;

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _dropDownOptions,
                        selectedIndex: selectedIndex,
                        onSelected: (_, option) =>
                        {
                            crowdControlUid = option.Data?.Uid ?? 0;
                            CacheRow();
                            Repaint();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                EditorGUI.BeginChangeCheck();
                int newUid = EditorGUILayout.IntField("Uid", crowdControlUid);
                if (EditorGUI.EndChangeCheck())
                {
                    crowdControlUid = Mathf.Max(0, newUid);
                    CacheRow();
                }
            }
        }

        private void DrawCommonRowEditorSection()
        {
            EditorGUILayout.LabelField("공통 Row 편집", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_editingRow == null)
                {
                    EditorGUILayout.HelpBox("편집할 공통 Row를 선택하세요.", MessageType.Info);
                    return;
                }

                _foldRowEdit = EditorGUILayout.Foldout(_foldRowEdit, "공통 Row 편집", true);
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
                            CacheRow();
                    }

                    using (new EditorGUI.DisabledScope(_editingRow == null))
                    {
                        if (GUILayout.Button("편집값 적용", GUILayout.Height(24)))
                            CommitEditingIfNeeded();
                    }
                }
            }
        }

        private void DrawDetailToolSection()
        {
            EditorGUILayout.LabelField("타입별 상세 설정", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_cachedRow == null)
                {
                    EditorGUILayout.HelpBox("Row를 선택하면 상세 설정 툴로 이동할 수 있습니다.", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField($"현재 타입: {_cachedRow.Type}");
                EditorGUILayout.HelpBox(
                    "Height / DownWaitTime / EndYMode / RecoverTime / IsStopOnWall / IsGroundOnly / IsAirOnly 등 상세 컬럼은 타입별 상세 테이블에서 관리합니다.",
                    MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(GetOpenDetailAction(_cachedRow.Type) == null))
                    {
                        if (GUILayout.Button("상세 사용툴 열기", GUILayout.Height(24)))
                            GetOpenDetailAction(_cachedRow.Type)?.Invoke(_cachedRow.Uid);
                    }

                    if (GUILayout.Button("TableEditor(공통) 열기", GUILayout.Height(24)))
                        TableEditorWindow.OpenAndFocusRowByIntKey(ConfigAddressableTable.CrowdControl, "Uid", _cachedRow.Uid);

                    using (new EditorGUI.DisabledScope(GetDetailTableKey(_cachedRow.Type) == null))
                    {
                        if (GUILayout.Button("TableEditor(상세) 열기", GUILayout.Height(24)))
                            TableEditorWindow.OpenAndFocusRowByIntKey(GetDetailTableKey(_cachedRow.Type), "CrowdControlUid", _cachedRow.Uid);
                    }
                }
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
                            ApplyCommonRowToRuntime(_cachedRow);
                        }
                    }
                }

                EditorGUILayout.Space(4);

                using (new EditorGUI.DisabledScope(!Application.isPlaying || _cachedRow == null))
                {
                    if (GUILayout.Button("CrowdControl 적용", GUILayout.Height(24)))
                    {
                        CommitEditingIfNeeded();
                        ApplyCrowdControlToTarget();
                    }
                }
            }
        }

        private void DrawReloadSection()
        {
            DrawTableReloadSection(_lastReloadMessage, "crowd_control 재로딩", () => ReloadTable(preserveSelection: true));
        }

        private void ReloadTable(bool preserveSelection)
        {
            int previousUid = preserveSelection ? crowdControlUid : 0;
            try
            {
                _tableCrowdControl = TableLoaderManager.LoadCrowdControlTable(forceReload: true);
                _tableDictionary = _tableCrowdControl != null ? _tableCrowdControl.GetDatas() : new Dictionary<int, StruckTableCrowdControl>();
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
                source: _tableDictionary?.Values,
                targetOptions: _dropDownOptions,
                isValidRow: row => row != null && row.Uid > 0,
                keySelector: row => row.Uid.ToString(),
                valueSelector: BuildDropdownValue,
                assignSelected: row => { });
        }

        private int FindFirstUid()
        {
            return _dropDownOptions.Count > 0 ? _dropDownOptions[0].Data?.Uid ?? 0 : 0;
        }

        private void TryConsumePendingSelection()
        {
            int pendingUid = UseCrowdControlSelectionBridge.PendingCrowdControlUid;
            if (pendingUid > 0)
                crowdControlUid = pendingUid;
            UseCrowdControlSelectionBridge.PendingCrowdControlUid = 0;
        }

        private void CacheRow()
        {
            _cachedRow = null;
            _editingRow = null;
            _editingDirty = false;

            if (_tableDictionary == null || !_tableDictionary.TryGetValue(crowdControlUid, out StruckTableCrowdControl row) || row == null)
                return;

            _cachedRow = row;
            _editingRow = TableRowEditorUtility.CloneShallow<StruckTableCrowdControl>(row);
            NormalizeRow(_editingRow);
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

        private void NormalizeEditingFieldValue(object target, string memberName)
        {
            var row = target as StruckTableCrowdControl;
            if (row == null || string.IsNullOrWhiteSpace(memberName))
                return;

            switch (memberName)
            {
                case nameof(StruckTableCrowdControl.Distance):
                    if (row.Distance < 0f) row.Distance = 0f;
                    break;
                case nameof(StruckTableCrowdControl.Duration):
                    if (row.Duration < 0f) row.Duration = 0f;
                    break;
            }
        }

        private void NormalizeRow(StruckTableCrowdControl row)
        {
            if (row == null)
                return;

            foreach (var field in RowEditorFields)
                NormalizeEditingFieldValue(row, field.MemberName);
        }

        private void ApplyCommonRowToRuntime(StruckTableCrowdControl row)
        {
            if (row == null || !Application.isPlaying || !GGemCo2DCore.TableLoaderManager.Instance)
                return;

            StruckTableCrowdControl runtimeRow = GGemCo2DCore.TableLoaderManager.Instance.TableCrowdControl?.GetDataByUid(row.Uid);
            if (runtimeRow == null)
                return;

            TableRowEditorUtility.CopyMembers(row, runtimeRow, RowEditorFields);
        }

        private void ApplyCrowdControlToTarget()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "플레이 모드에서만 적용 가능합니다.", "OK");
                return;
            }

            if (_target == null)
            {
                EditorUtility.DisplayDialog(Title, "Target이 비어있습니다.", "OK");
                return;
            }

            CharacterCrowdControlController controller = _target.GetComponent<CharacterCrowdControlController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog(Title, "Target에 CharacterCrowdControlController가 없습니다.", "OK");
                return;
            }

            ApplyCommonRowToRuntime(_cachedRow);
            controller.ApplyCrowdControlByUid(crowdControlUid, _source);
        }

        private void TrySaveTable()
        {
            if (!TableTextRowPatchUtility.TryPatchRowByUid(
                    ConfigAddressableTable.TableCrowdControl.Path,
                    _cachedRow.Uid,
                    _cachedRow,
                    SerializeRow,
                    out string error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            _lastReloadMessage = $"테이블 저장 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            ReloadTable(preserveSelection: true);
            CacheRow();
        }

        private static string SerializeRow(StruckTableCrowdControl row, IReadOnlyList<string> headers)
        {
            string[] values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "Type" => row.Type.ToString(),
                    "DirectionType" => row.DirectionType.ToString(),
                    "FixedDirectionX" => MathHelper.FormatFloat(row.FixedDirectionX),
                    "FixedDirectionY" => MathHelper.FormatFloat(row.FixedDirectionY),
                    "Distance" => MathHelper.FormatFloat(row.Distance),
                    "EaseType" => row.EaseType.ToString(),
                    "Duration" => MathHelper.FormatFloat(row.Duration),
                    "Height" => MathHelper.FormatFloat(row.Height),
                    "EndYMode" => row.EndYMode.ToString(),
                    "EndYOffset" => MathHelper.FormatFloat(row.EndYOffset),
                    "EndYAbsolute" => MathHelper.FormatFloat(row.EndYAbsolute),
                    "DownWaitTime" => MathHelper.FormatFloat(row.DownWaitTime),
                    "RecoverTime" => MathHelper.FormatFloat(row.RecoverTime),
                    "IsLockControl" => MathHelper.FormatBool(row.IsLockControl),
                    "IsUseKnockbackStatus" => MathHelper.FormatBool(row.IsUseKnockbackStatus),
                    "IsUseDontControlStatus" => MathHelper.FormatBool(row.IsUseDontControlStatus),
                    "StaggerAnimationName" => row.StaggerAnimationName ?? string.Empty,
                    "IsStopOnWall" => MathHelper.FormatBool(row.IsStopOnWall),
                    "IsGroundOnly" => MathHelper.FormatBool(row.IsGroundOnly),
                    "IsAirOnly" => MathHelper.FormatBool(row.IsAirOnly),
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }

        private static string BuildDropdownValue(StruckTableCrowdControl row)
        {
            return row == null ? string.Empty : $"[{row.Type}] {row.Uid} - {row.Name}";
        }

        private static Action<int> GetOpenDetailAction(CrowdControlConstants.Type type)
        {
            return type switch
            {
                CrowdControlConstants.Type.KnockBack => UseCrowdControlKnockBack.OpenAndSelect,
                CrowdControlConstants.Type.KnockDown => UseCrowdControlKnockDown.OpenAndSelect,
                CrowdControlConstants.Type.KnockUp => UseCrowdControlKnockUp.OpenAndSelect,
                _ => null,
            };
        }

        private static string GetDetailTableKey(CrowdControlConstants.Type type)
        {
            return type switch
            {
                CrowdControlConstants.Type.KnockBack => ConfigAddressableTable.CrowdControlKnockBack,
                CrowdControlConstants.Type.KnockDown => ConfigAddressableTable.CrowdControlKnockDown,
                CrowdControlConstants.Type.KnockUp => ConfigAddressableTable.CrowdControlKnockUp,
                _ => null,
            };
        }

        private static TableRowEditorUtility.TableRowEditorField[] BuildCommonRowFields()
        {
            var options = new TableRowEditorUtility.TableRowEditorBuildOptions();
            options.ReadOnlyMembers.Add(nameof(StruckTableCrowdControl.Uid));
            options.GroupByMemberName[nameof(StruckTableCrowdControl.Uid)] = "Common";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.Name)] = "Common";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.Type)] = "Common";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.DirectionType)] = "Motion";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.FixedDirectionX)] = "Motion";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.FixedDirectionY)] = "Motion";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.Distance)] = "Motion";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.EaseType)] = "Motion";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.Duration)] = "Motion";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.IsLockControl)] = "State / Animation";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.IsUseKnockbackStatus)] = "State / Animation";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.IsUseDontControlStatus)] = "State / Animation";
            options.GroupByMemberName[nameof(StruckTableCrowdControl.StaggerAnimationName)] = "State / Animation";

            List<TableRowEditorUtility.TableRowEditorField> result = new();
            foreach (var field in TableRowEditorUtility.BuildFields<StruckTableCrowdControl>(options))
            {
                if (!LegacyDetailMembers.Contains(field.MemberName))
                    result.Add(field);
            }

            return result.ToArray();
        }
    }
}
