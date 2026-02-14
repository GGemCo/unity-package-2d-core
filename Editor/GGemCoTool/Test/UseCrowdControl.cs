using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// CrowdControl 테스트/편집 EditorWindow.
    /// - crowd_control 테이블 Row를 직접 편집하고,
    ///   (1) 플레이 모드에서 즉시 적용/테스트
    ///   (2) 테이블 파일(crowd_control.txt)에 저장
    /// 할 수 있습니다.
    ///
    /// NOTE
    /// - EditorPrefs 저장은 사용하지 않습니다.
    /// - 런타임 오버라이드는 제거했습니다.
    /// - 읽기 전용 표시 영역을 제거하고, 편집 영역에서 바로 수정합니다.
    /// - 대상 선택 섹션을 최상단에 배치했습니다(UseProjectile UX 참고).
    /// </summary>
    public sealed class UseCrowdControl : DefaultEditorWindow
    {
        private const string Title = "CrowdControl 사용툴";

        [MenuItem(ConfigEditor.NameToolUseCrowdControl, false, (int)ConfigEditor.ToolOrdering.UseCrowdControl)]
        public static void ShowWindow() => GetWindow<UseCrowdControl>(Title);

        // ------------------------------
        // Target
        // ------------------------------
        [Header("대상")]
        [SerializeField] private GameObject _target;
        [SerializeField] private GameObject _source;

        // ------------------------------
        // Table
        // ------------------------------
        [Header("정의(테이블)")]
        [Tooltip("crowd_control 테이블 Uid")]
        [SerializeField] private int crowdControlUid;

        private TableCrowdControl _tableCrowdControl;
        private Dictionary<int, StruckTableCrowdControl> _tableDictionary;

        private readonly List<string> _names = new();
        private readonly List<int> _uids = new();
        private int _selectedIndex;

        // ------------------------------
        // Editing
        // ------------------------------
        [SerializeField] private bool _foldRowEdit = true;
        private StruckTableCrowdControl _cachedRow;
        private StruckTableCrowdControl _editingRow;
        private bool _editingDirty;

        private Vector2 _scroll;

        protected override void OnEnable()
        {
            base.OnEnable();

            ReloadTable(preserveSelection: true);
            CacheRow();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("CrowdControl 테스트/편집(테이블 저장 + 인게임 적용)", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4);

                    DrawTargetSection();
                    EditorGUILayout.Space(6);

                    DrawTableSelectionSection();
                    EditorGUILayout.Space(6);

                    DrawRowEditorSection();
                    EditorGUILayout.Space(10);

                    DrawBottomButtons();
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        // ==============================
        // Sections
        // ==============================
        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("대상 선택", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _target = (GameObject)EditorGUILayout.ObjectField("Target", _target, typeof(GameObject), true);
                _source = (GameObject)EditorGUILayout.ObjectField("Source", _source, typeof(GameObject), true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Selection → Target"))
                        _target = Selection.activeGameObject;

                    if (GUILayout.Button("Selection → Source"))
                        _source = Selection.activeGameObject;
                }

                if (_target == null)
                {
                    EditorGUILayout.HelpBox("Target이 비어있습니다. Hierarchy에서 캐릭터를 선택 후 지정하세요.", MessageType.Warning);
                }
            }
        }

        private void DrawTableSelectionSection()
        {
            EditorGUILayout.LabelField("테이블 선택", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_names.Count <= 0)
                {
                    EditorGUILayout.HelpBox("crowd_control 테이블을 불러오지 못했습니다. 테이블 경로/설정을 확인해주세요.", MessageType.Error);
                    return;
                }

                if (_selectedIndex >= _names.Count)
                    _selectedIndex = 0;

                EditorGUI.BeginChangeCheck();
                _selectedIndex = EditorGUILayout.Popup("CrowdControl", _selectedIndex, _names.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    crowdControlUid = _uids[_selectedIndex];
                    CacheRow();
                }

                EditorGUI.BeginChangeCheck();
                int newUid = EditorGUILayout.IntField("Uid", crowdControlUid);
                if (EditorGUI.EndChangeCheck())
                {
                    crowdControlUid = Mathf.Max(0, newUid);
                    SyncSelectedIndexByUid();
                    CacheRow();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("테이블 다시 불러오기"))
                    {
                        ReloadTable(preserveSelection: true);
                        CacheRow();
                        ShowNotification(new GUIContent("테이블 리로드 완료"));
                    }

                    using (new EditorGUI.DisabledScope(!_editingDirty))
                    {
                        if (GUILayout.Button("되돌리기"))
                        {
                            _editingRow = CloneRow(_cachedRow);
                            _editingDirty = false;
                            GUI.FocusControl(null);
                        }
                    }
                }
            }
        }

        private void DrawRowEditorSection()
        {
            if (_cachedRow == null || _editingRow == null)
            {
                EditorGUILayout.HelpBox("선택된 CrowdControl 데이터가 없습니다.", MessageType.Info);
                return;
            }

            _foldRowEdit = EditorGUILayout.Foldout(_foldRowEdit, "CrowdControl 테이블 편집(선택 Row)", true);
            if (!_foldRowEdit) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(new GUIContent("Uid"), _editingRow.Uid);
                }

                EditorGUI.BeginChangeCheck();

                _editingRow.Id = EditorGUILayout.TextField(new GUIContent("Id"), _editingRow.Id ?? string.Empty);
                _editingRow.Type = (CrowdControlConstants.Type)EditorGUILayout.EnumPopup(new GUIContent("Type"), _editingRow.Type);
                _editingRow.DirectionType = (CrowdControlConstants.DirectionType)EditorGUILayout.EnumPopup(new GUIContent("DirectionType"), _editingRow.DirectionType);

                _editingRow.FixedDirectionX = EditorGUILayout.FloatField(new GUIContent("FixedDirectionX"), _editingRow.FixedDirectionX);
                _editingRow.FixedDirectionY = EditorGUILayout.FloatField(new GUIContent("FixedDirectionY"), _editingRow.FixedDirectionY);

                _editingRow.Distance = EditorGUILayout.FloatField(new GUIContent("Distance"), _editingRow.Distance);
                if (_editingRow.Distance < 0f) _editingRow.Distance = 0f;

                _editingRow.EaseType = (Easing.EaseType)EditorGUILayout.EnumPopup(new GUIContent("EaseType"), _editingRow.EaseType);

                _editingRow.Duration = EditorGUILayout.FloatField(new GUIContent("Duration"), _editingRow.Duration);
                if (_editingRow.Duration < 0f) _editingRow.Duration = 0f;

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Flags", EditorStyles.miniBoldLabel);
                _editingRow.IsLockControl = EditorGUILayout.ToggleLeft("IsLockControl", _editingRow.IsLockControl);
                _editingRow.IsUseKnockbackStatus = EditorGUILayout.ToggleLeft("IsUseKnockbackStatus", _editingRow.IsUseKnockbackStatus);
                _editingRow.IsUseDontControlStatus = EditorGUILayout.ToggleLeft("IsUseDontControlStatus", _editingRow.IsUseDontControlStatus);

                _editingRow.StaggerAnimationType = (CrowdControlConstants.StaggerAnimationType)EditorGUILayout.EnumPopup(
                    new GUIContent("StaggerAnimationType"), _editingRow.StaggerAnimationType);

                _editingRow.IsStopOnWall = EditorGUILayout.ToggleLeft("IsStopOnWall", _editingRow.IsStopOnWall);
                _editingRow.IsGroundOnly = EditorGUILayout.ToggleLeft("IsGroundOnly", _editingRow.IsGroundOnly);
                _editingRow.IsAirOnly = EditorGUILayout.ToggleLeft("IsAirOnly", _editingRow.IsAirOnly);

                if (EditorGUI.EndChangeCheck())
                    _editingDirty = true;
            }
        }

        private void DrawBottomButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_editingDirty))
                {
                    if (GUILayout.Button("테스트 적용(인게임 테이블 반영)"))
                    {
                        if (!ApplyEditingToCachedRow())
                            return;

                        UpdateInGameTableInfo(_cachedRow);
                        _editingDirty = false;
                        ShowNotification(new GUIContent("인게임 테이블 반영 완료"));
                    }
                }

                if (GUILayout.Button("저장(테이블 파일)"))
                {
                    if (!ApplyEditingToCachedRow())
                        return;

                    if (!TrySaveCrowdControlTableFile(out var err))
                    {
                        EditorUtility.DisplayDialog(Title, err, "OK");
                        return;
                    }

                    // 저장 후 재로드(툴 테이블)
                    int keepUid = crowdControlUid;
                    TableLoaderManagerBase.Unload(ConfigAddressableTable.TableCrowdControl.Path);
                    _tableCrowdControl = TableLoaderManager.LoadCrowdControlTable(forceReload: true);
                    _tableDictionary = _tableCrowdControl != null ? _tableCrowdControl.GetDatas() : null;

                    LoadDropdown();
                    crowdControlUid = keepUid;
                    SyncSelectedIndexByUid();
                    CacheRow();

                    // 플레이 중이면 인게임에도 반영
                    UpdateInGameTableInfo(_cachedRow);

                    _editingDirty = false;
                    ShowNotification(new GUIContent("crowd_control 테이블 저장 완료"));
                }

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("CrowdControl 적용"))
                        ApplyCrowdControlToTarget();
                }
            }

            EditorGUILayout.HelpBox(
                "- '테스트 적용'은 플레이 중인 게임의 TableLoaderManager.Instance.TableCrowdControl 값을 즉시 갱신합니다.\n" +
                "- '저장(테이블 파일)'은 Assets/.../Tables/crowd_control.txt 파일을 저장하고 리임포트합니다.\n" +
                "- 'CrowdControl 적용'은 플레이 모드에서만 동작합니다.",
                MessageType.Info);
        }

        // ==============================
        // Table / Cache
        // ==============================
        private void ReloadTable(bool preserveSelection)
        {
            int keepUid = preserveSelection ? crowdControlUid : 0;

            _tableCrowdControl = TableLoaderManager.LoadCrowdControlTable(forceReload: true);
            _tableDictionary = _tableCrowdControl != null ? _tableCrowdControl.GetDatas() : null;

            LoadDropdown();

            crowdControlUid = keepUid;
            SyncSelectedIndexByUid();
        }

        private void LoadDropdown()
        {
            _names.Clear();
            _uids.Clear();

            if (_tableDictionary == null || _tableDictionary.Count == 0)
                return;

            var uids = new List<int>(_tableDictionary.Keys);
            uids.Sort();

            foreach (var uid in uids)
            {
                if (!_tableDictionary.TryGetValue(uid, out var row) || row == null)
                    continue;

                _uids.Add(uid);
                string name = string.IsNullOrWhiteSpace(row.Id)
                    ? $"{uid}"
                    : $"{uid} - {row.Id}";
                _names.Add(name);
            }

            if (_uids.Count > 0 && crowdControlUid == 0)
            {
                crowdControlUid = _uids[0];
                _selectedIndex = 0;
            }
        }

        private void SyncSelectedIndexByUid()
        {
            if (_uids.Count == 0)
            {
                _selectedIndex = 0;
                return;
            }

            int idx = _uids.IndexOf(crowdControlUid);
            _selectedIndex = idx >= 0 ? idx : 0;
            if (idx < 0)
                crowdControlUid = _uids[_selectedIndex];
        }

        private void CacheRow()
        {
            _cachedRow = null;
            _editingRow = null;
            _editingDirty = false;

            if (_tableDictionary == null) return;
            if (!_tableDictionary.TryGetValue(crowdControlUid, out var row) || row == null)
                return;

            _cachedRow = row;
            _editingRow = CloneRow(row);
        }

        private static StruckTableCrowdControl CloneRow(StruckTableCrowdControl row)
        {
            if (row == null) return null;

            return new StruckTableCrowdControl
            {
                Uid = row.Uid,
                Id = row.Id,
                Type = row.Type,
                DirectionType = row.DirectionType,
                FixedDirectionX = row.FixedDirectionX,
                FixedDirectionY = row.FixedDirectionY,
                Distance = row.Distance,
                EaseType = row.EaseType,
                Duration = row.Duration,
                IsLockControl = row.IsLockControl,
                IsUseKnockbackStatus = row.IsUseKnockbackStatus,
                IsUseDontControlStatus = row.IsUseDontControlStatus,
                StaggerAnimationType = row.StaggerAnimationType,
                IsStopOnWall = row.IsStopOnWall,
                IsGroundOnly = row.IsGroundOnly,
                IsAirOnly = row.IsAirOnly,
            };
        }

        private bool ApplyEditingToCachedRow()
        {
            if (_cachedRow == null || _editingRow == null)
                return false;

            _cachedRow.Id = _editingRow.Id;
            _cachedRow.Type = _editingRow.Type;
            _cachedRow.DirectionType = _editingRow.DirectionType;
            _cachedRow.FixedDirectionX = _editingRow.FixedDirectionX;
            _cachedRow.FixedDirectionY = _editingRow.FixedDirectionY;
            _cachedRow.Distance = _editingRow.Distance;
            _cachedRow.EaseType = _editingRow.EaseType;
            _cachedRow.Duration = _editingRow.Duration;
            _cachedRow.IsLockControl = _editingRow.IsLockControl;
            _cachedRow.IsUseKnockbackStatus = _editingRow.IsUseKnockbackStatus;
            _cachedRow.IsUseDontControlStatus = _editingRow.IsUseDontControlStatus;
            _cachedRow.StaggerAnimationType = _editingRow.StaggerAnimationType;
            _cachedRow.IsStopOnWall = _editingRow.IsStopOnWall;
            _cachedRow.IsGroundOnly = _editingRow.IsGroundOnly;
            _cachedRow.IsAirOnly = _editingRow.IsAirOnly;

            return true;
        }

        // ==============================
        // Apply / Runtime
        // ==============================
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

            var controller = _target.GetComponent<CharacterCrowdControlController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog(Title, "Target에 CharacterCrowdControlController가 없습니다.", "OK");
                return;
            }

            controller.ApplyCrowdControlByUid(crowdControlUid, _source);
        }

        private static void UpdateInGameTableInfo(StruckTableCrowdControl row)
        {
            if (row == null) return;
            if (!Application.isPlaying) return;
            if (!GGemCo2DCore.TableLoaderManager.Instance) return;

            var info = GGemCo2DCore.TableLoaderManager.Instance.TableCrowdControl.GetDataByUid(row.Uid);
            if (info == null) return;

            info.Id = row.Id;
            info.Type = row.Type;
            info.DirectionType = row.DirectionType;
            info.FixedDirectionX = row.FixedDirectionX;
            info.FixedDirectionY = row.FixedDirectionY;
            info.Distance = row.Distance;
            info.EaseType = row.EaseType;
            info.Duration = row.Duration;
            info.IsLockControl = row.IsLockControl;
            info.IsUseKnockbackStatus = row.IsUseKnockbackStatus;
            info.IsUseDontControlStatus = row.IsUseDontControlStatus;
            info.StaggerAnimationType = row.StaggerAnimationType;
            info.IsStopOnWall = row.IsStopOnWall;
            info.IsGroundOnly = row.IsGroundOnly;
            info.IsAirOnly = row.IsAirOnly;
        }

        // ==============================
        // Save
        // ==============================
        private static string FormatFloat(float v) => v.ToString(CultureInfo.InvariantCulture);
        private static int BoolToInt(bool v) => v ? 1 : 0;

        private bool TrySaveCrowdControlTableFile(out string error)
        {
            error = null;

            if (_tableCrowdControl == null)
            {
                error = "CrowdControl 테이블이 로드되지 않았습니다.";
                return false;
            }

            try
            {
                var assetPath = ConfigAddressableTable.TableCrowdControl.Path; // Assets/.../crowd_control.txt
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var fullPath = Path.Combine(projectRoot ?? string.Empty, assetPath);

                // Canonical header order (TableCrowdControl 기준)
                var header = string.Join("\t", new[]
                {
                    "Uid","Id",
                    "Type","DirectionType",
                    "FixedDirectionX","FixedDirectionY",
                    "Distance","EaseType","Duration",
                    "IsLockControl","IsUseKnockbackStatus","IsUseDontControlStatus",
                    "StaggerAnimationType",
                    "IsStopOnWall","IsGroundOnly","IsAirOnly",
                });

                var sb = new StringBuilder(1024 * 32);
                sb.AppendLine(header);

                var datas = _tableCrowdControl.GetDatas();
                var uids = new List<int>(datas.Keys);
                uids.Sort();

                foreach (var uid in uids)
                {
                    if (!datas.TryGetValue(uid, out var r) || r == null)
                        continue;

                    sb.Append(r.Uid).Append('\t');
                    sb.Append(r.Id ?? string.Empty).Append('\t');

                    sb.Append(r.Type).Append('\t');
                    sb.Append(r.DirectionType).Append('\t');

                    sb.Append(FormatFloat(r.FixedDirectionX)).Append('\t');
                    sb.Append(FormatFloat(r.FixedDirectionY)).Append('\t');

                    sb.Append(FormatFloat(r.Distance)).Append('\t');
                    sb.Append(r.EaseType).Append('\t');
                    sb.Append(FormatFloat(r.Duration)).Append('\t');

                    sb.Append(r.IsLockControl).Append('\t');
                    sb.Append(r.IsUseKnockbackStatus).Append('\t');
                    sb.Append(r.IsUseDontControlStatus).Append('\t');

                    sb.Append(r.StaggerAnimationType).Append('\t');

                    sb.Append(r.IsStopOnWall).Append('\t');
                    sb.Append(r.IsGroundOnly).Append('\t');
                    sb.Append(r.IsAirOnly);
                    sb.AppendLine();
                }

                File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                error = $"CrowdControl 테이블 저장 중 오류: {e.Message}";
                return false;
            }
        }
    }
}
