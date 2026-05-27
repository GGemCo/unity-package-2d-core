using System.Collections.Generic;
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
    /// - crowd_control 공통 테이블만 편집합니다. 타입별 상세 값은 별도 상세 테이블에서 관리합니다.
    /// </summary>
    public sealed class UseCrowdControl : DefaultEditorWindow
    {
        private const string Title = "CrowdControl 사용툴";

        /// <summary>
        /// Target/Source 자동 연동 프리셋입니다.
        /// </summary>
        private enum AutoBindPreset
        {
            /// <summary>
            /// Target은 플레이어, Source는 몬스터로 설정합니다.
            /// </summary>
            PlayerTargetMonsterSource = 0,

            /// <summary>
            /// Target은 몬스터, Source는 플레이어로 설정합니다.
            /// </summary>
            MonsterTargetPlayerSource = 1
        }

        [MenuItem(ConfigEditor.NameToolUseCrowdControl, false, (int)ConfigEditor.ToolOrdering.UseCrowdControl)]
        public static void ShowWindow() => GetWindow<UseCrowdControl>(Title);

        public static void OpenAndSelect(int uid)
        {
            UseCrowdControlSelectionBridge.PendingCrowdControlUid = uid;
            GetWindow<UseCrowdControl>(Title).Show();
        }

        // ------------------------------
        // Target
        // ------------------------------
        [Header("대상")]
        [SerializeField] private GameObject _target;
        [SerializeField] private GameObject _source;
        [Tooltip("CrowdControl 적용 시 동일 애니메이션을 첫 프레임부터 강제로 다시 재생합니다.")]
        [SerializeField] private bool _forceRefreshAnimationOnApply = true;

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
        private bool _foldRowEdit = true;
        private StruckTableCrowdControl _cachedRow;
        private StruckTableCrowdControl _editingRow;
        private bool _editingDirty;

        private Vector2 _scroll;
        private static readonly TableRowEditorUtility.TableRowEditorField[] RowEditorFields =
            TableRowEditorUtility.BuildFields<StruckTableCrowdControl>(BuildRowEditorOptions());

        private static TableRowEditorUtility.TableRowEditorBuildOptions BuildRowEditorOptions()
        {
            var options = new TableRowEditorUtility.TableRowEditorBuildOptions();
            options.ReadOnlyMembers.Add(nameof(StruckTableCrowdControl.Uid));
            options.GroupByMemberName[nameof(StruckTableCrowdControl.StaggerAnimationName)] = null;
            return options;
        }

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

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("플레이어, 몬스터"))
                            TryAutoBindTargetSource(AutoBindPreset.PlayerTargetMonsterSource);

                        if (GUILayout.Button("몬스터, 플레이어"))
                            TryAutoBindTargetSource(AutoBindPreset.MonsterTargetPlayerSource);
                    }
                }

                _forceRefreshAnimationOnApply = EditorGUILayout.ToggleLeft(
                    "CrowdControl 적용 시 애니메이션 강제 새로고침",
                    _forceRefreshAnimationOnApply);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("자동 연동 버튼은 플레이 모드에서만 동작합니다.", MessageType.Info);
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
                EditorGUILayout.HelpBox("선택된 데이터가 없습니다.", MessageType.Info);
                return;
            }

            _foldRowEdit = EditorGUILayout.Foldout(_foldRowEdit, "테이블 편집(선택 Row)", true);
            if (!_foldRowEdit) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                var drawResult = TableRowEditorUtility.DrawObjectEditor(_editingRow, RowEditorFields, NormalizeEditingFieldValue);
                if (drawResult.Changed)
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
                    ShowNotification(new GUIContent("테이블 저장 완료"));
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
                string name = string.IsNullOrWhiteSpace(row.Name)
                    ? $"{uid}"
                    : $"{uid} - {row.Name}";
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
            return TableRowEditorUtility.CloneShallow<StruckTableCrowdControl>(row);
        }

        private bool ApplyEditingToCachedRow()
        {
            if (_cachedRow == null || _editingRow == null)
                return false;

            TableRowEditorUtility.CopyMembers(_editingRow, _cachedRow, RowEditorFields);
            NormalizeEditingRow();

            return true;
        }

        private void NormalizeEditingFieldValue(object target, string memberName)
        {
            if (!ReferenceEquals(target, _editingRow) || string.IsNullOrWhiteSpace(memberName))
                return;

            switch (memberName)
            {
                case nameof(StruckTableCrowdControl.Distance):
                    if (_editingRow.Distance < 0f) _editingRow.Distance = 0f;
                    break;

                case nameof(StruckTableCrowdControl.Duration):
                    if (_editingRow.Duration < 0f) _editingRow.Duration = 0f;
                    break;
            }
        }

        private void NormalizeEditingRow()
        {
            NormalizeEditingFieldValue(_editingRow, nameof(StruckTableCrowdControl.Distance));
            NormalizeEditingFieldValue(_editingRow, nameof(StruckTableCrowdControl.Duration));
        }

        // ==============================
        // Apply / Runtime
        // ==============================
        /// <summary>
        /// 지정한 프리셋 기준으로 Target/Source를 자동 연동합니다.
        /// </summary>
        /// <param name="preset">자동 연동 프리셋입니다.</param>
        private void TryAutoBindTargetSource(AutoBindPreset preset)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(Title, "자동 연동은 플레이 모드에서만 사용할 수 있습니다.", "OK");
                return;
            }

            if (!TryResolveAutoBindPair(preset, out var targetCharacter, out var sourceCharacter, out var error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            _target = targetCharacter.gameObject;
            _source = sourceCharacter != null ? sourceCharacter.gameObject : null;

            ShowNotification(new GUIContent($"자동 연동 완료: Target={_target.name}, Source={_source?.name ?? "(없음)"}"));
        }

        /// <summary>
        /// 프리셋에 필요한 플레이어/몬스터 쌍을 해석합니다.
        /// </summary>
        /// <param name="preset">자동 연동 프리셋입니다.</param>
        /// <param name="targetCharacter">해석된 Target 캐릭터입니다.</param>
        /// <param name="sourceCharacter">해석된 Source 캐릭터입니다.</param>
        /// <param name="error">실패 시 오류 메시지입니다.</param>
        /// <returns>해석 성공 시 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryResolveAutoBindPair(
            AutoBindPreset preset,
            out CharacterBase targetCharacter,
            out CharacterBase sourceCharacter,
            out string error)
        {
            targetCharacter = null;
            sourceCharacter = null;
            error = null;

            CharacterBase player = TryFindPlayerCharacter();
            if (player == null)
            {
                error = "플레이어(CharacterBase)를 찾지 못했습니다.";
                return false;
            }

            CharacterBase monster = TryFindMonsterCharacter();
            if (monster == null)
            {
                error = "몬스터(CharacterBase)를 찾지 못했습니다.";
                return false;
            }

            // 프리셋에 따라 Target/Source 역할만 교체합니다.
            if (preset == AutoBindPreset.PlayerTargetMonsterSource)
            {
                targetCharacter = player;
                sourceCharacter = monster;
            }
            else
            {
                targetCharacter = monster;
                sourceCharacter = player;
            }

            if (targetCharacter == null)
            {
                error = "자동 연동 결과 Target이 비어있습니다.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 현재 씬에서 플레이어 캐릭터를 찾습니다.
        /// </summary>
        /// <returns>찾은 플레이어 캐릭터, 없으면 <see langword="null"/>입니다.</returns>
        private static CharacterBase TryFindPlayerCharacter()
        {
            if (!Application.isPlaying)
                return null;

            // 1차: SceneGame의 player 참조를 우선 사용합니다.
            var sceneGame = SceneGame.Instance;
            if (sceneGame != null && sceneGame.player != null)
            {
                var playerFromScene = sceneGame.player.GetComponent<CharacterBase>();
                if (playerFromScene != null)
                    return playerFromScene;
            }

            // 2차: 씬의 CharacterBase 목록에서 Player 타입을 검색합니다.
            CharacterBase[] characters = CompatObjectFind.FindAll<CharacterBase>();
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterBase character = characters[i];
                if (character == null)
                    continue;

                if (character is Player || character.IsPlayer())
                    return character;
            }

            return null;
        }

        /// <summary>
        /// 현재 씬에서 몬스터 캐릭터를 찾습니다.
        /// </summary>
        /// <returns>찾은 몬스터 캐릭터, 없으면 <see langword="null"/>입니다.</returns>
        private static CharacterBase TryFindMonsterCharacter()
        {
            if (!Application.isPlaying)
                return null;

            var sceneGame = SceneGame.Instance;

            // 1차: 현재 플레이어 기준 근접 몬스터를 우선 사용합니다.
            if (sceneGame != null && sceneGame.mapManager != null)
            {
                CharacterBase nearMonster = sceneGame.mapManager.GetNearByMonsterDistance(10000);
                if (nearMonster != null)
                    return nearMonster;
            }

            // 2차: 씬의 CharacterBase 목록에서 Monster 타입을 검색합니다.
            CharacterBase[] characters = CompatObjectFind.FindAll<CharacterBase>();
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterBase character = characters[i];
                if (character == null)
                    continue;

                if (character is Monster || character.IsMonster())
                    return character;
            }

            return null;
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

            var controller = _target.GetComponent<CharacterCrowdControlController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog(Title, "Target에 CharacterCrowdControlController가 없습니다.", "OK");
                return;
            }

            controller.ApplyCrowdControlByUid(
                crowdControlUid,
                _source,
                isEndCharacterStop: false,
                forceRefreshAnimation: _forceRefreshAnimationOnApply);
        }

        private static void UpdateInGameTableInfo(StruckTableCrowdControl row)
        {
            if (row == null) return;
            if (!Application.isPlaying) return;
            if (!GGemCo2DCore.TableLoaderManager.Instance) return;

            var info = GGemCo2DCore.TableLoaderManager.Instance.TableCrowdControl.GetDataByUid(row.Uid);
            if (info == null) return;

            TableRowEditorUtility.CopyMembers(row, info);
        }

        // ==============================
        // Save
        // ==============================

        private static string SerializeCrowdControlRow(StruckTableCrowdControl row, IReadOnlyList<string> headers)
        {
            var values = new string[headers.Count];

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
                    "IsUseKnockbackStatus" => MathHelper.FormatBool(row.IsUseKnockbackStatus),
                    "IsUseDontControlStatus" => MathHelper.FormatBool(row.IsUseDontControlStatus),
                    "StaggerAnimationName" => row.StaggerAnimationName ?? string.Empty,
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }

        private bool TrySaveCrowdControlTableFile(out string error)
        {
            error = null;

            if (_cachedRow == null)
            {
                error = "저장할 Row가 없습니다.";
                return false;
            }

            if (_tableCrowdControl == null)
            {
                error = "테이블이 로드되지 않았습니다.";
                return false;
            }

            if (!TableTextRowPatchUtility.TryPatchRowByUid(
                    ConfigAddressableTable.TableCrowdControl.Path,
                    _cachedRow.Uid,
                    _cachedRow,
                    SerializeCrowdControlRow,
                    out error))
            {
                error = $"테이블 저장 중 오류: {error}";
                return false;
            }

            return true;
        }
    }
}
