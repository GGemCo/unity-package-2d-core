#if UNITY_EDITOR
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// start/play/end 스프라이트 시퀀스를 기반으로 AnimationClip 3개와 AnimatorController,
    /// 그리고 이를 참조하는 이펙트 Prefab을 자동 생성하는 에디터 윈도우입니다.
    /// </summary>
    /// <remarks>
    /// - 클립 이름은 start/play/end로 고정됩니다.
    /// - start/end 마지막 프레임에는 완료 이벤트(<see cref="EventNameComplete"/>)가 자동 추가됩니다.
    /// - 생성 파일의 저장 경로 및 마지막 입력값은 EditorPrefs로 유지됩니다.
    /// </remarks>
    public sealed class VfxEffectPrefabAutoBuilderWindow : EditorWindow
    {
        /// <summary>
        /// 생성될 프리팹이 스프라이트를 표시할 대상 컴포넌트 타입입니다.
        /// </summary>
        private enum EffectTargetType
        {
            /// <summary>SpriteRenderer의 m_Sprite를 애니메이션합니다.</summary>
            SpriteRenderer,

            /// <summary>UI Image의 m_Sprite를 애니메이션합니다.</summary>
            UiImage
        }

        /// <summary>
        /// 스프라이트 리스트를 클립에 적용하기 전 정렬 방식입니다.
        /// </summary>
        private enum SortMode
        {
            /// <summary>사용자가 입력/정렬한 순서를 유지합니다.</summary>
            KeepInputOrder,

            /// <summary>스프라이트 이름을 기준으로 오름차순 정렬합니다.</summary>
            ByNameAscending
        }

        /// <summary>
        /// 하나의 클립(start/play/end)에 들어갈 스프라이트 목록과 정렬 설정을 묶어 보관합니다.
        /// </summary>
        [Serializable]
        private sealed class ClipSprites
        {
            /// <summary>클립에 들어갈 스프라이트 시퀀스입니다.</summary>
            public List<Sprite> sprites = new List<Sprite>(32);

            /// <summary>클립 생성 전 적용할 정렬 방식입니다.</summary>
            public SortMode sortMode = SortMode.KeepInputOrder;
        }

        // -------------------------
        // EditorPrefs Keys
        // -------------------------
        /// <summary>이 툴에서 사용하는 EditorPrefs 키 접두사입니다.</summary>
        private const string PrefKeyPrefix = "GGemCo.VfxEffectPrefabAutoBuilder.";

        private const string KeyTargetType     = PrefKeyPrefix + "TargetType";
        private const string KeyPrefabName     = PrefKeyPrefix + "PrefabName";
        private const string KeyFps            = PrefKeyPrefix + "Fps";
        private const string KeyPrefabFolder   = PrefKeyPrefix + "PrefabFolderPath";
        private const string KeyAnimFolder     = PrefKeyPrefix + "AnimFolderPath";

        private const string KeyStartSort      = PrefKeyPrefix + "StartSort";
        private const string KeyPlaySort       = PrefKeyPrefix + "PlaySort";
        private const string KeyEndSort        = PrefKeyPrefix + "EndSort";

        private const string KeyStartSprites   = PrefKeyPrefix + "StartSprites";
        private const string KeyPlaySprites    = PrefKeyPrefix + "PlaySprites";
        private const string KeyEndSprites     = PrefKeyPrefix + "EndSprites";

        // Object Picker Ids
        /// <summary>start 클립용 Object Picker 컨트롤 ID입니다.</summary>
        private const int ObjectPickerStartId  = 12001;

        /// <summary>play 클립용 Object Picker 컨트롤 ID입니다.</summary>
        private const int ObjectPickerPlayId   = 12002;

        /// <summary>end 클립용 Object Picker 컨트롤 ID입니다.</summary>
        private const int ObjectPickerEndId    = 12003;

        // -------------------------
        // State
        // -------------------------
        /// <summary>스크롤 뷰 위치입니다.</summary>
        private Vector2 _scroll;

        /// <summary>프리팹이 스프라이트를 표시할 컴포넌트 타입입니다.</summary>
        private EffectTargetType _targetType = EffectTargetType.SpriteRenderer;

        // Output
        /// <summary>생성할 Prefab 저장 폴더(프로젝트 내 폴더 에셋)입니다.</summary>
        private DefaultAsset? _prefabOutputFolder;

        /// <summary>AnimatorController 및 AnimationClip 저장 폴더(프로젝트 내 폴더 에셋)입니다.</summary>
        private DefaultAsset? _animOutputFolder;

        /// <summary>생성될 프리팹 이름(및 controller 파일명 prefix)입니다.</summary>
        private string _prefabName = "Effect_New";

        // Animation
        /// <summary>클립의 프레임레이트(FPS)입니다.</summary>
        private float _fps = 12f;

        /// <summary>start 클립 입력(스프라이트/정렬)입니다.</summary>
        private readonly ClipSprites _start = new ClipSprites();

        /// <summary>play 클립 입력(스프라이트/정렬)입니다.</summary>
        private readonly ClipSprites _play  = new ClipSprites();

        /// <summary>end 클립 입력(스프라이트/정렬)입니다.</summary>
        private readonly ClipSprites _end   = new ClipSprites();

        /// <summary>start 클립의 고정 이름입니다.</summary>
        private const string ClipNameStart = "start";

        /// <summary>play 클립의 고정 이름입니다.</summary>
        private const string ClipNamePlay  = "play";

        /// <summary>end 클립의 고정 이름입니다.</summary>
        private const string ClipNameEnd   = "end";

        /// <summary>start/end 마지막 프레임에 자동 추가되는 애니메이션 이벤트 함수명입니다.</summary>
        private const string EventNameComplete = "GGemCoAniEventComplete";

        /// <summary>윈도우 타이틀 텍스트입니다.</summary>
        private const string Title = "이팩트 프리팹 생성툴";

        // -------------------------
        // Menu
        // -------------------------
        /// <summary>
        /// 메뉴에서 본 윈도우를 열고 기본 크기/타이틀을 설정합니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolCreateVfxEffectPrefab, false, (int)ConfigEditor.ToolOrdering.CreateVfxEffectPrefab)]
        public static void Open()
        {
            var w = GetWindow<VfxEffectPrefabAutoBuilderWindow>();
            w.titleContent = new GUIContent(Title);
            w.minSize = new Vector2(600, 620);
            w.Show();
        }

        /// <summary>
        /// 윈도우 활성화 시 EditorPrefs를 로드하고, 출력 폴더가 비어있다면 Assets로 보정합니다.
        /// </summary>
        private void OnEnable()
        {
            LoadPrefs();

            // 기본 폴더 보정
            if (_prefabOutputFolder == null)
                _prefabOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");

            if (_animOutputFolder == null)
                _animOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
        }

        /// <summary>
        /// 윈도우 비활성화 시 현재 설정을 EditorPrefs에 저장합니다.
        /// </summary>
        private void OnDisable()
        {
            SavePrefs();
        }

        /// <summary>
        /// 에디터 UI를 렌더링하고 입력값 검증 후 빌드 버튼을 처리합니다.
        /// </summary>
        private void OnGUI()
        {
            HandleObjectPickerEvents();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("이펙트 프리팹 자동 생성기", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _targetType = (EffectTargetType)EditorGUILayout.EnumPopup("대상 타입", _targetType);
                _prefabName = EditorGUILayout.TextField("프리팹 이름", _prefabName);

                _fps = EditorGUILayout.FloatField("FPS(초당 프레임)", _fps);
                _fps = Mathf.Clamp(_fps, 1f, 120f);
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("출력 폴더", EditorStyles.boldLabel);

                _prefabOutputFolder = (DefaultAsset?)EditorGUILayout.ObjectField(
                    "프리팹 출력 폴더",
                    _prefabOutputFolder,
                    typeof(DefaultAsset),
                    false);

                _animOutputFolder = (DefaultAsset?)EditorGUILayout.ObjectField(
                    "애니메이터/클립 출력 폴더",
                    _animOutputFolder,
                    typeof(DefaultAsset),
                    false);

                EditorGUILayout.HelpBox(
                    "Prefab은 프리팹 출력 폴더에 저장됩니다.\n" +
                    "AnimatorController(.controller) 및 AnimationClip(.anim)은 애니메이터/클립 출력 폴더에 저장됩니다.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("클립별 스프라이트 (start / play / end)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "클립 이름은 start/play/end로 고정됩니다.\n" +
                    "드래그앤드롭으로 여러 장을 한 번에 넣을 수 있습니다.\n" +
                    "start/end 마지막 프레임에 GGemCoAniEventComplete 이벤트가 자동 추가됩니다.",
                    MessageType.Info);

                DrawSpriteListSection(ClipNameStart, _start, ObjectPickerStartId);
                DrawSpriteListSection(ClipNamePlay,  _play,  ObjectPickerPlayId);
                DrawSpriteListSection(ClipNameEnd,   _end,   ObjectPickerEndId);
            }

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(!CanBuild(out _)))
            {
                if (GUILayout.Button("프리팹 + 애니메이터 + 클립 생성", GUILayout.Height(36)))
                {
                    if (!CanBuild(out var error))
                    {
                        EditorUtility.DisplayDialog("생성 실패", error, "확인");
                        EditorGUILayout.EndScrollView();
                        return;
                    }

                    try
                    {
                        Build();
                        SavePrefs();
                        EditorUtility.DisplayDialog("생성 완료", "생성이 완료되었습니다.", "확인");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        EditorUtility.DisplayDialog("생성 실패", ex.Message, "확인");
                    }
                }
            }

            if (!CanBuild(out var warn))
                EditorGUILayout.HelpBox(warn, MessageType.Warning);

            EditorGUILayout.EndScrollView();

            // UI 변경 시 즉시 저장(선택 사항이지만, “입력/선택 저장” 만족을 위해 안전하게 적용)
            if (GUI.changed)
                SavePrefs();
        }

        // -------------------------
        // UI
        // -------------------------
        /// <summary>
        /// start/play/end 중 하나의 클립 섹션 UI(드롭 영역, 정렬, 개별 항목 편집)를 그립니다.
        /// </summary>
        /// <param name="clipLabel">표시할 클립 레이블(및 고정 클립명)입니다.</param>
        /// <param name="clip">해당 클립의 입력 데이터(스프라이트/정렬)입니다.</param>
        /// <param name="pickerId">Object Picker에서 구분에 사용할 컨트롤 ID입니다.</param>
        private void DrawSpriteListSection(string clipLabel, ClipSprites clip, int pickerId)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"{clipLabel} 클립 스프라이트", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawDropArea(clip.sprites);

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    clip.sortMode = (SortMode)EditorGUILayout.EnumPopup("정렬", clip.sortMode);

                    if (GUILayout.Button("정렬 적용", GUILayout.Width(110)))
                        ApplySort(clip.sprites, clip.sortMode);
                }

                EditorGUILayout.Space(6);

                int removeIndex = -1;

                for (int i = 0; i < clip.sprites.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        clip.sprites[i] = (Sprite)EditorGUILayout.ObjectField($"[{i:00}]", clip.sprites[i], typeof(Sprite), false);

                        using (new EditorGUI.DisabledScope(i <= 0))
                        {
                            if (GUILayout.Button("▲", GUILayout.Width(28)))
                                (clip.sprites[i - 1], clip.sprites[i]) = (clip.sprites[i], clip.sprites[i - 1]);
                        }

                        using (new EditorGUI.DisabledScope(i >= clip.sprites.Count - 1))
                        {
                            if (GUILayout.Button("▼", GUILayout.Width(28)))
                                (clip.sprites[i + 1], clip.sprites[i]) = (clip.sprites[i], clip.sprites[i + 1]);
                        }

                        if (GUILayout.Button("X", GUILayout.Width(28)))
                            removeIndex = i;
                    }
                }

                if (removeIndex >= 0)
                    clip.sprites.RemoveAt(removeIndex);

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ 스프라이트 추가", GUILayout.Height(22)))
                        clip.sprites.Add(null!);

                    if (GUILayout.Button("+ 스프라이트 선택…", GUILayout.Height(22)))
                    {
                        // Unity 오브젝트 피커는 환경에 따라 “진짜 멀티 선택”이 보장되지 않으므로,
                        // 멀티는 드래그앤드롭을 주력으로 사용합니다.
                        EditorGUIUtility.ShowObjectPicker<Sprite>(null, false, "", pickerId);
                    }

                    if (GUILayout.Button("모두 비우기", GUILayout.Height(22)))
                        clip.sprites.Clear();
                }
            }
        }

        /// <summary>
        /// 프로젝트 창에서 드래그된 Sprite/Texture2D를 받아 스프라이트 목록에 추가하는 드롭 영역을 그립니다.
        /// </summary>
        /// <param name="targetList">드롭된 스프라이트가 추가될 대상 리스트입니다.</param>
        private void DrawDropArea(List<Sprite> targetList)
        {
            var rect = GUILayoutUtility.GetRect(0, 46, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "여기에 스프라이트를 드롭하세요 (Project 창에서 여러 개 드래그 가능)", EditorStyles.helpBox);

            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;

            if (!rect.Contains(evt.mousePosition))
                return;

            bool hasValid = DragAndDrop.objectReferences.Any(IsSpriteOrTexture);
            if (!hasValid)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                var spritesToAdd = CollectSpritesFromDraggedObjects(DragAndDrop.objectReferences);
                AddSprites(targetList, spritesToAdd, allowDuplicates: false);
                evt.Use();
            }
            else
            {
                evt.Use();
            }
        }

        /// <summary>
        /// 드래그/피커 입력 오브젝트가 Sprite 또는 Texture2D인지 판별합니다.
        /// </summary>
        /// <param name="obj">검사할 유니티 오브젝트입니다.</param>
        /// <returns>Sprite 또는 Texture2D이면 true를 반환합니다.</returns>
        private static bool IsSpriteOrTexture(UnityEngine.Object obj)
            => obj is Sprite || obj is Texture2D;

        /// <summary>
        /// 드래그된 오브젝트 목록에서 Sprite를 수집합니다.
        /// Texture2D가 들어오면 해당 텍스처 경로의 서브에셋(Sprite)까지 모두 수집합니다.
        /// </summary>
        /// <param name="objects">드래그된 유니티 오브젝트 배열입니다.</param>
        /// <returns>중복이 제거된 Sprite 리스트입니다.</returns>
        private static List<Sprite> CollectSpritesFromDraggedObjects(UnityEngine.Object[] objects)
        {
            var list = new List<Sprite>(64);

            foreach (var obj in objects)
            {
                if (obj is Sprite sp)
                {
                    list.Add(sp);
                    continue;
                }

                if (obj is Texture2D tex)
                {
                    var path = AssetDatabase.GetAssetPath(tex);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var sa in subAssets)
                    {
                        if (sa is Sprite subSprite)
                            list.Add(subSprite);
                    }
                }
            }

            return list
                .Where(s => s != null)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 스프라이트 리스트에 새 스프라이트들을 추가합니다.
        /// </summary>
        /// <param name="target">추가 대상 리스트입니다.</param>
        /// <param name="add">추가할 스프라이트 리스트입니다.</param>
        /// <param name="allowDuplicates">중복 허용 여부입니다.</param>
        private static void AddSprites(List<Sprite> target, List<Sprite> add, bool allowDuplicates)
        {
            if (add.Count == 0) return;

            if (allowDuplicates)
            {
                target.AddRange(add);
                return;
            }

            var set = new HashSet<Sprite>(target);
            foreach (var s in add)
            {
                if (s == null) continue;
                if (set.Add(s))
                    target.Add(s);
            }
        }

        /// <summary>
        /// 정렬 모드에 따라 스프라이트 리스트를 정렬합니다.
        /// </summary>
        /// <param name="list">정렬할 리스트입니다.</param>
        /// <param name="mode">정렬 방식입니다.</param>
        private static void ApplySort(List<Sprite> list, SortMode mode)
        {
            if (mode == SortMode.KeepInputOrder) return;

            list.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                return string.CompareOrdinal(a.name, b.name);
            });
        }

        /// <summary>
        /// Object Picker에서 선택된 오브젝트(Sprite/Texture2D)를 감지해 해당 클립 리스트에 추가합니다.
        /// </summary>
        private void HandleObjectPickerEvents()
        {
            if (Event.current.commandName != "ObjectSelectorUpdated" &&
                Event.current.commandName != "ObjectSelectorClosed")
                return;

            int id = EditorGUIUtility.GetObjectPickerControlID();
            var picked = EditorGUIUtility.GetObjectPickerObject();
            if (picked == null) return;

            if (picked is Sprite sp)
            {
                switch (id)
                {
                    case ObjectPickerStartId: AddSprites(_start.sprites, new List<Sprite> { sp }, false); break;
                    case ObjectPickerPlayId:  AddSprites(_play.sprites,  new List<Sprite> { sp }, false); break;
                    case ObjectPickerEndId:   AddSprites(_end.sprites,   new List<Sprite> { sp }, false); break;
                }
            }
            else if (picked is Texture2D tex)
            {
                var sprites = CollectSpritesFromDraggedObjects(new UnityEngine.Object[] { tex });
                switch (id)
                {
                    case ObjectPickerStartId: AddSprites(_start.sprites, sprites, false); break;
                    case ObjectPickerPlayId:  AddSprites(_play.sprites,  sprites, false); break;
                    case ObjectPickerEndId:   AddSprites(_end.sprites,   sprites, false); break;
                }
            }

            Repaint();
        }

        // -------------------------
        // Validation
        // -------------------------
        /// <summary>
        /// 현재 입력값으로 빌드 가능 여부를 검사합니다.
        /// </summary>
        /// <param name="error">실패 시 사용자에게 보여줄 오류 메시지입니다.</param>
        /// <returns>빌드 가능하면 true, 불가능하면 false를 반환합니다.</returns>
        private bool CanBuild(out string error)
        {
            if (string.IsNullOrWhiteSpace(_prefabName))
            {
                error = "프리팹 이름을 입력하세요.";
                return false;
            }

            if (_prefabOutputFolder == null)
            {
                error = "프리팹 출력 폴더를 지정하세요.";
                return false;
            }

            if (_animOutputFolder == null)
            {
                error = "애니메이터/클립 출력 폴더를 지정하세요.";
                return false;
            }

            var prefabFolderPath = AssetDatabase.GetAssetPath(_prefabOutputFolder);
            if (string.IsNullOrEmpty(prefabFolderPath) || !AssetDatabase.IsValidFolder(prefabFolderPath))
            {
                error = "프리팹 출력 폴더가 유효한 프로젝트 폴더가 아닙니다.";
                return false;
            }

            var animFolderPath = AssetDatabase.GetAssetPath(_animOutputFolder);
            if (string.IsNullOrEmpty(animFolderPath) || !AssetDatabase.IsValidFolder(animFolderPath))
            {
                error = "애니메이터/클립 출력 폴더가 유효한 프로젝트 폴더가 아닙니다.";
                return false;
            }

            if (_start.sprites.Count == 0 || _play.sprites.Count == 0 || _end.sprites.Count == 0)
            {
                error = "start/play/end 각각 최소 1개의 스프라이트가 필요합니다.";
                return false;
            }

            if (HasNull(_start.sprites) || HasNull(_play.sprites) || HasNull(_end.sprites))
            {
                error = "스프라이트 리스트에 비어있는 항목(null)이 있습니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 스프라이트 리스트에 null 항목이 포함되어 있는지 검사합니다.
        /// </summary>
        /// <param name="sprites">검사할 스프라이트 리스트입니다.</param>
        /// <returns>null이 하나라도 있으면 true를 반환합니다.</returns>
        private static bool HasNull(List<Sprite> sprites)
        {
            foreach (var sprite in sprites)
            {
                if (sprite == null) return true;
            }

            return false;
        }

        // -------------------------
        // Build
        // -------------------------
        /// <summary>
        /// 입력된 start/play/end 스프라이트로 AnimationClip 3개와 AnimatorController, Prefab을 생성합니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 지원하지 않는 <see cref="EffectTargetType"/> 값이 들어왔을 때 발생합니다.
        /// </exception>
        private void Build()
        {
            var prefabFolderPath = AssetDatabase.GetAssetPath(_prefabOutputFolder!);
            var animFolderPath   = AssetDatabase.GetAssetPath(_animOutputFolder!);

            // 정렬 옵션 반영(필요시)
            ApplySort(_start.sprites, _start.sortMode);
            ApplySort(_play.sprites,  _play.sortMode);
            ApplySort(_end.sprites,   _end.sortMode);

            // 1) Clips (name 고정: start/play/end)
            var startClip = CreateSpriteSwapClip(ClipNameStart, _start.sprites, _fps, loop: false, _targetType);
            var playClip  = CreateSpriteSwapClip(ClipNamePlay,  _play.sprites,  _fps, loop: true,  _targetType);
            var endClip   = CreateSpriteSwapClip(ClipNameEnd,   _end.sprites,   _fps, loop: false, _targetType);

            // 2) start/end 마지막 프레임에 이벤트 추가
            AddCompleteEventAtLastFrame(startClip, _start.sprites.Count, _fps);
            AddCompleteEventAtLastFrame(endClip,   _end.sprites.Count,   _fps);

            // 클립 name은 고정
            var startPath = AssetDatabase.GenerateUniqueAssetPath($"{animFolderPath}/{ClipNameStart}.anim");
            var playPath  = AssetDatabase.GenerateUniqueAssetPath($"{animFolderPath}/{ClipNamePlay}.anim");
            var endPath   = AssetDatabase.GenerateUniqueAssetPath($"{animFolderPath}/{ClipNameEnd}.anim");

            AssetDatabase.CreateAsset(startClip, startPath);
            AssetDatabase.CreateAsset(playClip,  playPath);
            AssetDatabase.CreateAsset(endClip,   endPath);

            // 3) AnimatorController 생성
            var controllerPath = AssetDatabase.GenerateUniqueAssetPath($"{animFolderPath}/{_prefabName}.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm = controller.layers[0].stateMachine;

            // 3-1) Empty State 생성 + Default 지정
            var stEmpty = sm.AddState("Empty");
            stEmpty.motion = null;
            sm.defaultState = stEmpty;

            // 3-2) start/play/end State만 추가 (Transition은 생성하지 않음)
            var stStart = sm.AddState(ClipNameStart);
            stStart.motion = startClip;

            var stPlay = sm.AddState(ClipNamePlay);
            stPlay.motion = playClip;

            var stEnd = sm.AddState(ClipNameEnd);
            stEnd.motion = endClip;

            // 4) Prefab 생성(Animator는 Controller만 연결)
            var root = new GameObject(_prefabName);
            try
            {
                var animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;

                switch (_targetType)
                {
                    case EffectTargetType.SpriteRenderer:
                        root.AddComponent<SpriteRenderer>();
                        break;

                    case EffectTargetType.UiImage:
                    {
                        var rt = root.AddComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = new Vector2(256, 256);

                        root.AddComponent<CanvasRenderer>();
                        root.AddComponent<Image>();
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{prefabFolderPath}/{_prefabName}.prefab");
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// SpriteRenderer 또는 UI Image의 m_Sprite를 프레임 단위로 교체하는 AnimationClip을 생성합니다.
        /// </summary>
        /// <param name="clipName">생성될 클립의 이름입니다.</param>
        /// <param name="sprites">프레임 순서대로 적용할 스프라이트 목록입니다.</param>
        /// <param name="fps">클립의 프레임레이트(FPS)입니다.</param>
        /// <param name="loop">클립 루프 여부입니다.</param>
        /// <param name="targetType">대상 컴포넌트 타입(SpriteRenderer / Image)입니다.</param>
        /// <returns>생성된 <see cref="AnimationClip"/> 입니다.</returns>
        private static AnimationClip CreateSpriteSwapClip(
            string clipName,
            List<Sprite> sprites,
            float fps,
            bool loop,
            EffectTargetType targetType)
        {
            var clip = new AnimationClip
            {
                name = clipName,
                frameRate = fps
            };

            var binding = new EditorCurveBinding
            {
                path = "",
                propertyName = "m_Sprite",
                type = (targetType == EffectTargetType.SpriteRenderer)
                    ? typeof(SpriteRenderer)
                    : typeof(Image)
            };

            var keys = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>
        /// 클립의 마지막 프레임 시점에 완료 이벤트(<see cref="EventNameComplete"/>)를 1회 추가합니다.
        /// </summary>
        /// <param name="clip">이벤트를 추가할 애니메이션 클립입니다.</param>
        /// <param name="spriteCount">클립에 포함된 스프라이트(프레임) 수입니다.</param>
        /// <param name="fps">클립 프레임레이트(FPS)입니다.</param>
        private static void AddCompleteEventAtLastFrame(AnimationClip clip, int spriteCount, float fps)
        {
            if (spriteCount <= 0) return;

            // 마지막 프레임 시점 = (spriteCount - 1) / fps
            // (키프레임이 동일 타임에 존재하므로, 이벤트도 동일 시점에 배치)
            float lastFrameTime = (spriteCount - 1) / fps;

            var existing = AnimationUtility.GetAnimationEvents(clip) ?? Array.Empty<AnimationEvent>();
            // 동일 이름 이벤트가 이미 있으면 중복 추가 방지
            if (existing.Any(e => e is { functionName: EventNameComplete }))
                return;

            var eNew = new AnimationEvent
            {
                functionName = EventNameComplete,
                time = lastFrameTime
            };

            var merged = new List<AnimationEvent>(existing.Length + 1);
            merged.AddRange(existing);
            merged.Add(eNew);

            AnimationUtility.SetAnimationEvents(clip, merged.ToArray());
            EditorUtility.SetDirty(clip);
        }

        // -------------------------
        // Prefs Save/Load
        // -------------------------
        /// <summary>
        /// 현재 UI 입력값(폴더, 프리팹명, FPS, 스프라이트 목록 등)을 EditorPrefs에 저장합니다.
        /// </summary>
        private void SavePrefs()
        {
            EditorPrefs.SetInt(KeyTargetType, (int)_targetType);
            EditorPrefs.SetString(KeyPrefabName, _prefabName);
            EditorPrefs.SetFloat(KeyFps, _fps);

            EditorPrefs.SetString(KeyPrefabFolder, GetFolderPath(_prefabOutputFolder));
            EditorPrefs.SetString(KeyAnimFolder, GetFolderPath(_animOutputFolder));

            EditorPrefs.SetInt(KeyStartSort, (int)_start.sortMode);
            EditorPrefs.SetInt(KeyPlaySort,  (int)_play.sortMode);
            EditorPrefs.SetInt(KeyEndSort,   (int)_end.sortMode);

            EditorPrefs.SetString(KeyStartSprites, SerializeObjectIds(_start.sprites));
            EditorPrefs.SetString(KeyPlaySprites,  SerializeObjectIds(_play.sprites));
            EditorPrefs.SetString(KeyEndSprites,   SerializeObjectIds(_end.sprites));
        }

        /// <summary>
        /// EditorPrefs에서 이전 입력값(폴더, 프리팹명, FPS, 스프라이트 목록 등)을 복원합니다.
        /// </summary>
        private void LoadPrefs()
        {
            _targetType = (EffectTargetType)EditorPrefs.GetInt(KeyTargetType, (int)EffectTargetType.SpriteRenderer);
            _prefabName = EditorPrefs.GetString(KeyPrefabName, "Effect_New");
            _fps = EditorPrefs.GetFloat(KeyFps, 12f);

            var prefabFolderPath = EditorPrefs.GetString(KeyPrefabFolder, "Assets");
            var animFolderPath   = EditorPrefs.GetString(KeyAnimFolder, "Assets");

            _prefabOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(prefabFolderPath);
            _animOutputFolder   = AssetDatabase.LoadAssetAtPath<DefaultAsset>(animFolderPath);

            _start.sortMode = (SortMode)EditorPrefs.GetInt(KeyStartSort, (int)SortMode.KeepInputOrder);
            _play.sortMode  = (SortMode)EditorPrefs.GetInt(KeyPlaySort,  (int)SortMode.KeepInputOrder);
            _end.sortMode   = (SortMode)EditorPrefs.GetInt(KeyEndSort,   (int)SortMode.KeepInputOrder);

            _start.sprites.Clear();
            _play.sprites.Clear();
            _end.sprites.Clear();

            _start.sprites.AddRange(DeserializeObjectIds<Sprite>(EditorPrefs.GetString(KeyStartSprites, "")));
            _play.sprites.AddRange(DeserializeObjectIds<Sprite>(EditorPrefs.GetString(KeyPlaySprites, "")));
            _end.sprites.AddRange(DeserializeObjectIds<Sprite>(EditorPrefs.GetString(KeyEndSprites, "")));
        }

        /// <summary>
        /// 폴더 에셋으로부터 유효한 프로젝트 폴더 경로를 얻습니다(유효하지 않으면 Assets로 대체).
        /// </summary>
        /// <param name="folderAsset">폴더로 사용될 DefaultAsset 입니다.</param>
        /// <returns>유효한 폴더 경로(기본값: Assets)입니다.</returns>
        private static string GetFolderPath(DefaultAsset? folderAsset)
        {
            if (folderAsset == null) return "Assets";
            var path = AssetDatabase.GetAssetPath(folderAsset);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
                return "Assets";
            return path;
        }

        /// <summary>
        /// 오브젝트 목록을 GlobalObjectId 문자열로 직렬화하여 EditorPrefs에 저장 가능한 형태로 변환합니다.
        /// </summary>
        /// <typeparam name="T">UnityEngine.Object 파생 타입입니다.</typeparam>
        /// <param name="objs">직렬화할 오브젝트 리스트입니다.</param>
        /// <returns>세미콜론(;) 구분 GlobalObjectId 문자열입니다.</returns>
        /// <remarks>
        /// GlobalObjectId를 사용하면 “멀티 스프라이트(서브에셋)”도 안정적으로 복원됩니다.
        /// </remarks>
        private static string SerializeObjectIds<T>(List<T> objs) where T : UnityEngine.Object
        {
            var ids = new List<string>(objs.Count);
            foreach (var t in objs)
            {
                if (t == null) continue;
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(t);
                ids.Add(gid.ToString());
            }
            return string.Join(";", ids);
        }

        /// <summary>
        /// 직렬화된 GlobalObjectId 문자열을 역직렬화하여 오브젝트 목록으로 복원합니다.
        /// </summary>
        /// <typeparam name="T">UnityEngine.Object 파생 타입입니다.</typeparam>
        /// <param name="data">세미콜론(;) 구분 GlobalObjectId 문자열입니다.</param>
        /// <returns>복원된 오브젝트 리스트입니다.</returns>
        private static List<T> DeserializeObjectIds<T>(string data) where T : UnityEngine.Object
        {
            var list = new List<T>(32);
            if (string.IsNullOrWhiteSpace(data))
                return list;

            var parts = data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (!GlobalObjectId.TryParse(p, out var gid))
                    continue;

                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as T;
                if (obj != null)
                    list.Add(obj);
            }
            return list;
        }
    }
}
#endif
