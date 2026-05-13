using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 레이저 테스트용 EditorWindow 입니다.
    /// - laser 테이블 정적 데이터를 선택합니다.
    /// - 선택 Row를 직접 수정하고, 플레이 중 즉시 반영하거나 테이블 파일에 저장할 수 있습니다.
    /// - 타겟/좌표/지속시간/사거리 오버라이드를 적용해 플레이 모드에서 즉시 검증할 수 있습니다.
    /// </summary>
    public sealed class UseLaser : DefaultEditorWindow
    {
        private const string Title = "레이저 사용툴";

        private enum TargetSelectMode
        {
            NearMonster = 0,
            MonsterUid = 1,
            ManualPosition = 2,
        }

        private static readonly TableRowEditorUtility.TableRowEditorField[] LaserRowEditorFields =
        {
            new("Uid", readOnly: true),
            new("Name"),
            new("VfxUid"),
            new("VfxScale"),
            new("StartPosition", "StartPosition (x,y)"),
            new("HitVfxUid"),
            new("Count"),
            new("SecDelayByOne"),
            new("RotateByMoveDirection"),
            new("MaxDistance"),
            new("Duration"),
            new("TickInterval"),
            new("TickOnSpawn"),
            new("BlockMode"),
            new("HitMode"),
            new("AimUpdateMode"),
            new("RaycastDirectionMode"),
            new("RaycastAngleDeg"),
            new("VfxAngleSyncMode"),
        };

        [SerializeField, Tooltip("laser 테이블에서 사용할 레이저 Uid입니다.")] private int laserUid;
        [SerializeField, Tooltip("테스트 발사 시 사용할 데미지 타입입니다.")] private ConfigCommon.DamageType damageType = ConfigCommon.DamageType.Physic;
        [SerializeField, Tooltip("레이저 적중 시 적용할 기본 데미지 값입니다.")] private long damage = 10;
        [SerializeField, Tooltip("레이저 비주얼 크기에 곱할 배율입니다.")] private float scaleMultiplier = 1f;
        [SerializeField, Tooltip("체크하면 테이블 Duration 대신 아래 오버라이드 값을 사용합니다.")] private bool useDurationOverride;
        [SerializeField, Tooltip("레이저 지속 시간을 직접 덮어쓸 값입니다.")] private float durationOverride = 0.25f;
        [SerializeField, Tooltip("체크하면 테이블 TickInterval 대신 아래 오버라이드 값을 사용합니다.")] private bool useTickIntervalOverride;
        [SerializeField, Tooltip("지속형 레이저의 틱 간격을 직접 덮어쓸 값입니다.")] private float tickIntervalOverride;
        [SerializeField, Tooltip("체크하면 테이블 MaxDistance 대신 아래 오버라이드 값을 사용합니다.")] private bool useMaxDistanceOverride;
        [SerializeField, Tooltip("레이저 최대 사거리를 직접 덮어쓸 값입니다.")] private float maxDistanceOverride = 10f;
        [SerializeField, Tooltip("활성 시간 동안 타겟 방향을 계속 추적할지 여부입니다.")] private bool updateAimContinuously;
        [SerializeField, Tooltip("체크하면 테이블 RaycastDirectionMode 대신 아래 오버라이드 값을 사용합니다.")] private bool useRaycastDirectionModeOverride;
        [SerializeField, Tooltip("레이캐스트 방향 계산 모드를 직접 덮어쓸 값입니다.")] private LaserConstants.RaycastDirectionMode raycastDirectionModeOverride = LaserConstants.RaycastDirectionMode.TowardTarget;
        [SerializeField, Tooltip("체크하면 테이블 RaycastAngleDeg 대신 아래 오버라이드 값을 사용합니다.")] private bool useRaycastAngleOverride;
        [SerializeField, Tooltip("각도 기반 Raycast 방향에 사용할 각도 오버라이드 값입니다.")] private float raycastAngleOverrideDeg;
        [SerializeField, Tooltip("체크하면 테이블 VfxAngleSyncMode 대신 아래 오버라이드 값을 사용합니다.")] private bool useVfxAngleSyncModeOverride;
        [SerializeField, Tooltip("VFX 각도 동기화 모드를 직접 덮어쓸 값입니다.")] private LaserConstants.VfxAngleSyncMode vfxAngleSyncModeOverride = LaserConstants.VfxAngleSyncMode.FollowRaycast;
        [SerializeField, Tooltip("테스트 발사 시 사용할 비주얼 출력 타입입니다.")] private ProjectileConstants.ProjectileVisualType visualType = ProjectileConstants.ProjectileVisualType.Default;
        [SerializeField, Tooltip("테이블 VFX 대신 사용할 비주얼 VFX Uid입니다. 0이면 기본값을 사용합니다.")] private int visualVfxUidOverride;
        [SerializeField, Tooltip("Sprite 비주얼 타입에서 사용할 스프라이트입니다.")] private Sprite visualSprite;
        [SerializeField, Tooltip("Animator 비주얼 타입에서 사용할 컨트롤러입니다.")] private RuntimeAnimatorController visualAnimatorController;
        [SerializeField, Tooltip("레이저의 목표 지점을 어떤 방식으로 선택할지 결정합니다.")] private TargetSelectMode targetMode = TargetSelectMode.NearMonster;
        [SerializeField, Tooltip("가장 가까운 몬스터 검색 방식에서 사용할 최대 탐색 거리입니다.")] private float nearMonsterSearchDistance = 1000f;
        [SerializeField, Tooltip("특정 몬스터 Uid를 직접 지정해 타겟으로 사용할 때의 값입니다.")] private int targetMonsterUid;
        [SerializeField, Tooltip("수동 좌표 방식에서 사용할 월드 기준 목표 위치입니다.")] private Vector3 manualTargetPosition = Vector3.right * 3f;
        [SerializeField, Tooltip("SceneView에 예상 레이저 경로와 목표점을 표시할지 여부입니다.")] private bool showSceneGizmos = true;
        [SerializeField, Tooltip("한 번의 실행에서 연속으로 발사할 레이저 수입니다.")] private int toolBurstCount = 1;
        [SerializeField, Tooltip("연속 발사 시 각 발사 사이에 둘 지연 시간입니다.")] private float toolBurstDelaySeconds;
        [SerializeField, Tooltip("선택된 laser Row의 테이블 편집 UI를 접고 펼칩니다.")] private bool _foldLaserRowEdit = true;

        private Vector2 _scroll;
        private int _selectedIndexLaser;
        private readonly List<string> _namesLaser = new();
        private readonly List<int> _uidsLaser = new();
        private TableLaser _tableLaser;
        private StruckTableLaser _cachedLaserInfo;
        private StruckTableLaser _editingLaser;
        private bool _editingLaserDirty;
        private LaserController _laserController;

        [MenuItem(ConfigEditor.NameToolUseLaser, false, (int)ConfigEditor.ToolOrdering.UseLaser)]
        public static void ShowWindow()
        {
            GetWindow<UseLaser>(Title);
        }

        /// <summary>
        /// 윈도우 활성화 시 테이블과 SceneView 훅을 초기화합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            _tableLaser = TableLoaderManager.LoadLaserTable();
            LoadLaserDropdown();
            SyncLaserSelectionByUid();
            CacheLaserInfo();

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        /// <summary>
        /// 윈도우 비활성화 시 SceneView 훅을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        /// <summary>
        /// 툴 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("레이저 테스트(테이블 + 런타임 오버라이드)", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4f);

                    DrawStaticSection();
                    DrawLaserRowEditor();
                    DrawTargetSection();
                    DrawRuntimeSection();
                    DrawVisualSection();
                    DrawSpawnSection();

                    EditorGUILayout.Space(8f);
                    using (new EditorGUI.DisabledScope(!Application.isPlaying))
                    {
                        if (GUILayout.Button("레이저 발사"))
                            CreateAndLaunch();
                    }
                }

                EditorGUILayout.HelpBox(
                    "플레이 모드에서만 발사가 가능합니다.\n" +
                    "SceneView에서는 시작점, 목표점, 예상 사거리를 선으로 확인할 수 있습니다.",
                    MessageType.Info);
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 정적 laser 테이블 선택 UI를 그립니다.
        /// </summary>
        private void DrawStaticSection()
        {
            EditorGUILayout.LabelField("정적 정의(테이블)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Laser 선택", GUILayout.Width(110));
                    if (_namesLaser.Count > 0)
                    {
                        int newIndex = EditorGUILayout.Popup(_selectedIndexLaser, _namesLaser.ToArray());
                        if (newIndex != _selectedIndexLaser)
                        {
                            _selectedIndexLaser = newIndex;
                            laserUid = _uidsLaser[_selectedIndexLaser];
                            CacheLaserInfo();
                            Repaint();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("laser 테이블 로드 실패/비어있음");
                    }

                    if (GUILayout.Button("리로드", GUILayout.Width(60)))
                    {
                        _tableLaser = TableLoaderManager.LoadLaserTable(forceReload: true);
                        LoadLaserDropdown();
                        SyncLaserSelectionByUid();
                        CacheLaserInfo();
                    }
                }

                int newUid = EditorGUILayout.IntField(new GUIContent("LaserUid (Table)"), laserUid);
                if (newUid != laserUid)
                {
                    laserUid = newUid;
                    SyncLaserSelectionByUid();
                    CacheLaserInfo();
                }

                if (_cachedLaserInfo != null)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField($"Name: {_cachedLaserInfo.Name}");
                    EditorGUILayout.LabelField($"MaxDistance: {_cachedLaserInfo.MaxDistance}");
                    EditorGUILayout.LabelField($"Duration: {_cachedLaserInfo.Duration}");
                    EditorGUILayout.LabelField($"TickInterval: {_cachedLaserInfo.TickInterval}");
                    EditorGUILayout.LabelField($"HitMode: {_cachedLaserInfo.HitMode}");
                    EditorGUILayout.LabelField($"BlockMode: {_cachedLaserInfo.BlockMode}");
                    EditorGUILayout.LabelField($"AimUpdateMode: {_cachedLaserInfo.AimUpdateMode}");
                    EditorGUILayout.LabelField($"RaycastDirectionMode: {_cachedLaserInfo.RaycastDirectionMode}");
                    EditorGUILayout.LabelField($"RaycastAngleDeg: {_cachedLaserInfo.RaycastAngleDeg}");
                    EditorGUILayout.LabelField($"VfxAngleSyncMode: {_cachedLaserInfo.VfxAngleSyncMode}");
                }
            }
        }

        /// <summary>
        /// 선택된 laser Row 편집 UI를 그립니다.
        /// - 편집값은 복제본에만 반영하여 원본 캐시 오염을 방지합니다.
        /// - 테스트 적용은 플레이 중 런타임 테이블 캐시만 갱신합니다.
        /// - 저장은 laser.txt 행만 패치합니다.
        /// </summary>
        private void DrawLaserRowEditor()
        {
            if (_cachedLaserInfo == null || _editingLaser == null)
            {
                EditorGUILayout.HelpBox("선택된 데이터가 없습니다.", MessageType.Info);
                return;
            }

            _foldLaserRowEdit = EditorGUILayout.Foldout(_foldLaserRowEdit, "테이블 편집(선택 Row)", true);
            if (!_foldLaserRowEdit)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                TableRowEditorUtility.DrawResult drawResult = TableRowEditorUtility.DrawObjectEditor(_editingLaser, LaserRowEditorFields, NormalizeEditingLaserFieldValue);
                if (drawResult.Changed)
                    _editingLaserDirty = true;

                EditorGUILayout.Space(6f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!_editingLaserDirty))
                    {
                        if (GUILayout.Button("되돌리기"))
                        {
                            _editingLaser = CloneLaserRow(_cachedLaserInfo);
                            _editingLaserDirty = false;
                            GUI.FocusControl(null);
                        }
                    }

                    using (new EditorGUI.DisabledScope(!_editingLaserDirty || !Application.isPlaying))
                    {
                        if (GUILayout.Button("테스트 적용"))
                        {
                            UpdateInGameLaserTableInfo(_editingLaser);
                            _cachedLaserInfo = CloneLaserRow(_editingLaser);
                            _editingLaser = CloneLaserRow(_editingLaser);
                            _editingLaserDirty = false;
                            ShowNotification(new GUIContent("테스트 적용 완료"));
                            RepaintSceneViews();
                        }
                    }

                    if (GUILayout.Button("저장(테이블 파일)"))
                    {
                        if (!ApplyEditingToCachedRow())
                            return;

                        if (!TrySaveLaserTableFile(out string error))
                        {
                            EditorUtility.DisplayDialog(Title, error, "OK");
                            return;
                        }

                        int keepUid = laserUid;
                        _tableLaser = TableLoaderManager.LoadLaserTable(forceReload: true);

                        if (Application.isPlaying)
                            UpdateInGameLaserTableInfo(_cachedLaserInfo);

                        LoadLaserDropdown();
                        laserUid = keepUid;
                        SyncLaserSelectionByUid();
                        CacheLaserInfo();

                        _editingLaserDirty = false;
                        ShowNotification(new GUIContent("테이블 저장 완료"));
                        RepaintSceneViews();
                    }
                }
            }
        }

        /// <summary>
        /// 레이저 Row 편집 중 필드 값을 표준 범위로 정규화합니다.
        /// </summary>
        /// <param name="target">정규화할 편집 대상 Row입니다.</param>
        /// <param name="memberName">방금 변경된 멤버 이름입니다.</param>
        private static void NormalizeEditingLaserFieldValue(object target, string memberName)
        {
            if (target is not StruckTableLaser row)
                return;

            switch (memberName)
            {
                case nameof(StruckTableLaser.VfxScale):
                    row.VfxScale = Mathf.Max(0.01f, row.VfxScale);
                    break;
                case nameof(StruckTableLaser.Count):
                    row.Count = Mathf.Max(1, row.Count);
                    break;
                case nameof(StruckTableLaser.SecDelayByOne):
                    row.SecDelayByOne = Mathf.Max(0f, row.SecDelayByOne);
                    break;
                case nameof(StruckTableLaser.MaxDistance):
                    row.MaxDistance = Mathf.Max(0.01f, row.MaxDistance);
                    break;
                case nameof(StruckTableLaser.Duration):
                    row.Duration = Mathf.Max(0f, row.Duration);
                    break;
                case nameof(StruckTableLaser.TickInterval):
                    row.TickInterval = Mathf.Max(0f, row.TickInterval);
                    break;
            }
        }

        /// <summary>
        /// 편집 중인 복제 Row를 현재 캐시 Row에 반영합니다.
        /// </summary>
        /// <returns>반영할 Row가 있으면 true를 반환합니다.</returns>
        private bool ApplyEditingToCachedRow()
        {
            if (_cachedLaserInfo == null || _editingLaser == null)
                return false;

            TableRowEditorUtility.CopyMembers(_editingLaser, _cachedLaserInfo, LaserRowEditorFields);
            return true;
        }

        /// <summary>
        /// 편집 중인 값이 원본 캐시를 오염시키지 않도록 laser Row를 복제합니다.
        /// </summary>
        /// <param name="row">복제할 laser Row입니다.</param>
        /// <returns>복제된 laser Row입니다.</returns>
        private static StruckTableLaser CloneLaserRow(StruckTableLaser row)
        {
            return TableRowEditorUtility.CloneShallow<StruckTableLaser>(row);
        }

        /// <summary>
        /// 플레이 모드 테스트에 즉시 반영되도록 런타임 laser 테이블 캐시를 갱신합니다.
        /// </summary>
        /// <param name="row">갱신할 laser Row입니다.</param>
        private static void UpdateInGameLaserTableInfo(StruckTableLaser row)
        {
            if (!Application.isPlaying || !GGemCo2DCore.TableLoaderManager.Instance || row == null)
                return;

            GGemCo2DCore.TableLoaderManager.Instance.TableLaser?.Upsert(CloneLaserRow(row));
        }

        /// <summary>
        /// 실제 테이블 파일의 헤더 구성에 맞춰 laser Row를 탭 구분 문자열로 직렬화합니다.
        /// </summary>
        /// <param name="row">저장할 laser Row입니다.</param>
        /// <param name="headers">파일 헤더 목록입니다.</param>
        /// <returns>테이블 파일에 기록할 한 줄 문자열입니다.</returns>
        private static string SerializeLaserRow(StruckTableLaser row, IReadOnlyList<string> headers)
        {
            var values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "VfxUid" => row.VfxUid.ToString(),
                    "VfxScale" => MathHelper.FormatFloat(row.VfxScale),
                    "StartPosition" => MathHelper.FormatVector2(row.StartPosition),
                    "HitVfxUid" => row.HitVfxUid.ToString(),
                    "Count" => row.Count.ToString(),
                    "SecDelayByOne" => MathHelper.FormatFloat(row.SecDelayByOne),
                    "RotateByMoveDirection" => row.RotateByMoveDirection ? "Y" : "N",
                    "MaxDistance" => MathHelper.FormatFloat(row.MaxDistance),
                    "Duration" => MathHelper.FormatFloat(row.Duration),
                    "TickInterval" => MathHelper.FormatFloat(row.TickInterval),
                    "TickOnSpawn" => row.TickOnSpawn ? "Y" : "N",
                    "BlockMode" => row.BlockMode.ToString(),
                    "HitMode" => row.HitMode.ToString(),
                    "AimUpdateMode" => row.AimUpdateMode.ToString(),
                    "RaycastDirectionMode" => row.RaycastDirectionMode.ToString(),
                    "RaycastAngleDeg" => MathHelper.FormatFloat(row.RaycastAngleDeg),
                    "VfxAngleSyncMode" => row.VfxAngleSyncMode.ToString(),
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }

        /// <summary>
        /// 현재 편집 중인 laser Row를 laser.txt 파일에 저장합니다.
        /// </summary>
        /// <param name="error">저장 실패 시 오류 메시지입니다.</param>
        /// <returns>저장에 성공하면 true를 반환합니다.</returns>
        private bool TrySaveLaserTableFile(out string error)
        {
            error = null;

            if (_cachedLaserInfo == null)
            {
                error = "저장할 Row가 없습니다.";
                return false;
            }

            if (_tableLaser == null)
            {
                error = "테이블이 로드되지 않았습니다.";
                return false;
            }

            if (!TableTextRowPatchUtility.TryPatchRowByUid(
                    ConfigAddressableTable.TableLaser.Path,
                    _cachedLaserInfo.Uid,
                    _cachedLaserInfo,
                    SerializeLaserRow,
                    out error))
            {
                error = $"테이블 저장 중 오류: {error}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 타겟 선택 UI를 그립니다.
        /// </summary>
        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("타겟", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                targetMode = (TargetSelectMode)EditorGUILayout.EnumPopup(new GUIContent("TargetMode"), targetMode);
                nearMonsterSearchDistance = Mathf.Max(1f, EditorGUILayout.FloatField(new GUIContent("NearMonsterDistance"), nearMonsterSearchDistance));
                targetMonsterUid = EditorGUILayout.IntField(new GUIContent("TargetMonsterUid"), targetMonsterUid);
                manualTargetPosition = EditorGUILayout.Vector3Field(new GUIContent("ManualTargetPosition"), manualTargetPosition);
                showSceneGizmos = EditorGUILayout.Toggle(new GUIContent("Show Scene Gizmos"), showSceneGizmos);
            }
        }

        /// <summary>
        /// 런타임 오버라이드 UI를 그립니다.
        /// </summary>
        private void DrawRuntimeSection()
        {
            EditorGUILayout.LabelField("런타임 오버라이드", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                damageType = (ConfigCommon.DamageType)EditorGUILayout.EnumPopup(new GUIContent("DamageType"), damageType);
                damage = EditorGUILayout.LongField(new GUIContent("Damage"), damage);
                scaleMultiplier = Mathf.Max(0.01f, EditorGUILayout.FloatField(new GUIContent("ScaleMultiplier"), scaleMultiplier));
                updateAimContinuously = EditorGUILayout.Toggle(new GUIContent("UpdateAimContinuously"), updateAimContinuously);

                useDurationOverride = EditorGUILayout.Toggle(new GUIContent("UseDurationOverride"), useDurationOverride);
                if (useDurationOverride)
                    durationOverride = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("DurationOverride"), durationOverride));

                useTickIntervalOverride = EditorGUILayout.Toggle(new GUIContent("UseTickIntervalOverride"), useTickIntervalOverride);
                if (useTickIntervalOverride)
                    tickIntervalOverride = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("TickIntervalOverride"), tickIntervalOverride));

                useMaxDistanceOverride = EditorGUILayout.Toggle(new GUIContent("UseMaxDistanceOverride"), useMaxDistanceOverride);
                if (useMaxDistanceOverride)
                    maxDistanceOverride = Mathf.Max(0.01f, EditorGUILayout.FloatField(new GUIContent("MaxDistanceOverride"), maxDistanceOverride));

                useRaycastDirectionModeOverride = EditorGUILayout.Toggle(new GUIContent("UseRaycastDirectionModeOverride"), useRaycastDirectionModeOverride);
                if (useRaycastDirectionModeOverride)
                    raycastDirectionModeOverride = (LaserConstants.RaycastDirectionMode)EditorGUILayout.EnumPopup(new GUIContent("RaycastDirectionModeOverride"), raycastDirectionModeOverride);

                useRaycastAngleOverride = EditorGUILayout.Toggle(new GUIContent("UseRaycastAngleOverride"), useRaycastAngleOverride);
                if (useRaycastAngleOverride)
                    raycastAngleOverrideDeg = EditorGUILayout.FloatField(new GUIContent("RaycastAngleOverrideDeg"), raycastAngleOverrideDeg);

                useVfxAngleSyncModeOverride = EditorGUILayout.Toggle(new GUIContent("UseVfxAngleSyncModeOverride"), useVfxAngleSyncModeOverride);
                if (useVfxAngleSyncModeOverride)
                    vfxAngleSyncModeOverride = (LaserConstants.VfxAngleSyncMode)EditorGUILayout.EnumPopup(new GUIContent("VfxAngleSyncModeOverride"), vfxAngleSyncModeOverride);
            }
        }

        /// <summary>
        /// 비주얼 오버라이드 UI를 그립니다.
        /// </summary>
        private void DrawVisualSection()
        {
            EditorGUILayout.LabelField("비주얼 오버라이드", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                visualType = (ProjectileConstants.ProjectileVisualType)EditorGUILayout.EnumPopup(new GUIContent("VisualType"), visualType);
                visualVfxUidOverride = EditorGUILayout.IntField(new GUIContent("VisualVfxUidOverride"), visualVfxUidOverride);
                visualSprite = (Sprite)EditorGUILayout.ObjectField(new GUIContent("VisualSprite"), visualSprite, typeof(Sprite), false);
                visualAnimatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(new GUIContent("AnimatorController"), visualAnimatorController, typeof(RuntimeAnimatorController), false);
            }
        }

        /// <summary>
        /// 연속 발사 제어 UI를 그립니다.
        /// </summary>
        private void DrawSpawnSection()
        {
            EditorGUILayout.LabelField("발사 제어", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                toolBurstCount = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Count"), toolBurstCount));
                toolBurstDelaySeconds = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("Delay(sec)"), toolBurstDelaySeconds));
            }
        }

        /// <summary>
        /// 실제 레이저 발사를 수행합니다.
        /// </summary>
        private void CreateAndLaunch()
        {
            if (!Application.isPlaying || !SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "플레이 모드에서 실행해주세요.", "OK");
                return;
            }

            if (laserUid <= 0 || _cachedLaserInfo == null)
            {
                EditorUtility.DisplayDialog(Title, "유효한 LaserUid를 선택해주세요.", "OK");
                return;
            }

            var player = SceneGame.Instance.player;
            if (!player || !player.TryGetComponent(out CharacterBase owner))
            {
                EditorUtility.DisplayDialog(Title, "플레이어(CharacterBase)를 찾을 수 없습니다.", "OK");
                return;
            }

            _laserController ??= new LaserController();
            _laserController.Initialize(owner);

            ResolveTarget(out CharacterBase targetCharacter, out bool useTargetPositionOverride, out Vector2 targetPositionOverride);

            SceneGame.Instance.StartCoroutine(FireBurst(owner, targetCharacter, useTargetPositionOverride, targetPositionOverride));
        }

        /// <summary>
        /// 툴 상태에 따라 발사 타겟을 결정합니다.
        /// </summary>
        private void ResolveTarget(out CharacterBase targetCharacter, out bool useTargetPositionOverride, out Vector2 targetPositionOverride)
        {
            targetCharacter = null;
            useTargetPositionOverride = false;
            targetPositionOverride = manualTargetPosition;

            switch (targetMode)
            {
                case TargetSelectMode.NearMonster:
                    SceneGame sceneGame = SceneGame.Instance;
                    targetCharacter = sceneGame != null && sceneGame.mapManager != null
                        ? sceneGame.mapManager.GetNearByMonsterDistance((int)nearMonsterSearchDistance)
                        : null;
                    if (targetCharacter != null)
                    {
                        useTargetPositionOverride = false;
                        targetPositionOverride = targetCharacter.transform.position;
                    }
                    else
                    {
                        useTargetPositionOverride = true;
                    }
                    break;

                case TargetSelectMode.MonsterUid:
                    targetCharacter = FindCharacterByUid(targetMonsterUid);
                    if (targetCharacter != null)
                    {
                        useTargetPositionOverride = false;
                        targetPositionOverride = targetCharacter.transform.position;
                    }
                    else
                    {
                        useTargetPositionOverride = true;
                    }
                    break;

                default:
                    useTargetPositionOverride = true;
                    break;
            }
        }

        /// <summary>
        /// 설정된 횟수만큼 레이저를 순차 발사합니다.
        /// </summary>
        private IEnumerator FireBurst(CharacterBase owner, CharacterBase target, bool useTargetPositionOverride, Vector2 targetPositionOverride)
        {
            for (int i = 0; i < toolBurstCount; i++)
            {
                MetadataLaser meta = new MetadataLaser(
                    uid: laserUid,
                    damageType: damageType,
                    damage: damage,
                    target: target,
                    owner: owner,
                    scaleMultiplier: scaleMultiplier,
                    visualType: visualType,
                    visualSprite: visualSprite,
                    visualAnimatorController: visualAnimatorController,
                    visualVfxUidOverride: visualVfxUidOverride,
                    useTargetPositionOverride: useTargetPositionOverride,
                    targetPositionOverride: targetPositionOverride,
                    useDurationOverride: useDurationOverride,
                    durationOverride: durationOverride,
                    useTickIntervalOverride: useTickIntervalOverride,
                    tickIntervalOverride: tickIntervalOverride,
                    useMaxDistanceOverride: useMaxDistanceOverride,
                    maxDistanceOverride: maxDistanceOverride,
                    updateAimContinuously: updateAimContinuously,
                    useRaycastDirectionModeOverride: useRaycastDirectionModeOverride,
                    raycastDirectionModeOverride: raycastDirectionModeOverride,
                    useRaycastAngleOverride: useRaycastAngleOverride,
                    raycastAngleOverrideDeg: raycastAngleOverrideDeg,
                    useVfxAngleSyncModeOverride: useVfxAngleSyncModeOverride,
                    vfxAngleSyncModeOverride: vfxAngleSyncModeOverride);

                _laserController.Launch(meta);

                if (toolBurstDelaySeconds > 0f)
                    yield return new WaitForSeconds(toolBurstDelaySeconds);
            }
        }

        /// <summary>
        /// 현재 씬에서 UID에 해당하는 캐릭터를 찾습니다.
        /// </summary>
        private static CharacterBase FindCharacterByUid(int uid)
        {
            CharacterBase[] characters = CompatObjectFind.FindAll<CharacterBase>();
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterBase character = characters[i];
                if (character != null && character.uid == uid)
                    return character;
            }

            return null;
        }

        /// <summary>
        /// SceneView에 레이저 프리뷰 선을 그립니다.
        /// </summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!showSceneGizmos || _cachedLaserInfo == null)
                return;

            SceneGame sceneGame = SceneGame.Instance;
            GameObject player = sceneGame != null ? sceneGame.player : null;
            CharacterBase owner = player != null && player.TryGetComponent(out CharacterBase character) ? character : null;
            Vector3 start = player != null ? player.transform.position : Vector3.zero;
            start += (Vector3)_cachedLaserInfo.StartPosition;

            ResolveTarget(out CharacterBase targetCharacter, out bool useTargetPositionOverride, out Vector2 targetPositionOverride);
            Vector3 direction = ResolvePreviewRaycastDirection(owner, start, targetCharacter, useTargetPositionOverride, targetPositionOverride);
            if (direction.sqrMagnitude <= 1e-6f)
                direction = ResolvePreviewOwnerFacingDirection(owner);

            float distance = useMaxDistanceOverride ? maxDistanceOverride : _cachedLaserInfo.MaxDistance;
            Vector3 end = start + direction.normalized * distance;

            Handles.color = Color.cyan;
            Handles.DrawAAPolyLine(4f, start, end);
            Handles.SphereHandleCap(0, start, Quaternion.identity, 0.15f, EventType.Repaint);
            Handles.SphereHandleCap(0, end, Quaternion.identity, 0.12f, EventType.Repaint);
        }

        /// <summary>
        /// 현재 UseLaser 설정을 기준으로 SceneView 프리뷰용 Raycast 방향을 계산합니다.
        /// </summary>
        /// <param name="owner">레이저를 발사할 시전자입니다.</param>
        /// <param name="start">프리뷰 시작점입니다.</param>
        /// <param name="targetCharacter">선택된 타겟 캐릭터입니다.</param>
        /// <param name="useTargetPositionOverride">좌표 타겟 오버라이드 사용 여부입니다.</param>
        /// <param name="targetPositionOverride">좌표 타겟 오버라이드 값입니다.</param>
        /// <returns>프리뷰에 사용할 정규화 Raycast 방향입니다.</returns>
        private Vector3 ResolvePreviewRaycastDirection(
            CharacterBase owner,
            Vector3 start,
            CharacterBase targetCharacter,
            bool useTargetPositionOverride,
            Vector2 targetPositionOverride)
        {
            if (ResolveEffectiveRaycastDirectionMode() == LaserConstants.RaycastDirectionMode.ByAngle)
                return ResolvePreviewDirectionByAngle(owner);

            Vector3 targetPosition = targetCharacter != null
                ? targetCharacter.transform.position
                : (Vector3)targetPositionOverride;

            if (!useTargetPositionOverride && targetCharacter == null)
                return ResolvePreviewOwnerFacingDirection(owner);

            Vector3 direction = targetPosition - start;
            if (direction.sqrMagnitude <= 1e-6f)
                return ResolvePreviewOwnerFacingDirection(owner);

            return direction.normalized;
        }

        /// <summary>
        /// 현재 UseLaser 설정의 각도 값을 기준으로 SceneView 프리뷰 방향을 계산합니다.
        /// </summary>
        /// <param name="owner">레이저를 발사할 시전자입니다.</param>
        /// <returns>각도 기반 정규화 방향입니다.</returns>
        private Vector3 ResolvePreviewDirectionByAngle(CharacterBase owner)
        {
            float angleDeg = ResolveEffectiveRaycastAngleDeg();
            float baseAngle = 0f;

            if (owner != null && owner.IsFlipped())
            {
                baseAngle = 180f;
                angleDeg = -angleDeg;
            }

            float worldAngle = (baseAngle + angleDeg) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(worldAngle), Mathf.Sin(worldAngle), 0f);
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : ResolvePreviewOwnerFacingDirection(owner);
        }

        /// <summary>
        /// 현재 UseLaser 설정에서 적용할 RaycastDirectionMode를 해석합니다.
        /// </summary>
        /// <returns>오버라이드 또는 테이블 기준 RaycastDirectionMode입니다.</returns>
        private LaserConstants.RaycastDirectionMode ResolveEffectiveRaycastDirectionMode()
        {
            if (useRaycastDirectionModeOverride)
                return raycastDirectionModeOverride;

            return _cachedLaserInfo != null
                ? _cachedLaserInfo.RaycastDirectionMode
                : LaserConstants.RaycastDirectionMode.TowardTarget;
        }

        /// <summary>
        /// 현재 UseLaser 설정에서 적용할 Raycast 각도 값을 해석합니다.
        /// </summary>
        /// <returns>오버라이드 또는 테이블 기준 Raycast 각도(도)입니다.</returns>
        private float ResolveEffectiveRaycastAngleDeg()
        {
            if (useRaycastAngleOverride)
                return raycastAngleOverrideDeg;

            return _cachedLaserInfo != null ? _cachedLaserInfo.RaycastAngleDeg : 0f;
        }

        /// <summary>
        /// 프리뷰 방향 계산에 사용할 시전자 기본 바라보기 방향을 반환합니다.
        /// </summary>
        /// <param name="owner">레이저를 발사할 시전자입니다.</param>
        /// <returns>시전자 기준 기본 방향입니다.</returns>
        private static Vector3 ResolvePreviewOwnerFacingDirection(CharacterBase owner)
        {
            return owner != null && owner.IsFlipped() ? Vector3.left : Vector3.right;
        }

        /// <summary>
        /// 드롭다운 목록을 갱신합니다.
        /// </summary>
        private void LoadLaserDropdown()
        {
            _namesLaser.Clear();
            _uidsLaser.Clear();

            if (_tableLaser == null)
                return;

            foreach (KeyValuePair<int, StruckTableLaser> pair in _tableLaser.GetDatas())
            {
                StruckTableLaser row = pair.Value;
                if (row == null)
                    continue;

                _uidsLaser.Add(pair.Key);
                _namesLaser.Add($"{pair.Key} - {row.Name}");
            }

            if (_uidsLaser.Count == 0)
            {
                _selectedIndexLaser = 0;
                return;
            }

            if (laserUid <= 0)
                laserUid = _uidsLaser[0];
        }

        /// <summary>
        /// 현재 UID와 일치하는 드롭다운 인덱스를 동기화합니다.
        /// </summary>
        private void SyncLaserSelectionByUid()
        {
            _selectedIndexLaser = 0;
            for (int i = 0; i < _uidsLaser.Count; i++)
            {
                if (_uidsLaser[i] != laserUid)
                    continue;

                _selectedIndexLaser = i;
                return;
            }
        }

        /// <summary>
        /// 현재 선택된 레이저 정적 데이터를 캐시하고, 편집용 복제본을 새로 만듭니다.
        /// </summary>
        private void CacheLaserInfo()
        {
            _cachedLaserInfo = (_tableLaser != null && laserUid > 0)
                ? _tableLaser.GetDataByUid(laserUid)
                : null;

            _editingLaser = _cachedLaserInfo != null ? CloneLaserRow(_cachedLaserInfo) : null;
            _editingLaserDirty = false;
        }

        /// <summary>
        /// SceneView를 다시 그려 최신 프리뷰가 반영되도록 요청합니다.
        /// </summary>
        private static void RepaintSceneViews()
        {
            SceneView.RepaintAll();
        }
    }
}
