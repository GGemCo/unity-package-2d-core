using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GGemCo2DCore;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 프로젝타일 테스트 EditorWindow.
    /// - 테이블(정적): ProjectileUid로 정의 참조
    /// - 런타임(동적): DamageType/Damage/Speed/Scale/Visual 등을 MetadataProjectile로 오버라이드
    /// - 타겟 선택 모드(근처/UID/마우스) + Position/Area 직관 테스트(마우스 위치/좌표 고정/기즈모)
    /// </summary>
    public class UseProjectile : DefaultEditorWindow
    {
        private const string Title = "프로젝타일 사용툴";
        private const string PrefsKey = "GGemCo_UseProjectile_";

        [MenuItem(ConfigEditor.NameToolUseProjectile, false, (int)ConfigEditor.ToolOrdering.UseProjectile)]
        public static void ShowWindow() => GetWindow<UseProjectile>(Title);

        // ------------------------------
        // Static (Table)
        // ------------------------------
        [Header("정적 정의(테이블)")]
        [Tooltip("Projectile 테이블 Uid(필수)")]
        [SerializeField] private int projectileUid = 0;

        // ------------------------------
        // Runtime (Dynamic)
        // ------------------------------
        [Header("전투(런타임)")]
        [SerializeField] private ConfigCommon.DamageType damageType = ConfigCommon.DamageType.Physic;

        [Tooltip("테스트용 데미지")]
        [SerializeField] private long damage = 10;

        [Header("이동/스케일(런타임)")]
        [Tooltip("테이블 MoveSpeed에 곱해지는 배율")]
        [SerializeField] private float speedMultiplier = 1f;

        [Tooltip("표현/콜라이더 등 전체 스케일 배율")]
        [SerializeField] private float scaleMultiplier = 1f;

        // ------------------------------
        // Target Select (Tool)
        // ------------------------------
        private enum TargetSelectMode
        {
            NearMonster = 0,
            MonsterUid = 1,
            ManualPosition = 2,
        }

        [Header("타겟 선택(툴)")]
        [SerializeField] private TargetSelectMode targetMode = TargetSelectMode.NearMonster;

        [Tooltip("근처 몬스터 탐색 거리")]
        [SerializeField] private float nearMonsterSearchDistance = 1000f;

        [Tooltip("특정 몬스터 UID로 타겟 지정")]
        [SerializeField] private int targetMonsterUid = 0;

        [Tooltip("Area 타입 테스트 시 사용할 반경(툴 오버라이드, 지원 시 적용)")]
        [SerializeField] private float areaRadiusOverride = 2f;

        [Header("타겟 UX")]
        [Tooltip("Projectile.TargetType에 따라 타겟 모드를 자동 추천(스냅)합니다.")]
        [SerializeField] private bool autoSnapTargetMode = true;

        [Header("좌표(수동 입력)")]
        [Tooltip("Position/Area 타입 테스트 시 목표 지점(월드 좌표)을 수동으로 입력합니다.")]
        [SerializeField] private Vector3 manualTargetPosition = Vector3.zero;

        [Tooltip("SceneView 기즈모 표시(Position/Area)")]
        [SerializeField] private bool showSceneGizmos = true;

        // ------------------------------
        // Visual (Runtime)
        // ------------------------------
        [Header("표현 방식(런타임)")]
        [SerializeField] private ProjectileConstants.ProjectileVisualType visualType = ProjectileConstants.ProjectileVisualType.Default;

        [Tooltip("Sprite 타입일 때 사용")]
        [SerializeField] private Sprite visualSprite;

        [Tooltip("Animator 타입일 때 사용")]
        [SerializeField] private RuntimeAnimatorController visualAnimatorController;

        [Tooltip("Effect 타입일 때: 0이면 테이블 EffectUid 사용, 0보다 크면 이 값을 우선")]
        [SerializeField] private int visualEffectUidOverride;

        // ------------------------------
        // Spawn (Tool control)
        // ------------------------------
        [Header("발사 갯수/딜레이(툴 제어)")]
        [SerializeField] private int count = 1;
        [SerializeField] private float secDelayByOne;

        // ------------------------------
        // UI / State
        // ------------------------------
        private bool _foldRuntime = true;
        private bool _foldVisual = true;
        private bool _foldSpawn = true;
        private bool _foldTarget = true;

        private Vector2 _scroll;

        // Projectile dropdown
        private int _selectedIndexProjectile;
        private readonly List<string> _namesProjectile = new();
        private readonly List<int> _uidsProjectile = new();

        // Effect dropdown
        private int _selectedIndexEffect;
        private readonly List<string> _namesEffect = new();
        private readonly List<int> _uidsEffect = new();

        private Dictionary<int, StruckTableEffect> _tableDictionaryEffect;

        private TableProjectile _tableProjectile;
        private TableEffect _tableEffect;
        private ProjectileController _projectileController;

        private StruckTableProjectile _cachedProjectileInfo;

        // 테이블 편집(선택된 Projectile Row)
        [SerializeField] private bool _foldProjectileRowEdit = true;
        private StruckTableProjectile _editingProjectile;
        private bool _editingProjectileDirty;


        protected override void OnEnable()
        {
            base.OnEnable();

            _selectedIndexProjectile = 0;
            _selectedIndexEffect = 0;

            _tableProjectile = TableLoaderManager.LoadProjectileTable();
            _tableEffect = TableLoaderManager.LoadEffectTable();
            _tableDictionaryEffect = _tableEffect != null ? _tableEffect.GetDatas() : null;

            _projectileController ??= new ProjectileController();

            LoadProjectileDropdown();
            LoadEffectDropdown();
            LoadPrefs();

            SyncProjectileSelectionByUid();
            SyncEffectSelectionByUid();

            CacheProjectileInfo();
            ApplyAutoSnapTargetModeIfNeeded(force: true);

            // SceneView 기즈모 표시(클릭 캡처는 사용하지 않습니다)
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        protected void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("프로젝타일 테스트(테이블 + 런타임 오버라이드)", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4);

                    DrawStaticSection();
                    EditorGUILayout.Space(4);

                    DrawTargetSection();
                    EditorGUILayout.Space(4);

                    DrawRuntimeSection();
                    EditorGUILayout.Space(4);

                    DrawVisualSection();
                    EditorGUILayout.Space(4);

                    DrawSpawnSection();
                    EditorGUILayout.Space(8);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("기본값"))
                        {
                            ResetDefaults();
                            SavePrefsSilently();
                        }

                        if (GUILayout.Button("저장"))
                            SavePrefs();

                        using (new EditorGUI.DisabledScope(!Application.isPlaying))
                        {
                            if (GUILayout.Button("프로젝타일 발사"))
                                CreateAndLaunch();
                        }
                    }
                }

                EditorGUILayout.HelpBox(
                    "플레이 모드에서만 발사가 가능합니다.\n" +
                    "Position/Area 타입 테스트는 '마우스 포지션' 또는 '좌표 고정'을 사용하면 직관적입니다.\n" +
                    "SceneView 기즈모는 Position/Area 범위를 시각적으로 확인할 수 있습니다.",
                    MessageType.Info);
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        // ==============================
        // Sections
        // ==============================
        private void DrawStaticSection()
        {
            EditorGUILayout.LabelField("정적 정의(테이블)", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Projectile 선택", GUILayout.Width(110));

                    if (_namesProjectile.Count > 0)
                    {
                        int newIndex = EditorGUILayout.Popup(_selectedIndexProjectile, _namesProjectile.ToArray());
                        if (newIndex != _selectedIndexProjectile)
                        {
                            _selectedIndexProjectile = newIndex;

                            projectileUid = (_selectedIndexProjectile >= 0 && _selectedIndexProjectile < _uidsProjectile.Count)
                                ? _uidsProjectile[_selectedIndexProjectile]
                                : projectileUid;

                            CacheProjectileInfo();
                            ApplyAutoSnapTargetModeIfNeeded(force: false);
                            RepaintSceneViews();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Projectile 테이블 로드 실패/비어있음");
                    }

                    if (GUILayout.Button("리로드", GUILayout.Width(60)))
                    {
                        _tableProjectile = TableLoaderManager.LoadProjectileTable(forceReload: true);
                        LoadProjectileDropdown();
                        SyncProjectileSelectionByUid();
                        CacheProjectileInfo();
                        ApplyAutoSnapTargetModeIfNeeded(force: true);
                        RepaintSceneViews();
                    }
                }

                int newUid = EditorGUILayout.IntField(new GUIContent("ProjectileUid (Table)"), projectileUid);
                if (newUid != projectileUid)
                {
                    projectileUid = newUid;
                    SyncProjectileSelectionByUid();
                    CacheProjectileInfo();
                    ApplyAutoSnapTargetModeIfNeeded(force: false);
                    RepaintSceneViews();
                }
            }

            DrawProjectileRowEditor();
        }

        private void DrawTargetSection()
        {
            _foldTarget = EditorGUILayout.Foldout(_foldTarget, "타겟 선택/좌표/기즈모", true);
            if (!_foldTarget) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                autoSnapTargetMode = EditorGUILayout.ToggleLeft("TargetType 기반 자동 추천(스냅)", autoSnapTargetMode);

                targetMode = (TargetSelectMode)EditorGUILayout.EnumPopup(new GUIContent("TargetMode"), targetMode);

                switch (targetMode)
                {
                    case TargetSelectMode.NearMonster:
                        nearMonsterSearchDistance = EditorGUILayout.FloatField(new GUIContent("NearSearchDistance"), nearMonsterSearchDistance);
                        nearMonsterSearchDistance = Mathf.Max(1f, nearMonsterSearchDistance);
                        break;

                    case TargetSelectMode.MonsterUid:
                        targetMonsterUid = EditorGUILayout.IntField(new GUIContent("MonsterUid"), targetMonsterUid);
                        targetMonsterUid = Mathf.Max(0, targetMonsterUid);
                        break;

                    case TargetSelectMode.ManualPosition:
                        manualTargetPosition = EditorGUILayout.Vector3Field(new GUIContent("TargetPosition (World)"), manualTargetPosition);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledScope(!Application.isPlaying))
                            {
                                if (GUILayout.Button("플레이어 위치 복사"))
                                {
                                    var owner = TryGetOwnerPlayer();
                                    if (owner != null)
                                        manualTargetPosition = owner.transform.position;
                                }
                            }

                            if (GUILayout.Button("0으로 초기화"))
                            {
                                manualTargetPosition = Vector3.zero;
                            }
                        }
                        break;
                }

                EditorGUILayout.Space(4);

                showSceneGizmos = EditorGUILayout.ToggleLeft("SceneView 기즈모 표시(Position/Area)", showSceneGizmos);

                areaRadiusOverride = EditorGUILayout.FloatField(new GUIContent("AreaRadiusOverride"), areaRadiusOverride);
                areaRadiusOverride = Mathf.Max(0.01f, areaRadiusOverride);

                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Position/Area 타입 테스트는 TargetMode=ManualPosition을 권장합니다.\n" +
                    "마우스 클릭 기반 좌표 고정 기능은 제거되었습니다.",
                    MessageType.None);
            }
        }

        private void DrawRuntimeSection()
        {
            _foldRuntime = EditorGUILayout.Foldout(_foldRuntime, "런타임(전투/이동) 오버라이드", true);
            if (!_foldRuntime) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                damageType = (ConfigCommon.DamageType)EditorGUILayout.EnumPopup(new GUIContent("DamageType"), damageType);
                damage = EditorGUILayout.LongField(new GUIContent("Damage"), damage);

                speedMultiplier = EditorGUILayout.FloatField(new GUIContent("SpeedMultiplier"), speedMultiplier);
                scaleMultiplier = EditorGUILayout.FloatField(new GUIContent("ScaleMultiplier"), scaleMultiplier);

                speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
                scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
                if (damage < 0) damage = 0;
            }
        }

        private void DrawVisualSection()
        {
            _foldVisual = EditorGUILayout.Foldout(_foldVisual, "표현(Visual) 오버라이드", true);
            if (!_foldVisual) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                visualType = (ProjectileConstants.ProjectileVisualType)EditorGUILayout.EnumPopup(new GUIContent("VisualType"), visualType);

                using (new EditorGUI.DisabledScope(visualType != ProjectileConstants.ProjectileVisualType.Effect))
                {
                    EditorGUILayout.LabelField("Effect (선택)", EditorStyles.miniBoldLabel);

                    _selectedIndexEffect = EditorGUILayout.Popup(
                        new GUIContent("EffectUid Override"),
                        _selectedIndexEffect,
                        _namesEffect.ToArray());

                    visualEffectUidOverride = (_selectedIndexEffect >= 0 && _selectedIndexEffect < _uidsEffect.Count)
                        ? _uidsEffect[_selectedIndexEffect]
                        : 0;

                    EditorGUILayout.HelpBox(
                        "Effect 타입일 때만 사용됩니다.\n" +
                        "0이면 테이블 EffectUid를 사용하고, 0보다 크면 Override 값을 우선합니다.",
                        MessageType.None);
                }

                using (new EditorGUI.DisabledScope(visualType != ProjectileConstants.ProjectileVisualType.Sprite))
                {
                    visualSprite = (Sprite)EditorGUILayout.ObjectField(
                        new GUIContent("Sprite"),
                        visualSprite,
                        typeof(Sprite),
                        false);
                }

                using (new EditorGUI.DisabledScope(visualType != ProjectileConstants.ProjectileVisualType.Animator))
                {
                    visualAnimatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                        new GUIContent("AnimatorController"),
                        visualAnimatorController,
                        typeof(RuntimeAnimatorController),
                        false);
                }
            }
        }

        private void DrawSpawnSection()
        {
            _foldSpawn = EditorGUILayout.Foldout(_foldSpawn, "발사(툴 제어)", true);
            if (!_foldSpawn) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                count = EditorGUILayout.IntField(new GUIContent("Count"), count);
                secDelayByOne = EditorGUILayout.FloatField(new GUIContent("Delay(sec)"), secDelayByOne);

                count = Mathf.Max(1, count);
                secDelayByOne = Mathf.Max(0f, secDelayByOne);
            }
        }

        // ==============================
        // Launch
        // ==============================
        private void CreateAndLaunch()
        {
            if (!Application.isPlaying || !SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "플레이 모드에서 실행해주세요.", "OK");
                return;
            }

            if (projectileUid <= 0)
            {
                EditorUtility.DisplayDialog(Title, "ProjectileUid(테이블 Uid)를 입력/선택해주세요.", "OK");
                return;
            }

            var projectileInfo = _cachedProjectileInfo;
            if (projectileInfo == null)
            {
                EditorUtility.DisplayDialog(Title, $"Projectile 테이블에서 uid={projectileUid} 데이터를 찾을 수 없습니다.", "OK");
                return;
            }

            var player = SceneGame.Instance.player;
            if (!player)
            {
                EditorUtility.DisplayDialog(Title, "플레이어가 없습니다.", "OK");
                return;
            }

            var owner = player.GetComponent<CharacterBase>();
            if (!owner)
            {
                EditorUtility.DisplayDialog(Title, "플레이어(CharacterBase)를 찾을 수 없습니다.", "OK");
                return;
            }

            _projectileController.Initialize(owner);

            ResolveTarget(projectileInfo, out var targetCharacter, out var hasTargetPosition, out var targetPosition);

            if (projectileInfo.TargetType == ProjectileConstants.TargetType.Fixed && !targetCharacter)
            {
                EditorUtility.DisplayDialog(Title,
                    "TargetType=Fixed 인데 타겟 몬스터를 찾을 수 없습니다.\n" +
                    "타겟 선택을 '근처 몬스터' 또는 '특정 UID 몬스터'로 지정해주세요.",
                    "OK");
                return;
            }

            if ((projectileInfo.TargetType == ProjectileConstants.TargetType.Position ||
                 projectileInfo.TargetType == ProjectileConstants.TargetType.Area) &&
                !hasTargetPosition)
            {
                hasTargetPosition = true;
                targetPosition = owner.transform.position;
            }

            SceneGame.Instance.StartCoroutine(FireBurst(owner, targetCharacter, hasTargetPosition, targetPosition,
                projectileInfo.TargetType == ProjectileConstants.TargetType.Area));
        }

        private void ResolveTarget(
            StruckTableProjectile projectileInfo,
            out CharacterBase targetCharacter,
            out bool hasTargetPosition,
            out Vector3 targetPosition)
        {
            targetCharacter = null;
            hasTargetPosition = false;
            targetPosition = default;

            if (projectileInfo == null)
                return;

            // Fixed: 캐릭터 타겟이 필요
            if (projectileInfo.TargetType == ProjectileConstants.TargetType.Fixed)
            {
                var resolvedMode = targetMode == TargetSelectMode.ManualPosition
                    ? TargetSelectMode.NearMonster
                    : targetMode;

                switch (resolvedMode)
                {
                    case TargetSelectMode.NearMonster:
                        targetCharacter = SceneGame.Instance.mapManager != null
                            ? SceneGame.Instance.mapManager.GetNearByMonsterDistance((int)nearMonsterSearchDistance)
                            : null;
                        break;

                    case TargetSelectMode.MonsterUid:
                        targetCharacter = FindCharacterByUid(targetMonsterUid);
                        break;
                }

                return;
            }

            // Position/Area: 좌표 우선(수동 입력)
            hasTargetPosition = true;
            targetPosition = manualTargetPosition;

            // 필요 시 몬스터/UID를 선택해서 해당 위치를 사용(편의)
            switch (targetMode)
            {
                case TargetSelectMode.NearMonster:
                    targetCharacter = SceneGame.Instance.mapManager != null
                        ? SceneGame.Instance.mapManager.GetNearByMonsterDistance((int)nearMonsterSearchDistance)
                        : null;
                    if (targetCharacter != null)
                        targetPosition = targetCharacter.transform.position;
                    break;

                case TargetSelectMode.MonsterUid:
                    targetCharacter = FindCharacterByUid(targetMonsterUid);
                    if (targetCharacter != null)
                        targetPosition = targetCharacter.transform.position;
                    break;

                case TargetSelectMode.ManualPosition:
                default:
                    break;
            }
        }

        private IEnumerator FireBurst(CharacterBase owner, CharacterBase target, bool hasTargetPosition, Vector3 targetPosition, bool isArea)
        {
            for (int i = 0; i < count; i++)
            {
                var meta = new MetadataProjectile(
                    uid: projectileUid,
                    damageType: damageType,
                    damage: damage,
                    target: target,
                    owner: owner,
                    speedMultiplier: speedMultiplier,
                    scaleMultiplier: scaleMultiplier,
                    visualType: visualType,
                    visualSprite: visualSprite,
                    visualAnimatorController: visualAnimatorController,
                    visualEffectUidOverride: visualEffectUidOverride);

                if (hasTargetPosition)
                {
                    meta = ApplyTargetPosition(meta, targetPosition);
                    if (isArea)
                        meta = ApplyAreaRadius(meta, areaRadiusOverride);
                }

                _projectileController.Launch(meta);

                if (secDelayByOne > 0f)
                    yield return new WaitForSeconds(secDelayByOne);
            }
        }

        // ==============================
        // GameView/SceneView Click Capture + Gizmos
        // ==============================
        private void OnSceneGUI(SceneView sceneView)
        {
            // SceneView 기즈모 표시(마우스 클릭 캡처는 사용하지 않습니다)
            if (sceneView == null)
                return;

            if (!showSceneGizmos)
                return;

            var info = _cachedProjectileInfo;
            if (info == null)
                return;

            // Position/Area일 때만 기즈모 표시
            if (info.TargetType != ProjectileConstants.TargetType.Position &&
                info.TargetType != ProjectileConstants.TargetType.Area)
                return;

            if (!TryGetPreviewTargetPosition(sceneView, out var previewPos))
                return;

            DrawTargetGizmos(previewPos, info.TargetType == ProjectileConstants.TargetType.Area ? areaRadiusOverride : 0f);
        }

        private bool TryGetPreviewTargetPosition(SceneView sceneView, out Vector3 pos)
        {
            pos = manualTargetPosition;

            if (!Application.isPlaying || !SceneGame.Instance)
                return true;

            // 편의: 타겟을 몬스터 기준으로 잡는 경우 해당 위치를 미리보기로 사용
            if (targetMode == TargetSelectMode.NearMonster)
            {
                var t = SceneGame.Instance.mapManager != null
                    ? SceneGame.Instance.mapManager.GetNearByMonsterDistance((int)nearMonsterSearchDistance)
                    : null;

                if (t != null)
                    pos = t.transform.position;
            }
            else if (targetMode == TargetSelectMode.MonsterUid)
            {
                var t = FindCharacterByUid(targetMonsterUid);
                if (t != null)
                    pos = t.transform.position;
            }

            return true;
        }

        private static void DrawTargetGizmos(Vector3 center, float radius)
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            // 십자(목표점)
            float size = HandleUtility.GetHandleSize(center) * 0.2f;
            Handles.DrawLine(center + Vector3.left * size, center + Vector3.right * size);
            Handles.DrawLine(center + Vector3.up * size, center + Vector3.down * size);

            // Area(원)
            if (radius > 0f)
            {
                Handles.DrawWireDisc(center, Vector3.forward, radius);
                Handles.Label(center + Vector3.up * (radius + size * 0.5f), $"Area r={radius:0.##}");
            }
            else
            {
                Handles.Label(center + Vector3.up * (size * 1.2f), "Position");
            }
        }
        private static void RepaintSceneViews()
        {
            SceneView.RepaintAll();
        }

        // ==============================
        // Mouse to World (Play mode)
        // ==============================
        /// <summary>
        /// 플레이 모드에서 현재 마우스의 월드 좌표를 얻습니다(Camera.main 기준).
        /// </summary>
        // ==============================
        // Metadata injection (reflection-friendly)
        // ==============================
        private static MetadataProjectile ApplyTargetPosition(MetadataProjectile meta, Vector3 pos)
        {
            object boxed = meta;
            TrySetMemberValue(boxed, "TargetPosition", pos);
            TrySetMemberValue(boxed, "targetPosition", pos);
            TrySetMemberValue(boxed, "Position", pos);
            TrySetMemberValue(boxed, "position", pos);
            TrySetMemberValue(boxed, "AimPosition", pos);
            TrySetMemberValue(boxed, "aimPosition", pos);
            TrySetMemberValue(boxed, "SpawnPosition", pos);
            TrySetMemberValue(boxed, "spawnPosition", pos);
            TrySetMemberValue(boxed, "Center", pos);
            TrySetMemberValue(boxed, "center", pos);
            return (MetadataProjectile)boxed;
        }

        private static MetadataProjectile ApplyAreaRadius(MetadataProjectile meta, float radius)
        {
            object boxed = meta;
            TrySetMemberValue(boxed, "AreaRadius", radius);
            TrySetMemberValue(boxed, "areaRadius", radius);
            TrySetMemberValue(boxed, "Radius", radius);
            TrySetMemberValue(boxed, "radius", radius);
            TrySetMemberValue(boxed, "TargetRadius", radius);
            TrySetMemberValue(boxed, "targetRadius", radius);
            return (MetadataProjectile)boxed;
        }

        private static void TrySetMemberValue(object target, string memberName, object value)
        {
            if (target == null) return;

            var t = target.GetType();

            var prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite && IsAssignable(prop.PropertyType, value))
            {
                try { prop.SetValue(target, ConvertIfNeeded(value, prop.PropertyType)); } catch { }
                return;
            }

            var field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && IsAssignable(field.FieldType, value))
            {
                try { field.SetValue(target, ConvertIfNeeded(value, field.FieldType)); } catch { }
            }
        }

        private static bool IsAssignable(Type targetType, object value)
        {
            if (value == null) return !targetType.IsValueType;

            var vt = value.GetType();
            if (targetType.IsAssignableFrom(vt)) return true;

            if (targetType == typeof(float) && (vt == typeof(int) || vt == typeof(long) || vt == typeof(double))) return true;
            if (targetType == typeof(double) && (vt == typeof(int) || vt == typeof(long) || vt == typeof(float))) return true;
            if (targetType == typeof(int) && (vt == typeof(long) || vt == typeof(float) || vt == typeof(double))) return true;
            if (targetType == typeof(long) && (vt == typeof(int) || vt == typeof(float) || vt == typeof(double))) return true;

            return false;
        }

        private static object ConvertIfNeeded(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;

            try
            {
                if (targetType == typeof(float)) return Convert.ToSingle(value, CultureInfo.InvariantCulture);
                if (targetType == typeof(double)) return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (targetType == typeof(int)) return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (targetType == typeof(long)) return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            catch { }

            return value;
        }

        // ==============================
        // Character lookup
        // ==============================
        private static CharacterBase FindCharacterByUid(int uid)
        {
            if (uid <= 0) return null;

            var characters = UnityEngine.Object.FindObjectsByType<CharacterBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in characters)
            {
                if (!c) continue;

                if (TryReadIntMember(c, "Uid", out var v) && v == uid) return c;
                if (TryReadIntMember(c, "uid", out v) && v == uid) return c;
                if (TryReadIntMember(c, "MonsterUid", out v) && v == uid) return c;
                if (TryReadIntMember(c, "monsterUid", out v) && v == uid) return c;
                if (TryReadIntMember(c, "TableUid", out v) && v == uid) return c;
                if (TryReadIntMember(c, "tableUid", out v) && v == uid) return c;

                if (!string.IsNullOrEmpty(c.name) && c.name.Contains(uid.ToString(CultureInfo.InvariantCulture)))
                    return c;
            }

            return null;
        }

        private static bool TryReadIntMember(object obj, string memberName, out int value)
        {
            value = 0;
            if (obj == null) return false;

            var t = obj.GetType();

            var prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanRead)
            {
                try
                {
                    var o = prop.GetValue(obj);
                    if (o is int i) { value = i; return true; }
                    if (o is long l) { value = (int)l; return true; }
                }
                catch { }
            }

            var field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                try
                {
                    var o = field.GetValue(obj);
                    if (o is int i) { value = i; return true; }
                    if (o is long l) { value = (int)l; return true; }
                }
                catch { }
            }

            return false;
        }

        // ==============================
        // Dropdowns
        // ==============================
        private void LoadProjectileDropdown()
        {
            _namesProjectile.Clear();
            _uidsProjectile.Clear();

            _namesProjectile.Add("Select...");
            _uidsProjectile.Add(0);

            if (_tableProjectile == null)
                return;

            foreach (var data in _tableProjectile.GetDatas())
            {
                var info = data.Value;
                if (info == null || info.Uid <= 0) continue;
                _namesProjectile.Add($"{info.Uid} - {info.Name}");
                _uidsProjectile.Add(info.Uid);
            }

            _selectedIndexProjectile = 0;
        }

        private void LoadEffectDropdown()
        {
            _namesEffect.Clear();
            _uidsEffect.Clear();

            _namesEffect.Add("None");
            _uidsEffect.Add(0);

            if (_tableDictionaryEffect == null)
            {
                _selectedIndexEffect = 0;
                return;
            }

            foreach (var kvp in _tableDictionaryEffect)
            {
                var info = kvp.Value;
                if (info == null || info.Uid <= 0) continue;

                _namesEffect.Add($"{info.Uid} - {info.Name}");
                _uidsEffect.Add(info.Uid);
            }

            _selectedIndexEffect = 0;
        }

        private void SyncProjectileSelectionByUid()
        {
            _selectedIndexProjectile = GetProjectileIndex(projectileUid);
        }

        private int GetProjectileIndex(int searchUid)
        {
            for (int i = 0; i < _uidsProjectile.Count; i++)
            {
                if (searchUid == _uidsProjectile[i]) return i;
            }
            return 0;
        }

        private void SyncEffectSelectionByUid()
        {
            _selectedIndexEffect = GetEffectIndex(visualEffectUidOverride);
        }

        private int GetEffectIndex(int searchUid)
        {
            for (int i = 0; i < _uidsEffect.Count; i++)
            {
                if (searchUid == _uidsEffect[i]) return i;
            }
            return 0;
        }

        // ==============================
        // TargetType auto-snap
        // ==============================
        private void CacheProjectileInfo()
        {
            _cachedProjectileInfo = (_tableProjectile != null && projectileUid > 0)
                ? _tableProjectile.GetDataByUid(projectileUid)
                : null;

            _editingProjectile = _cachedProjectileInfo != null ? CloneProjectileRow(_cachedProjectileInfo) : null;
            _editingProjectileDirty = false;
        }

        private void ApplyAutoSnapTargetModeIfNeeded(bool force)
        {
            if (!autoSnapTargetMode)
                return;

            if (_cachedProjectileInfo == null)
                return;

            var tt = _cachedProjectileInfo.TargetType;

            // force=false이면 "강제 변경"이 아니라 추천 수준으로만 동작하도록:
            // - 현재 모드가 이미 합리적이면 변경하지 않음
            // - Fixed인데 MousePosition이면 NearMonster로
            // - Position/Area인데 Near/Uid이면 MousePosition으로
            if (tt == ProjectileConstants.TargetType.Fixed)
            {
                if (force || targetMode == TargetSelectMode.ManualPosition)
                    targetMode = TargetSelectMode.NearMonster;
            }
            else if (tt == ProjectileConstants.TargetType.Position || tt == ProjectileConstants.TargetType.Area)
            {
                if (force || targetMode == TargetSelectMode.NearMonster || targetMode == TargetSelectMode.MonsterUid)
                    targetMode = TargetSelectMode.ManualPosition;
            }
        }

        // ==============================
        // Prefs
        // ==============================
        private void SavePrefs()
        {
            bool result = EditorUtility.DisplayDialog("저장하기", "현재 저장된 값을 덮어씌웁니다.\n저장하시겠습니까?", "네", "아니요");
            if (!result) return;

            SavePrefsSilently();
            ShowNotification(new GUIContent("저장되었습니다."));
        }

        private void SavePrefsSilently()
        {
            EditorPrefs.SetInt(PrefsKey + "projectileUid", projectileUid);
            EditorPrefs.SetInt(PrefsKey + "projectileSelectedIndex", _selectedIndexProjectile);

            EditorPrefs.SetInt(PrefsKey + "damageType", (int)damageType);
            EditorPrefs.SetString(PrefsKey + "damage", damage.ToString(CultureInfo.InvariantCulture));

            EditorPrefs.SetString(PrefsKey + "speedMultiplier", speedMultiplier.ToString(CultureInfo.InvariantCulture));
            EditorPrefs.SetString(PrefsKey + "scaleMultiplier", scaleMultiplier.ToString(CultureInfo.InvariantCulture));

            EditorPrefs.SetInt(PrefsKey + "visualType", (int)visualType);
            EditorPrefs.SetInt(PrefsKey + "visualEffectUidOverride", visualEffectUidOverride);
            EditorPrefs.SetInt(PrefsKey + "effectSelectedIndex", _selectedIndexEffect);

            EditorPrefs.SetInt(PrefsKey + "count", count);
            EditorPrefs.SetString(PrefsKey + "secDelayByOne", secDelayByOne.ToString(CultureInfo.InvariantCulture));

            EditorPrefs.SetInt(PrefsKey + "targetMode", (int)targetMode);
            EditorPrefs.SetString(PrefsKey + "nearMonsterSearchDistance", nearMonsterSearchDistance.ToString(CultureInfo.InvariantCulture));
            EditorPrefs.SetInt(PrefsKey + "targetMonsterUid", targetMonsterUid);
            EditorPrefs.SetString(PrefsKey + "areaRadiusOverride", areaRadiusOverride.ToString(CultureInfo.InvariantCulture));

            EditorPrefs.SetString(PrefsKey + "manualTargetX", manualTargetPosition.x.ToString(CultureInfo.InvariantCulture));
            EditorPrefs.SetString(PrefsKey + "manualTargetY", manualTargetPosition.y.ToString(CultureInfo.InvariantCulture));
            EditorPrefs.SetString(PrefsKey + "manualTargetZ", manualTargetPosition.z.ToString(CultureInfo.InvariantCulture));

            EditorPrefs.SetInt(PrefsKey + "autoSnapTargetMode", autoSnapTargetMode ? 1 : 0);
            EditorPrefs.SetInt(PrefsKey + "showSceneGizmos", showSceneGizmos ? 1 : 0);
        }

        private void LoadPrefs()
        {
            projectileUid = EditorPrefs.GetInt(PrefsKey + "projectileUid", projectileUid);
            _selectedIndexProjectile = EditorPrefs.GetInt(PrefsKey + "projectileSelectedIndex", _selectedIndexProjectile);

            damageType = (ConfigCommon.DamageType)EditorPrefs.GetInt(PrefsKey + "damageType", (int)damageType);

            if (long.TryParse(EditorPrefs.GetString(PrefsKey + "damage", damage.ToString(CultureInfo.InvariantCulture)), out var d))
                damage = d;

            if (float.TryParse(EditorPrefs.GetString(PrefsKey + "speedMultiplier", speedMultiplier.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var sm))
                speedMultiplier = sm;

            if (float.TryParse(EditorPrefs.GetString(PrefsKey + "scaleMultiplier", scaleMultiplier.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var scm))
                scaleMultiplier = scm;

            visualType = (ProjectileConstants.ProjectileVisualType)EditorPrefs.GetInt(PrefsKey + "visualType", (int)visualType);
            visualEffectUidOverride = EditorPrefs.GetInt(PrefsKey + "visualEffectUidOverride", visualEffectUidOverride);
            _selectedIndexEffect = EditorPrefs.GetInt(PrefsKey + "effectSelectedIndex", _selectedIndexEffect);

            count = EditorPrefs.GetInt(PrefsKey + "count", count);

            if (float.TryParse(EditorPrefs.GetString(PrefsKey + "secDelayByOne", secDelayByOne.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var sdb))
                secDelayByOne = sdb;

            targetMode = (TargetSelectMode)EditorPrefs.GetInt(PrefsKey + "targetMode", (int)targetMode);

            if (float.TryParse(EditorPrefs.GetString(PrefsKey + "nearMonsterSearchDistance", nearMonsterSearchDistance.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var nd))
                nearMonsterSearchDistance = nd;

            targetMonsterUid = EditorPrefs.GetInt(PrefsKey + "targetMonsterUid", targetMonsterUid);

            if (float.TryParse(EditorPrefs.GetString(PrefsKey + "areaRadiusOverride", areaRadiusOverride.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var ar))
                areaRadiusOverride = ar;

            if (float.TryParse(EditorPrefs.GetString(PrefsKey + "manualTargetX", manualTargetPosition.x.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var mx) &&
                float.TryParse(EditorPrefs.GetString(PrefsKey + "manualTargetY", manualTargetPosition.y.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var my) &&
                float.TryParse(EditorPrefs.GetString(PrefsKey + "manualTargetZ", manualTargetPosition.z.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var mz))
            {
                manualTargetPosition = new Vector3(mx, my, mz);
            }

            autoSnapTargetMode = EditorPrefs.GetInt(PrefsKey + "autoSnapTargetMode", autoSnapTargetMode ? 1 : 0) == 1;
            showSceneGizmos = EditorPrefs.GetInt(PrefsKey + "showSceneGizmos", showSceneGizmos ? 1 : 0) == 1;
        }

        private void ResetDefaults()
        {
            projectileUid = 0;
            _selectedIndexProjectile = 0;

            damageType = ConfigCommon.DamageType.Physic;
            damage = 10;

            speedMultiplier = 1f;
            scaleMultiplier = 1f;

            targetMode = TargetSelectMode.NearMonster;
            nearMonsterSearchDistance = 1000f;
            targetMonsterUid = 0;
            areaRadiusOverride = 2f;

            autoSnapTargetMode = true;
            showSceneGizmos = true;

            manualTargetPosition = Vector3.zero;

            visualType = ProjectileConstants.ProjectileVisualType.Default;
            visualSprite = null;
            visualAnimatorController = null;
            visualEffectUidOverride = 0;
            _selectedIndexEffect = 0;

            count = 1;
            secDelayByOne = 0f;

            CacheProjectileInfo();
            ApplyAutoSnapTargetModeIfNeeded(force: true);
            RepaintSceneViews();
        }
        /// <summary>
        /// 현재 프레임에서 마우스 좌클릭이 눌렸는지 확인합니다.
        /// - New Input System / Old Input System을 분기 처리합니다.
        /// </summary>
        /// <summary>
        /// 마우스 스크린 좌표를 얻습니다.
        /// - New Input System / Old Input System을 분기 처리합니다.
        /// </summary>
        // ==============================
        // Table Edit (Projectile)
        // ==============================
        private void DrawProjectileRowEditor()
        {
            if (_cachedProjectileInfo == null || _editingProjectile == null)
            {
                EditorGUILayout.HelpBox("선택된 Projectile 데이터가 없습니다.", MessageType.Info);
                return;
            }

            _foldProjectileRowEdit = EditorGUILayout.Foldout(_foldProjectileRowEdit, "Projectile 테이블 편집(선택 Row)", true);
            if (!_foldProjectileRowEdit) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(new GUIContent("Uid"), _editingProjectile.Uid);
                }

                EditorGUI.BeginChangeCheck();

                _editingProjectile.Type = (ProjectileConstants.Type)EditorGUILayout.EnumPopup(new GUIContent("Type"), _editingProjectile.Type);
                _editingProjectile.Name = EditorGUILayout.TextField(new GUIContent("Name"), _editingProjectile.Name);

                _editingProjectile.EffectUid = EditorGUILayout.IntField(new GUIContent("EffectUid"), _editingProjectile.EffectUid);
                _editingProjectile.EffectScale = EditorGUILayout.FloatField(new GUIContent("EffectScale"), _editingProjectile.EffectScale);

                _editingProjectile.MoveSpeed = EditorGUILayout.IntField(new GUIContent("MoveSpeed"), _editingProjectile.MoveSpeed);

                _editingProjectile.ArcHeightMin = EditorGUILayout.IntField(new GUIContent("ArcHeightMin"), _editingProjectile.ArcHeightMin);
                _editingProjectile.ArcHeightMax = EditorGUILayout.IntField(new GUIContent("ArcHeightMax"), _editingProjectile.ArcHeightMax);

                _editingProjectile.StartPosition = EditorGUILayout.Vector2Field(new GUIContent("StartPosition (x,y)"), _editingProjectile.StartPosition);
                _editingProjectile.ColliderSize = EditorGUILayout.Vector2Field(new GUIContent("ColliderSize (x,y)"), _editingProjectile.ColliderSize);

                _editingProjectile.HitEffectUid = EditorGUILayout.IntField(new GUIContent("HitEffectUid"), _editingProjectile.HitEffectUid);

                _editingProjectile.TargetType = (ProjectileConstants.TargetType)EditorGUILayout.EnumPopup(new GUIContent("TargetType"), _editingProjectile.TargetType);
                _editingProjectile.TargetPositionRangeX = EditorGUILayout.IntField(new GUIContent("TargetPositionRangeX"), _editingProjectile.TargetPositionRangeX);

                _editingProjectile.Count = EditorGUILayout.IntField(new GUIContent("Count"), _editingProjectile.Count);
                _editingProjectile.SecDelayByOne = EditorGUILayout.FloatField(new GUIContent("SecDelayByOne"), _editingProjectile.SecDelayByOne);

                if (EditorGUI.EndChangeCheck())
                {
                    _editingProjectileDirty = true;
                }

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!_editingProjectileDirty))
                    {
                        if (GUILayout.Button("되돌리기"))
                        {
                            _editingProjectile = CloneProjectileRow(_cachedProjectileInfo);
                            _editingProjectileDirty = false;
                        }
                    }

                    using (new EditorGUI.DisabledScope(!_editingProjectileDirty))
                    {
                        if (GUILayout.Button("테스트 적용"))
                        {
                            // 인게임 TableLoaderManager 의 값도 바꿔주기
                            var info = GGemCo2DCore.TableLoaderManager.Instance.GetProjectileData(projectileUid);
                            if (info != null)
                            {
                                UpdateInGameProjectileTableInfo(_editingProjectile);
                            }
                        }
                    }

                    if (GUILayout.Button("저장(테이블 파일)"))
                    {
                        if (!ApplyEditingToCachedRow())
                            return;

                        if (!TrySaveProjectileTableFile(out var err))
                        {
                            EditorUtility.DisplayDialog(Title, err, "OK");
                            return;
                        }

                        // 저장 후 재로드
                        var keepUid = projectileUid;

                        TableLoaderManagerBase.Unload(ConfigAddressableTable.TableProjectile.Path);
                        _tableProjectile = TableLoaderManager.LoadProjectileTable(forceReload: true);
                        
                        // 인게임 TableLoaderManager 의 값도 바꿔주기
                        var info = GGemCo2DCore.TableLoaderManager.Instance.GetProjectileData(projectileUid);
                        if (info != null)
                        {
                            info.EffectUid = _editingProjectile.EffectUid;
                            info.EffectScale = _editingProjectile.EffectScale;
                            info.MoveSpeed = _editingProjectile.MoveSpeed;
                            info.ArcHeightMin = _editingProjectile.ArcHeightMin;
                            info.ArcHeightMax = _editingProjectile.ArcHeightMax;
                            info.StartPosition = _editingProjectile.StartPosition;
                            info.ColliderSize = _editingProjectile.ColliderSize;
                            info.HitEffectUid = _editingProjectile.HitEffectUid;
                            // info.TargetType = _editingProjectile.TargetType;
                            // info.TargetPositionRangeX = _editingProjectile.TargetPositionRangeX;
                        }

                        LoadProjectileDropdown();
                        projectileUid = keepUid;
                        SyncProjectileSelectionByUid();
                        CacheProjectileInfo();

                        ApplyAutoSnapTargetModeIfNeeded(force: true);
                        RepaintSceneViews();

                        _editingProjectileDirty = false;
                        ShowNotification(new GUIContent("Projectile 테이블 저장 완료"));
                    }
                }
            }
        }

        private bool ApplyEditingToCachedRow()
        {
            if (_cachedProjectileInfo == null || _editingProjectile == null)
                return false;

            // Uid는 키이므로 편집하지 않습니다(표시만).
            _cachedProjectileInfo.Type = _editingProjectile.Type;
            _cachedProjectileInfo.Name = _editingProjectile.Name;
            _cachedProjectileInfo.EffectUid = _editingProjectile.EffectUid;
            _cachedProjectileInfo.EffectScale = _editingProjectile.EffectScale;
            _cachedProjectileInfo.MoveSpeed = _editingProjectile.MoveSpeed;
            _cachedProjectileInfo.ArcHeightMin = _editingProjectile.ArcHeightMin;
            _cachedProjectileInfo.ArcHeightMax = _editingProjectile.ArcHeightMax;
            _cachedProjectileInfo.StartPosition = _editingProjectile.StartPosition;
            _cachedProjectileInfo.ColliderSize = _editingProjectile.ColliderSize;
            _cachedProjectileInfo.HitEffectUid = _editingProjectile.HitEffectUid;
            _cachedProjectileInfo.TargetType = _editingProjectile.TargetType;
            _cachedProjectileInfo.TargetPositionRangeX = _editingProjectile.TargetPositionRangeX;
            _cachedProjectileInfo.Count = _editingProjectile.Count;
            _cachedProjectileInfo.SecDelayByOne = _editingProjectile.SecDelayByOne;

            return true;
        }

        private static StruckTableProjectile CloneProjectileRow(StruckTableProjectile row)
        {
            if (row == null) return null;

            UpdateInGameProjectileTableInfo(row);
            return new StruckTableProjectile
            {
                Uid = row.Uid,
                Type = row.Type,
                Name = row.Name,
                EffectUid = row.EffectUid,
                EffectScale = row.EffectScale,
                MoveSpeed = row.MoveSpeed,
                ArcHeightMin = row.ArcHeightMin,
                ArcHeightMax = row.ArcHeightMax,
                StartPosition = row.StartPosition,
                ColliderSize = row.ColliderSize,
                HitEffectUid = row.HitEffectUid,
                TargetType = row.TargetType,
                TargetPositionRangeX = row.TargetPositionRangeX,
                Count = row.Count,
                SecDelayByOne = row.SecDelayByOne
            };
        }

        private static void UpdateInGameProjectileTableInfo(StruckTableProjectile row)
        {
            if (!GGemCo2DCore.TableLoaderManager.Instance) return;
            // 인게임 TableLoaderManager 의 값도 바꿔주기
            var info = GGemCo2DCore.TableLoaderManager.Instance.GetProjectileData(row.Uid);
            if (info != null)
            {
                info.EffectUid = row.EffectUid;
                info.EffectScale = row.EffectScale;
                info.MoveSpeed = row.MoveSpeed;
                info.ArcHeightMin = row.ArcHeightMin;
                info.ArcHeightMax = row.ArcHeightMax;
                info.StartPosition = row.StartPosition;
                info.ColliderSize = row.ColliderSize;
                info.HitEffectUid = row.HitEffectUid;
                // info.TargetType = _editingProjectile.TargetType;
                // info.TargetPositionRangeX = _editingProjectile.TargetPositionRangeX;
            }
        }

        private static string FormatFloat(float v) => v.ToString(CultureInfo.InvariantCulture);

        private static string FormatVector2(Vector2 v) => $"{FormatFloat(v.x)},{FormatFloat(v.y)}";

        private bool TrySaveProjectileTableFile(out string error)
        {
            error = null;

            if (_tableProjectile == null)
            {
                error = "Projectile 테이블이 로드되지 않았습니다.";
                return false;
            }

            try
            {
                var assetPath = ConfigAddressableTable.TableProjectile.Path; // Assets/.../projectile.txt
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var fullPath = Path.Combine(projectRoot ?? string.Empty, assetPath);

                // Canonical header order (TableProjectile 기준)
                var header = string.Join("\t", new[]
                {
                    "Uid","Type","Name","EffectUid","EffectScale","MoveSpeed","ArcHeightMin","ArcHeightMax",
                    "StartPosition","ColliderSize","HitEffectUid","TargetType","TargetPositionRangeX","Count","SecDelayByOne"
                });

                var sb = new StringBuilder(1024 * 32);
                sb.AppendLine(header);

                var datas = _tableProjectile.GetDatas();

                // uid 오름차순으로 저장(가독성/버전관리 용이)
                var uids = new List<int>(datas.Keys);
                uids.Sort();

                foreach (var uid in uids)
                {
                    if (!datas.TryGetValue(uid, out var r) || r == null)
                        continue;
                    if (r == null) continue;

                    sb.Append(r.Uid).Append('\t');
                    sb.Append(r.Type).Append('\t');
                    sb.Append(r.Name ?? string.Empty).Append('\t');
                    sb.Append(r.EffectUid).Append('\t');
                    sb.Append(FormatFloat(r.EffectScale)).Append('\t');
                    sb.Append(r.MoveSpeed).Append('\t');
                    sb.Append(r.ArcHeightMin).Append('\t');
                    sb.Append(r.ArcHeightMax).Append('\t');
                    sb.Append(FormatVector2(r.StartPosition)).Append('\t');
                    sb.Append(FormatVector2(r.ColliderSize)).Append('\t');
                    sb.Append(r.HitEffectUid).Append('\t');
                    sb.Append(r.TargetType).Append('\t');
                    sb.Append(r.TargetPositionRangeX).Append('\t');
                    sb.Append(r.Count).Append('\t');
                    sb.Append(FormatFloat(r.SecDelayByOne));
                    sb.AppendLine();
                }

                // 디스크에 저장
                File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));

                // 에셋 리임포트
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.Refresh();

                return true;
            }
            catch (Exception e)
            {
                error = $"Projectile 테이블 저장 중 오류: {e.Message}";
                return false;
            }
        }

        private static CharacterBase TryGetOwnerPlayer()
        {
            if (!Application.isPlaying || !SceneGame.Instance)
                return null;

            return SceneGame.Instance.player.GetComponent<CharacterBase>();
        }

    }
}
