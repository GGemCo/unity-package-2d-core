using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 프로젝타일 사용 툴(컬럼 직접 입력형)
    /// - 테이블 Uid 없이 파라미터 기반 발사
    /// - Count/Delay 지원
    /// - 기본값은 마지막 입력을 EditorPrefs로 복원
    /// </summary>
    public class UseProjectile : DefaultEditorWindow
    {
        private const string Title = "프로젝타일 사용툴";
        private const string PrefsKey = "GGemCo_UseProjectile_";

        [MenuItem(ConfigEditor.NameToolUseProjectile, false, (int)ConfigEditor.ToolOrdering.UseProjectile)]
        public static void ShowWindow() => GetWindow<UseProjectile>(Title);

        // ---- 입력 파라미터 (ProjectileSpawnRequest와 동일) ----
        [Header("식별/분류")]
        [Tooltip("선택 사항. 기록용/디버깅용 Uid 값")]
        [SerializeField] private int uid;
        [SerializeField] private SkillConstants.DamageType damageType = SkillConstants.DamageType.Physic;
        [SerializeField] private long damage = 10;

        [Tooltip("Projectile의 타입(직선/곡선/레이저 등)")]
        [SerializeField] private ProjectileConstants.Type type;

        [Header("비주얼/이펙트")]
        [Tooltip("발사 시 재생할 이펙트 Uid")]
        [SerializeField] private int effectUid;
        [Tooltip("이펙트 스케일")]
        [SerializeField] private float effectScale = 1f;

        [Header("이동/궤적")]
        [Tooltip("초당 이동 속도(픽셀/유닛 프로젝트 기준에 따름)")]
        [SerializeField] private int moveSpeed = 300;

        [Tooltip("포물선 궤적 최소 높이(곡선형에서 사용)")]
        [SerializeField] private float arcHeightMin = 30;
        [Tooltip("포물선 궤적 최대 높이(곡선형에서 사용)")]
        [SerializeField] private float arcHeightMax = 60;

        [Header("위치/충돌")]
        [Tooltip("시작 위치. 플레이어 기준 오프셋")]
        [SerializeField] private Vector2 startPosition = new(0, 0);
        [Tooltip("충돌 박스 사이즈")]
        [SerializeField] private Vector2 colliderSize = new(16, 16);

        [Header("히트/타겟팅")]
        [Tooltip("히트 시 재생할 이펙트 Uid")]
        [SerializeField] private int hitEffectUid;

        [Tooltip("타겟팅 방식")]
        [SerializeField] private ProjectileConstants.TargetType targetType = ProjectileConstants.TargetType.Fixed;

        [Tooltip("타겟 x 범위(플레이어 기준 +range ~ +range 랜덤)")]
        [SerializeField] private int targetPositionRangeX = 50;

        [Header("발사 갯수/딜레이")]
        [Tooltip("발사 개수")]
        [SerializeField] private int count = 1;
        [Tooltip("개별 발사 간 지연(초)")]
        [SerializeField] private float secDelayByOne;

        // 내부 상태
        private bool _foldVisual = true;
        private bool _foldMove = true;
        private bool _foldHit = true;
        private bool _foldSpawn = true;

        private int _selectedIndexEffect;
        private int _selectedIndexHitEffect;
        private readonly List<string> _namesEffect = new List<string>();
        private readonly List<int> _uidsEffect = new List<int>();
        private Dictionary<int, Dictionary<string, string>> _tableDictionaryEffect;
        
        private TableProjectile _tableProjectile;
        private TableEffect _tableEffect;
        private ProjectileController _projectileController;

        protected override void OnEnable()
        {
            base.OnEnable();
            // 순서 중요. LoadPrefs에서 _selectedIndexEffect를 불러온다.
            _selectedIndexEffect = 0;
            _selectedIndexHitEffect = 0;
            _tableEffect = TableLoaderManager.LoadEffectTable();
            _tableDictionaryEffect = _tableEffect.GetDatas();
            _projectileController ??= new ProjectileController();
            
            // 순서 중요. LoadPrefs에서 _uidsEffect 사용
            LoadTableInfoData();
            LoadPrefs();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("프로젝타일 파라미터 입력", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                uid = EditorGUILayout.IntField(new GUIContent("Uid(선택)"), uid);
                type = (ProjectileConstants.Type)EditorGUILayout.EnumPopup(new GUIContent("Type"), type);

                _foldVisual = EditorGUILayout.Foldout(_foldVisual, "비주얼/이펙트", true);
                if (_foldVisual)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        _selectedIndexEffect =
                            EditorGUILayout.Popup("이펙트 선택", _selectedIndexEffect, _namesEffect.ToArray());
                        
                        effectScale = EditorGUILayout.FloatField(new GUIContent("EffectScale"), effectScale);
                    }
                }

                _foldMove = EditorGUILayout.Foldout(_foldMove, "이동/궤적", true);
                if (_foldMove)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        moveSpeed = EditorGUILayout.IntField(new GUIContent("MoveSpeed"), moveSpeed);
                        EditorGUILayout.MinMaxSlider(new GUIContent("ArcHeight Min~Max"), ref arcHeightMin, ref arcHeightMax, -1000, 1000);
                        EditorGUILayout.BeginHorizontal();
                        arcHeightMin = EditorGUILayout.FloatField(new GUIContent("ArcHeightMin"), arcHeightMin, GUILayout.MaxWidth(220));
                        arcHeightMax = EditorGUILayout.FloatField(new GUIContent("ArcHeightMax"), arcHeightMax, GUILayout.MaxWidth(220));
                        EditorGUILayout.EndHorizontal();
                    }
                }

                _foldHit = EditorGUILayout.Foldout(_foldHit, "위치/충돌/히트", true);
                if (_foldHit)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        startPosition = EditorGUILayout.Vector2Field(new GUIContent("StartPosition(Offset)"), startPosition);
                        colliderSize = EditorGUILayout.Vector2Field(new GUIContent("ColliderSize"), colliderSize);
                        _selectedIndexHitEffect =
                            EditorGUILayout.Popup("HitEffectUid 선택", _selectedIndexHitEffect, _namesEffect.ToArray());
                    }
                }

                _foldSpawn = EditorGUILayout.Foldout(_foldSpawn, "타겟팅/발사", true);
                if (_foldSpawn)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        targetType = (ProjectileConstants.TargetType)EditorGUILayout.EnumPopup(new GUIContent("TargetType"), targetType);
                        targetPositionRangeX = EditorGUILayout.IntField(new GUIContent("TargetPositionRangeX"), targetPositionRangeX);

                        EditorGUILayout.Space(4);
                        count = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Count"), count));
                        secDelayByOne = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("SecDelayByOne"), secDelayByOne));
                    }
                }
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("기본값으로 초기화"))
                {
                    if (ResetDefaults())
                    {
                        SavePrefs();
                    }
                }
                if (GUILayout.Button("저장")) SavePrefs();
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("프로젝타일 발사")) CreateAndLaunch();
                }
                if (GUILayout.Button("복사")) Copy();
            }

            EditorGUILayout.HelpBox(
                "플레이 모드에서만 발사가 가능합니다.\n" +
                "Count/Delay는 단일 코루틴으로 처리되어 오버헤드를 최소화합니다.\n" +
                "TargetType이 Position일 경우, 플레이어 기준 X 범위에서 랜덤 타겟을 생성합니다.",
                MessageType.Info);
        }

        private static string F(float v) => v.ToString(CultureInfo.InvariantCulture);
        private static string F(int v)   => v.ToString(CultureInfo.InvariantCulture);
        private static string F(long v)  => v.ToString(CultureInfo.InvariantCulture);
        private void Copy()
        {
            // StartPosition, ColliderSize는 "x,y" 형식으로 일관 유지
            string startPos   = $"{F(startPosition.x)},{F(startPosition.y)}";
            string colliderSz = $"{F(colliderSize.x)},{F(colliderSize.y)}";
            
            // TSV 컬럼 순서(실무에서 가장 많이 참조할 항목 우선)
            // Uid, Type, EffectUid, EffectScale, MoveSpeed,
            // ArcHeightMin, ArcHeightMax, StartPosition, ColliderSize,
            // HitEffectUid, TargetType, TargetPositionRangeX, Count, SecDelayByOne
            string[] cols =
            {
                F(uid),
                type.ToString(),
                "",
                F(_uidsEffect[_selectedIndexEffect]),
                F(effectScale),
                F(moveSpeed),
                F(arcHeightMin),
                F(arcHeightMax),
                startPos,
                colliderSz,
                F(_uidsEffect[_selectedIndexHitEffect]),
                targetType.ToString(),
                F(targetPositionRangeX),
                F(count),
                F(secDelayByOne)
            };

            string tsv = string.Join("\t", cols);
            EditorGUIUtility.systemCopyBuffer = tsv;

            // 사용자 피드백
            ShowNotification(new GUIContent("프로젝타일 파라미터를 클립보드에 복사했습니다."));
        }

        private void CreateAndLaunch()
        {
            if (!Application.isPlaying || !SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "플레이 모드에서 실행해주세요.", "OK");
                return;
            }
            var player = SceneGame.Instance.player;
            if (!player)
            {
                EditorUtility.DisplayDialog(Title, "플레이어가 없습니다.", "OK");
                return;
            }

            _projectileController.Initialize(player.GetComponent<CharacterBase>());

            var info = new StruckTableProjectile
            {
                Uid = uid,
                Type = type,
                Name = string.Empty,

                EffectUid = _uidsEffect[_selectedIndexEffect],
                EffectScale = Mathf.Max(0.01f, effectScale),

                MoveSpeed = Mathf.Max(0, moveSpeed),
                ArcHeightMin = (int)Mathf.Min(arcHeightMin, arcHeightMax),
                ArcHeightMax = (int)Mathf.Max(arcHeightMin, arcHeightMax),

                StartPosition = startPosition,
                ColliderSize = new Vector2(Mathf.Max(0.01f, colliderSize.x), Mathf.Max(0.01f, colliderSize.y)),

                HitEffectUid = _uidsEffect[_selectedIndexHitEffect],

                TargetType = targetType,
                TargetPositionRangeX = Mathf.Max(0, targetPositionRangeX),

                Count = Mathf.Max(1, count),
                SecDelayByOne = Mathf.Max(0f, secDelayByOne),
            };

            var target = SceneGame.Instance.mapManager.GetNearByMonsterDistance(1000);
            SceneGame.Instance.StartCoroutine(CreateProjectileBurst(info, target, info));
        }
        private IEnumerator CreateProjectileBurst(StruckTableProjectile info, CharacterBase target, StruckTableProjectile struckTableProjectile)
        {
            // 목표가 필요한 타입인데 타겟이 없다면 중단
            if (info.TargetType == ProjectileConstants.TargetType.Fixed && !target)
                yield break;

            var character = SceneGame.Instance.player.GetComponent<CharacterBase>();
            for (int i = 0; i < info.Count; i++)
            {
                var proj = SceneGame.Instance.ProjectileManager.CreateProjectile(struckTableProjectile);
                if (proj != null)
                {
                    proj.SetFromCharacter(character);
                    proj.SetDamage(damage);

                    // 좌표 산출
                    if (info.TargetType == ProjectileConstants.TargetType.Fixed)
                    {
                        proj.Launch(target);
                    }
                    else
                    {
                        // Area/None: 좌표 기반
                        // 직선형은 X를 고정, 곡선형은 X를 범위에서 샘플
                        float x = target
                            ? target.transform.position.x
                            : character.transform.position.x;

                        bool isArc = (info.ArcHeightMin > 0) || (info.ArcHeightMax > 0);
                        if (isArc && target)
                        {
                            x = Random.Range(target.transform.position.x - info.TargetPositionRangeX,
                                target.transform.position.x + info.TargetPositionRangeX);
                        }

                        float y = target
                            ? target.GetRandomPositionYInHitArea()
                            : character.transform.position.y;

                        proj.Launch(new Vector2(x, y));
                    }
                }

                float delay = Mathf.Max(0f, info.SecDelayByOne);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
        private void LoadTableInfoData()
        {
            _namesEffect.Clear();
            _uidsEffect.Clear();
            _namesEffect.Add("None");
            _uidsEffect.Add(0);
            foreach (var kvp in _tableDictionaryEffect)
            {
                var info = _tableEffect.GetDataByUid(kvp.Key);
                if (info.Uid <= 0) continue;

                _namesEffect.Add($"{info.Uid} - {info.Name}");
                _uidsEffect.Add(info.Uid);
            }
            _selectedIndexEffect = 0; // 추가
            _selectedIndexHitEffect = 0; // 추가
        }
        private int GetEffectIndex(int searchUid)
        {
            for (int i = 0; i < _uidsEffect.Count; i++)
            {
                if (searchUid == _uidsEffect[i]) return i;
            }

            return -1;
        }

        #region Prefs
        private void SavePrefs()
        {
            bool result = EditorUtility.DisplayDialog("저장하기", "현재 저장된 값을 덮어씌웁니다.\n저장하시겠습니까?", "네", "아니요");
            if (!result) return;
            
            EditorPrefs.SetInt(PrefsKey + "uid", uid);
            EditorPrefs.SetInt(PrefsKey + "type", (int)type);
            EditorPrefs.SetString(PrefsKey + "name", "");

            EditorPrefs.SetInt(PrefsKey + "effectUid", _uidsEffect[_selectedIndexEffect]);
            EditorPrefs.SetFloat(PrefsKey + "effectScale", effectScale);

            EditorPrefs.SetInt(PrefsKey + "moveSpeed", moveSpeed);
            EditorPrefs.SetFloat(PrefsKey + "arcMin", arcHeightMin);
            EditorPrefs.SetFloat(PrefsKey + "arcMax", arcHeightMax);

            EditorPrefs.SetFloat(PrefsKey + "startX", startPosition.x);
            EditorPrefs.SetFloat(PrefsKey + "startY", startPosition.y);
            EditorPrefs.SetFloat(PrefsKey + "colX", colliderSize.x);
            EditorPrefs.SetFloat(PrefsKey + "colY", colliderSize.y);

            EditorPrefs.SetInt(PrefsKey + "hitEffectUid", _uidsEffect[_selectedIndexHitEffect]);
            EditorPrefs.SetInt(PrefsKey + "targetType", (int)targetType);
            EditorPrefs.SetInt(PrefsKey + "targetRangeX", targetPositionRangeX);

            EditorPrefs.SetInt(PrefsKey + "count", count);
            EditorPrefs.SetFloat(PrefsKey + "delay", secDelayByOne);
        }

        private void LoadPrefs()
        {
            uid = EditorPrefs.GetInt(PrefsKey + "uid");
            type = (ProjectileConstants.Type)EditorPrefs.GetInt(PrefsKey + "type", (int)ProjectileConstants.Type.Default);

            effectUid = EditorPrefs.GetInt(PrefsKey + "effectUid");
            _selectedIndexEffect = GetEffectIndex(effectUid);
            effectScale = EditorPrefs.GetFloat(PrefsKey + "effectScale");

            moveSpeed = EditorPrefs.GetInt(PrefsKey + "moveSpeed");
            arcHeightMin = EditorPrefs.GetInt(PrefsKey + "arcMin");
            arcHeightMax = EditorPrefs.GetInt(PrefsKey + "arcMax");

            startPosition = new Vector2(
                EditorPrefs.GetFloat(PrefsKey + "startX"),
                EditorPrefs.GetFloat(PrefsKey + "startY")
            );
            colliderSize = new Vector2(
                EditorPrefs.GetFloat(PrefsKey + "colX"),
                EditorPrefs.GetFloat(PrefsKey + "colY")
            );

            hitEffectUid = EditorPrefs.GetInt(PrefsKey + "hitEffectUid");
            _selectedIndexHitEffect = GetEffectIndex(hitEffectUid);
            targetType = (ProjectileConstants.TargetType)EditorPrefs.GetInt(PrefsKey + "targetType", (int)ProjectileConstants.TargetType.Fixed);
            targetPositionRangeX = EditorPrefs.GetInt(PrefsKey + "targetRangeX");

            count = Mathf.Max(1, EditorPrefs.GetInt(PrefsKey + "count"));
            secDelayByOne = Mathf.Max(0f, EditorPrefs.GetFloat(PrefsKey + "delay"));
        }

        private bool ResetDefaults()
        {
            bool result = EditorUtility.DisplayDialog("기본값으로 초기화", "현재 저장된 값을 초기화 합니다.\n계속하시겠습니까?", "네", "아니요");
            if (!result) return false;
            
            uid = 0;
            type = ProjectileConstants.Type.Default;
            effectUid = 0;
            _selectedIndexEffect = 0;
            effectScale = 1f;
            moveSpeed = 300;
            arcHeightMin = 30;
            arcHeightMax = 60;
            startPosition = Vector2.zero;
            colliderSize = new Vector2(16, 16);
            hitEffectUid = 0;
            _selectedIndexHitEffect = 0;
            targetType = ProjectileConstants.TargetType.Fixed;
            targetPositionRangeX = 50;
            count = 1;
            secDelayByOne = 0f;
            return true;
        }
        #endregion
    }
}
