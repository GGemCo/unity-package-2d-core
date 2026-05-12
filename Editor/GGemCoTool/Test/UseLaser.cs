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

        [SerializeField] private int laserUid;
        [SerializeField] private ConfigCommon.DamageType damageType = ConfigCommon.DamageType.Physic;
        [SerializeField] private long damage = 10;
        [SerializeField] private float scaleMultiplier = 1f;
        [SerializeField] private bool useDurationOverride;
        [SerializeField] private float durationOverride = 0.25f;
        [SerializeField] private bool useTickIntervalOverride;
        [SerializeField] private float tickIntervalOverride;
        [SerializeField] private bool useMaxDistanceOverride;
        [SerializeField] private float maxDistanceOverride = 10f;
        [SerializeField] private bool updateAimContinuously;
        [SerializeField] private ProjectileConstants.ProjectileVisualType visualType = ProjectileConstants.ProjectileVisualType.Default;
        [SerializeField] private int visualVfxUidOverride;
        [SerializeField] private Sprite visualSprite;
        [SerializeField] private RuntimeAnimatorController visualAnimatorController;
        [SerializeField] private TargetSelectMode targetMode = TargetSelectMode.NearMonster;
        [SerializeField] private float nearMonsterSearchDistance = 1000f;
        [SerializeField] private int targetMonsterUid;
        [SerializeField] private Vector3 manualTargetPosition = Vector3.right * 3f;
        [SerializeField] private bool showSceneGizmos = true;
        [SerializeField] private int count = 1;
        [SerializeField] private float delaySeconds;

        private Vector2 _scroll;
        private int _selectedIndexLaser;
        private readonly List<string> _namesLaser = new();
        private readonly List<int> _uidsLaser = new();
        private TableLaser _tableLaser;
        private StruckTableLaser _cachedLaserInfo;
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
                }
            }
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
                count = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Count"), count));
                delaySeconds = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("Delay(sec)"), delaySeconds));
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
            for (int i = 0; i < count; i++)
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
                    updateAimContinuously: updateAimContinuously);

                _laserController.Launch(meta);

                if (delaySeconds > 0f)
                    yield return new WaitForSeconds(delaySeconds);
            }
        }

        /// <summary>
        /// 현재 씬에서 UID에 해당하는 캐릭터를 찾습니다.
        /// </summary>
        private static CharacterBase FindCharacterByUid(int uid)
        {
            CharacterBase[] characters = Object.FindObjectsOfType<CharacterBase>();
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
            Vector3 start = player != null ? player.transform.position : Vector3.zero;
            start += (Vector3)_cachedLaserInfo.StartPosition;

            ResolveTarget(out CharacterBase targetCharacter, out bool useTargetPositionOverride, out Vector2 targetPositionOverride);
            Vector3 end = targetCharacter != null ? targetCharacter.transform.position : (Vector3)targetPositionOverride;

            Vector3 direction = end - start;
            if (direction.sqrMagnitude <= 1e-6f)
                direction = Vector3.right;

            float distance = useMaxDistanceOverride ? maxDistanceOverride : _cachedLaserInfo.MaxDistance;
            end = start + direction.normalized * distance;

            Handles.color = Color.cyan;
            Handles.DrawAAPolyLine(4f, start, end);
            Handles.SphereHandleCap(0, start, Quaternion.identity, 0.15f, EventType.Repaint);
            Handles.SphereHandleCap(0, end, Quaternion.identity, 0.12f, EventType.Repaint);
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
        /// 현재 선택된 레이저 정적 데이터를 캐시합니다.
        /// </summary>
        private void CacheLaserInfo()
        {
            _cachedLaserInfo = _tableLaser != null ? _tableLaser.GetDataByUid(laserUid) : null;
        }
    }
}
