// Assets/Editor/GGemCo/AnimatedTileBatchCreator.cs
// Unity 6 / 2D Tilemap Extras (AnimatedTile) 필요
// Naming rule: name_row_col_frame (e.g., "water_0_2_3")
// - row, col, frame: int (0/1 시작 허용; 자동 보정)
// - 각 (name, row, col) 조합마다 Animated Tile 1개 생성, 프레임은 frame 오름차순으로 구성
// - 입력은 "슬라이스된 Sprite 시트의 서브 스프라이트 중 하나(Sprite)"를 지정
//   → 해당 Sprite가 속한 원본 텍스처의 모든 서브 스프라이트를 수집하여 처리

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 슬라이스된 스프라이트 이름 규칙(<c>name_row_col_frame</c>)을 기반으로
    /// 2D Tilemap Extras의 AnimatedTile 자산을 일괄 생성하는 EditorWindow.
    /// <para>
    /// - 한 (name,row,col) 그룹 = AnimatedTile 1개<br/>
    /// - 프레임은 frame 오름차순으로 세팅<br/>
    /// - <c>baseNameOverride</c> 지정 시, 정규식에서 추출된 <c>name</c> 대신 출력용 베이스 이름으로 사용
    /// </para>
    /// </summary>
    internal class AnimatedTileBatchCreator : EditorWindow
    {
        /// <summary>내부 미리보기/그룹핑에 사용하는 스프라이트 컨테이너.</summary>
        private class SpriteInfo
        {
            public string assetPath;
            public readonly Sprite sprite;
            public readonly string baseName; // 원본 이름(정규식 추출)
            public readonly int row;
            public readonly int col;
            public readonly int frame;

            public SpriteInfo(string assetPath, Sprite sprite, string baseName, int row, int col, int frame)
            {
                this.assetPath = assetPath;
                this.sprite = sprite;
                this.baseName = baseName;
                this.row = row;
                this.col = col;
                this.frame = frame;
            }
        }

        // -------- Constants / Defaults --------
        private const string WindowTitle = "애니메이션 타일 일괄생성기";
        private const string PrefsKey = "GGemCo_AnimTileBatch_";
        private static readonly Regex NameRegex =
            new(@"^(?<name>.+)_(?<row>\d+)_(?<col>\d+)_(?<frame>\d+)$", RegexOptions.Compiled);

        // Defaults: 3x4(Cols x Rows)
        private const int DefaultExpectedCols = 3;
        private const int DefaultExpectedRows = 4;

        // -------- UI State --------
        [SerializeField] private Sprite inputSprite;          // ⬅ 아틀라스/폴더 → Sprite(서브 스프라이트)로 변경
        [SerializeField] private DefaultAsset outputFolder;   // 생성 .asset 저장 위치

        [Tooltip("비워두면 이름 규칙의 name을 그대로 사용합니다. 지정 시 모든 그룹의 출력 베이스 이름을 강제합니다.")]
        [SerializeField] private string baseNameOverride = "";   // 출력용 베이스 이름 강제
        [SerializeField] private bool overwriteIfExists = false;
        
        [Header("Layout")]
        [SerializeField] private int expectedCols = DefaultExpectedCols;
        [SerializeField] private int expectedRows = DefaultExpectedRows;

        [SerializeField] private bool strictLayoutCheck = true;
        [SerializeField] private bool detectFrameBase = true; // auto-detect 0- or 1-based frame
        [SerializeField] private int manualFrameBase = 0;     // used when detectFrameBase=false

        [Header("Animation")]
        [SerializeField] private float minSpeed = 1f;
        [SerializeField] private float maxSpeed = 1f;
        [SerializeField] private Tile.ColliderType colliderType = Tile.ColliderType.None;

        private Vector2 _scroll;
        private Vector2 _scrollTextAreaPos;

        // Cache / Preview
        private readonly List<SpriteInfo> _loadedSprites = new();
        private readonly Dictionary<GroupKey, List<SpriteFrame>> _grouped = new();
        private string _previewSummary = "";

        // Reflection (Extras availability check)
        private static Type _animatedTileType;

        /// <summary>그룹 키: 동일 (name,row,col) 묶음.</summary>
        private readonly struct GroupKey : IEquatable<GroupKey>
        {
            public readonly string Name;
            public readonly int Row;
            public readonly int Col;

            public GroupKey(string name, int row, int col)
            {
                Name = name;
                Row = row;
                Col = col;
            }

            public bool Equals(GroupKey other) => Row == other.Row && Col == other.Col && Name == other.Name;
            public override bool Equals(object obj) => obj is GroupKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Name, Row, Col);
            public override string ToString() => $"{Name}_r{Row}_c{Col}";
        }

        /// <summary>프레임 비교/정렬용 구조체.</summary>
        private readonly struct SpriteFrame : IComparable<SpriteFrame>
        {
            public readonly Sprite Sprite;
            public readonly int Frame;

            public SpriteFrame(Sprite sprite, int frame)
            {
                Sprite = sprite;
                Frame = frame;
            }

            public int CompareTo(SpriteFrame other) => Frame.CompareTo(other.Frame);
        }

        // -------- Menu --------

        /// <summary>
        /// 툴 창 열기 (메뉴).
        /// </summary>
        [MenuItem(ConfigEditor.NameToolAnimatedTileBatchCreator, false, (int)ConfigEditor.ToolOrdering.AnimatedTileBatchCreator)]
        public static void ShowWindow()
        {
            var win = GetWindow<AnimatedTileBatchCreator>(utility: false, title: WindowTitle);
            win.minSize = new Vector2(540, 440);
            win.LoadPrefs();
            win.TryAutoAssignFromSelection();
            win.Focus();
        }

        /// <summary>
        /// Project 뷰 컨텍스트 메뉴 유효성 검사 (Sprite 선택 시에만 활성).
        /// </summary>
        private static bool ValidateContextCreate() => Selection.objects.OfType<Sprite>().Any();

        /// <summary>
        /// Project 뷰 컨텍스트 메뉴 항목: 선택한 Sprite로 창을 초기화하고 표시.
        /// </summary>
        [MenuItem("Assets/Create/2D/Tiles/Animated Tiles (from Sliced Sprite)")]
        private static void ContextCreate()
        {
            var selected = Selection.objects.OfType<Sprite>().FirstOrDefault();
            if (selected == null) return;

            var win = GetWindow<AnimatedTileBatchCreator>(utility: false, title: WindowTitle);
            win.minSize = new Vector2(540, 440);
            win.LoadPrefs();
            win.inputSprite = selected;

            // 기본 출력 폴더를 입력 스프라이트가 속한 경로로 세팅
            var texPath = AssetDatabase.GetAssetPath(selected);
            var folder = Path.GetDirectoryName(texPath)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                win.outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);

            win.Focus();
        }

        /// <summary>
        /// 윈도우 활성화 시 2D Tilemap Extras의 AnimatedTile 타입을 리플렉션으로 조회.
        /// </summary>
        private void OnEnable()
        {
            _animatedTileType = Type.GetType("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras");
        }

        /// <summary>
        /// 에디터 윈도우 GUI 루프.
        /// </summary>
        private void OnGUI()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            DrawHeader();
            DrawConfig();
            DrawActions();
            DrawPreview();
            EditorGUILayout.Space(20);
        }

        /// <summary>
        /// 헤더/도움말 영역을 렌더링.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Animated Tile Batch Creator", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Naming: name_row_col_frame (e.g., water_0_2_3)", EditorStyles.miniLabel);

            if (_animatedTileType == null)
            {
                EditorGUILayout.HelpBox(
                    "2D Tilemap Extras(AnimatedTile)를 찾을 수 없습니다.\n" +
                    "- Package Manager에서 '2D Tilemap Extras' 설치 필요\n" +
                    "- 설치 후 재컴파일/재시도 해주세요.",
                    MessageType.Error
                );
            }
        }

        /// <summary>
        /// 입력·레이아웃·애니메이션·출력 설정 UI를 렌더링.
        /// </summary>
        private void DrawConfig()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);

            inputSprite = (Sprite)EditorGUILayout.ObjectField(
                new GUIContent("Input Sprite", "슬라이스된 스프라이트 시트의 서브 스프라이트 중 하나를 지정"),
                inputSprite, typeof(Sprite), false);

            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                new GUIContent("Output Folder", "생성될 Animated Tile .asset 저장 위치"),
                outputFolder, typeof(DefaultAsset), false);
            baseNameOverride = EditorGUILayout.TextField(
                new GUIContent("Base Name Override", "비워두면 정규식의 name을 사용, 지정 시 출력 베이스 이름 강제"),
                baseNameOverride);
            overwriteIfExists = EditorGUILayout.Toggle(
                new GUIContent("Overwrite If Exists", "동일 경로의 자산이 있으면 덮어쓰기"),
                overwriteIfExists);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Layout / Validation", EditorStyles.boldLabel);
            expectedCols = EditorGUILayout.IntField(new GUIContent("Expected Cols", "예상 가로 칸수 (기본 3)"), expectedCols);
            expectedRows = EditorGUILayout.IntField(new GUIContent("Expected Rows", "예상 세로 칸수 (기본 4)"), expectedRows);
            strictLayoutCheck = EditorGUILayout.Toggle(new GUIContent("Strict Layout Check",
                    "발견된 (row, col)이 기대 범위를 벗어나면 에러 처리"),
                strictLayoutCheck);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            minSpeed = EditorGUILayout.FloatField(new GUIContent("Min Speed", "AnimatedTile.m_MinSpeed"), minSpeed);
            maxSpeed = EditorGUILayout.FloatField(new GUIContent("Max Speed", "AnimatedTile.m_MaxSpeed"), maxSpeed);
            colliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup(
                new GUIContent("Collider Type", "충돌이 필요 없다면 None 권장"),
                colliderType);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Frame Indexing", EditorStyles.boldLabel);
            detectFrameBase = EditorGUILayout.Toggle(
                new GUIContent("Auto-detect Frame Base", "프레임 시작 인덱스(0/1)를 자동 감지"),
                detectFrameBase);
            using (new EditorGUI.DisabledScope(detectFrameBase))
            {
                manualFrameBase = EditorGUILayout.IntPopup(
                    new GUIContent("Manual Frame Base", "자동 감지 끄면 사용 (보통 0 또는 1)"),
                    manualFrameBase, new[] { new GUIContent("0"), new GUIContent("1") }, new[] { 0, 1 });
            }
        }

        /// <summary>
        /// 실행 버튼(Dry Run / Create)을 렌더링.
        /// </summary>
        private void DrawActions()
        {
            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(_animatedTileType == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Dry Run", "미리보기 (그룹/프레임 구성 확인)")))
                {
                    SavePrefs();
                    DoDryRun();
                }

                if (GUILayout.Button(new GUIContent("Create", "Animated Tile 자산 생성")))
                {
                    SavePrefs();
                    if (EditorUtility.DisplayDialog("Create Animated Tiles",
                            "선택한 Sprite가 속한 텍스처의 모든 서브 스프라이트에서 Animated Tile을 생성합니다.\n진행하시겠습니까?",
                            "Create", "Cancel"))
                    {
                        DoCreate();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 미리보기/로그 영역을 렌더링.
        /// </summary>
        private void DrawPreview()
        {
            EditorGUILayout.Space(10);
            if (!string.IsNullOrEmpty(_previewSummary))
            {
                EditorGUILayout.LabelField("Preview / Log", EditorStyles.boldLabel);
                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = false };
                _scrollTextAreaPos = EditorGUILayout.BeginScrollView(_scrollTextAreaPos, GUILayout.Height(200));
                _previewSummary = EditorGUILayout.TextArea(_previewSummary, style, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// Dry Run: 입력 검증 → 스프라이트 로드/그룹핑 → 요약 출력.
        /// </summary>
        private void DoDryRun()
        {
            if (!ValidateInputs(out _, out _)) return;

            LoadSpritesFromSlicedTexture(inputSprite);
            GroupSprites();
            _previewSummary = BuildSummary(includeFrames: true);
            Repaint();
        }

        /// <summary>
        /// 실제 AnimatedTile 자산을 생성합니다.
        /// 폴더 생성은 “실제 생성이 확정된 시점(신규 생성 또는 덮어쓰기 직전)”에만 수행하여 빈 폴더 생성을 방지합니다.
        /// </summary>
        private void DoCreate()
        {
            if (!ValidateInputs(out _, out var outPath)) return;

            LoadSpritesFromSlicedTexture(inputSprite);
            GroupSprites();

            if (_grouped.Count == 0)
            {
                EditorUtility.DisplayDialog(WindowTitle, "만들 그룹이 없습니다. Dry Run으로 입력 스프라이트와 이름 규칙을 확인하세요.", "OK");
                return;
            }

            int created = 0, skipped = 0, overwritten = 0, errors = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var kv in _grouped.OrderBy(x => x.Key.Name).ThenBy(x => x.Key.Row).ThenBy(x => x.Key.Col))
                {
                    var key = kv.Key;
                    var frames = kv.Value;
                    frames.Sort();

                    if (frames.Count == 0)
                    {
                        skipped++;
                        continue;
                    }

                    // 에셋 경로(폴더/파일명) 계산 — baseNameOverride가 있으면 그것을 사용
                    // string folderForName = Path.Combine(outPath, key.Name).Replace("\\", "/");
                    string folderForName = outPath.Replace("\\", "/");
                    string assetName = $"{key.Name}_{key.Row}_{key.Col}.asset";
                    string assetPath = Path.Combine(folderForName, assetName).Replace("\\", "/");

                    // 기존 에셋 존재 여부 확인
                    var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (existing != null && !overwriteIfExists)
                    {
                        // 덮어쓰기 옵션이 꺼져 있으면 스킵 (⛔ 폴더 생성 안 함)
                        skipped++;
                        Debug.Log($"같은 이름의 애니메이션 타일이 있어 건너띄었습니다. assetPath: {assetPath}");
                        continue;
                    }

                    // 여기서부터 “실제 생성 확정” → 폴더를 지연 생성(빈 폴더 방지)
                    folderForName = EnsureFolder(folderForName);

                    // 기존 에셋 삭제(덮어쓰기)
                    if (existing != null && overwriteIfExists)
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                        overwritten++;
                    }

                    // AnimatedTile 인스턴스 생성 (리플렉션)
                    var tile = ScriptableObject.CreateInstance(_animatedTileType);
                    var fSprites = _animatedTileType.GetField("m_AnimatedSprites");
                    var fMin = _animatedTileType.GetField("m_MinSpeed");
                    var fMax = _animatedTileType.GetField("m_MaxSpeed");
                    var fCol = _animatedTileType.GetField("m_TileColliderType");

                    if (fSprites == null || fMin == null || fMax == null || fCol == null)
                    {
                        errors++;
                        Debug.LogError("[AnimTileBatch] AnimatedTile 필드를 찾지 못했습니다. 패키지 버전을 확인하세요.");
                        UnityEngine.Object.DestroyImmediate(tile);
                        continue;
                    }

                    // 필드 주입
                    var spritesArray = frames.Select(f => f.Sprite).ToArray();
                    fSprites.SetValue(tile, spritesArray);
                    fMin.SetValue(tile, minSpeed);
                    fMax.SetValue(tile, Mathf.Max(minSpeed, maxSpeed));
                    fCol.SetValue(tile, colliderType);

                    // 에셋 생성
                    AssetDatabase.CreateAsset((UnityEngine.Object)tile, assetPath);
                    created++;
                }
            }
            catch (Exception ex)
            {
                errors++;
                Debug.LogException(ex);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            _previewSummary =
                $"Created: {created}, Overwritten: {overwritten}, Skipped: {skipped}, Errors: {errors}\n\n" +
                BuildSummary(includeFrames: false);
            Repaint();

            if (outputFolder != null) EditorGUIUtility.PingObject(outputFolder);
            Debug.Log($"[AnimTileBatch] 완료 — Created:{created}, Overwritten:{overwritten}, Skipped:{skipped}, Errors:{errors}");
        }

        // -------- Helpers --------

        /// <summary>
        /// 현재 선택으로 입력/출력 기본값을 유추하여 채웁니다.
        /// </summary>
        private void TryAutoAssignFromSelection()
        {
            if (inputSprite == null)
            {
                var selSprite = Selection.objects.OfType<Sprite>().FirstOrDefault();
                if (selSprite != null) inputSprite = selSprite;
            }
            if (outputFolder == null && inputSprite != null)
            {
                var path = AssetDatabase.GetAssetPath(inputSprite);
                var folder = Path.GetDirectoryName(path)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                    outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            }
        }

        /// <summary>
        /// 필수 입력값(입력 스프라이트, 출력 폴더, 레이아웃 수치)을 검증합니다.
        /// </summary>
        private bool ValidateInputs(out string spritePath, out string outputPath)
        {
            spritePath = outputPath = null;

            if (inputSprite == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Input Sprite를 지정하세요.", "OK");
                return false;
            }
            spritePath = AssetDatabase.GetAssetPath(inputSprite);
            if (string.IsNullOrEmpty(spritePath))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Input Sprite 경로가 유효하지 않습니다.", "OK");
                return false;
            }

            if (outputFolder == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Output Folder를 지정하세요.", "OK");
                return false;
            }
            outputPath = AssetDatabase.GetAssetPath(outputFolder);
            if (!AssetDatabase.IsValidFolder(outputPath))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Output Folder 경로가 유효하지 않습니다.", "OK");
                return false;
            }

            if (expectedCols <= 0 || expectedRows <= 0)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Expected Rows/Cols가 1 이상이어야 합니다.", "OK");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 선택된 서브 스프라이트가 속한 원본 텍스처 경로에서 모든 서브 스프라이트(Sprite) 수집.
        /// </summary>
        /// <param name="anySubSprite">슬라이스된 시트의 서브 스프라이트 중 하나</param>
        private void LoadSpritesFromSlicedTexture(Sprite anySubSprite)
        {
            _loadedSprites.Clear();
            if (!anySubSprite) return;

            // 이 경로는 텍스처 자산(.png 등)을 가리킨다. (서브 스프라이트도 동일 경로)
            string textureAssetPath = AssetDatabase.GetAssetPath(anySubSprite);
            if (string.IsNullOrEmpty(textureAssetPath)) return;

            // 텍스처의 모든 서브 에셋 중 Sprite만 수집
            var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(textureAssetPath);
            if (reps == null || reps.Length == 0)
                reps = AssetDatabase.LoadAllAssetsAtPath(textureAssetPath);

            foreach (var obj in reps)
            {
                if (obj is not Sprite s) continue;
                var name = s.name; // e.g., "water_0_2_3"
                var m = NameRegex.Match(name);
                if (!m.Success) continue;

                var baseName = m.Groups["name"].Value;
                int row = int.Parse(m.Groups["row"].Value);
                int col = int.Parse(m.Groups["col"].Value);
                int frame = int.Parse(m.Groups["frame"].Value);

                _loadedSprites.Add(new SpriteInfo(textureAssetPath, s, baseName, row, col, frame));
            }
        }

        /// <summary>
        /// 로드된 스프라이트를 (name,row,col) 단위로 그룹핑하고 프레임 인덱스를 정규화합니다.
        /// <para>
        /// <c>baseNameOverride</c> 가 지정된 경우 그룹 이름은 모두 그 값으로 통일됩니다.
        /// </para>
        /// </summary>
        private void GroupSprites()
        {
            _grouped.Clear();
            if (_loadedSprites.Count == 0) return;

            // 프레임 베이스 자동 감지/수동 설정
            int frameBase = manualFrameBase;
            if (detectFrameBase)
            {
                int minFrame = _loadedSprites.Min(s => s.frame);
                frameBase = (minFrame == 0) ? 0 : 1;
            }

            foreach (var s in _loadedSprites)
            {
                // 레이아웃 범위 검증
                if (strictLayoutCheck)
                {
                    if (s.col < 0 || s.col >= expectedCols || s.row < 0 || s.row >= expectedRows)
                    {
                        Debug.LogError($"[AnimTileBatch] Out-of-range (row,col): {s.baseName} ({s.row},{s.col}) → 예상 ({expectedRows} rows, {expectedCols} cols)");
                        continue;
                    }
                }

                // ✅ baseNameOverride 적용 (없으면 원래 name 사용)
                string finalBaseName = string.IsNullOrEmpty(baseNameOverride) ? s.baseName : baseNameOverride;

                var key = new GroupKey(finalBaseName, s.row, s.col);
                if (!_grouped.TryGetValue(key, out var list))
                {
                    list = new List<SpriteFrame>();
                    _grouped[key] = list;
                }

                int normalizedFrame = s.frame - frameBase;
                list.Add(new SpriteFrame(s.sprite, normalizedFrame));
            }
        }

        /// <summary>
        /// 현재 그룹핑된 결과를 요약 문자열로 생성합니다.
        /// </summary>
        /// <param name="includeFrames">프레임 인덱스 목록을 함께 출력할지 여부</param>
        private string BuildSummary(bool includeFrames)
        {
            if (_grouped.Count == 0)
                return "No valid sprites found on the sliced texture with naming rule 'name_row_col_frame'.";

            var lines = new List<string>
            {
                $"Groups: {_grouped.Count} (name,row,col), Expected Layout: {expectedCols}x{expectedRows}, AutoFrameBase: {detectFrameBase}"
            };

            var byName = _grouped.GroupBy(kv => kv.Key.Name).OrderBy(g => g.Key);
            foreach (var g in byName)
            {
                lines.Add($"- {g.Key}");
                foreach (var kv in g.OrderBy(kv => kv.Key.Row).ThenBy(kv => kv.Key.Col))
                {
                    var key = kv.Key;
                    var frames = kv.Value;
                    if (includeFrames)
                    {
                        var fStr = string.Join(",", frames.Select(f => f.Frame).Distinct().OrderBy(x => x));
                        lines.Add($"    r{key.Row} c{key.Col} → {frames.Count} frames [{fStr}]");
                    }
                    else
                    {
                        lines.Add($"    r{key.Row} c{key.Col} → {frames.Count} frames");
                    }
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// 주어진 경로의 폴더를 보장합니다(필요 시 단계별 생성). Project상 경로만 반환합니다.
        /// </summary>
        /// <param name="folderAbsOrProjectPath">"Assets/..." 또는 절대 경로</param>
        /// <returns>Project 상대 경로("Assets/...")</returns>
        private static string EnsureFolder(string folderAbsOrProjectPath)
        {
            string projRel = folderAbsOrProjectPath;
            if (!folderAbsOrProjectPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                var dataPath = Application.dataPath.Replace("\\", "/");
                if (folderAbsOrProjectPath.StartsWith(dataPath, StringComparison.Ordinal))
                {
                    projRel = "Assets" + folderAbsOrProjectPath.Substring(dataPath.Length);
                }
                else if (folderAbsOrProjectPath.Contains("/Assets/"))
                {
                    projRel = folderAbsOrProjectPath.Substring(folderAbsOrProjectPath.IndexOf("Assets/", StringComparison.Ordinal));
                }
            }

            var segments = projRel.Replace("\\", "/").Split('/');
            string path = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{path}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(path, segments[i]);
                path = next;
            }

            return path;
        }

        /// <summary>
        /// 에디터 환경설정(EditorPrefs)에 현재 설정을 저장합니다.
        /// </summary>
        private void SavePrefs()
        {
            EditorPrefs.SetBool(PrefsKey + "strictLayoutCheck", strictLayoutCheck);
            EditorPrefs.SetBool(PrefsKey + "detectFrameBase", detectFrameBase);
            EditorPrefs.SetFloat(PrefsKey + "minSpeed", minSpeed);
            EditorPrefs.SetFloat(PrefsKey + "maxSpeed", maxSpeed);
            EditorPrefs.SetInt(PrefsKey + "expectedCols", expectedCols);
            EditorPrefs.SetInt(PrefsKey + "expectedRows", expectedRows);
            EditorPrefs.SetInt(PrefsKey + "manualFrameBase", manualFrameBase);
            EditorPrefs.SetInt(PrefsKey + "colliderType", (int)colliderType);
            EditorPrefs.SetBool(PrefsKey + "overwriteIfExists", overwriteIfExists);
            EditorPrefs.SetString(PrefsKey + "baseNameOverride", baseNameOverride);
        }

        /// <summary>
        /// 에디터 환경설정(EditorPrefs)에서 설정을 불러옵니다.
        /// </summary>
        private void LoadPrefs()
        {
            strictLayoutCheck = EditorPrefs.GetBool(PrefsKey + "strictLayoutCheck", true);
            detectFrameBase = EditorPrefs.GetBool(PrefsKey + "detectFrameBase", true);
            minSpeed = EditorPrefs.GetFloat(PrefsKey + "minSpeed", 1f);
            maxSpeed = EditorPrefs.GetFloat(PrefsKey + "maxSpeed", 1f);
            expectedCols = EditorPrefs.GetInt(PrefsKey + "expectedCols", DefaultExpectedCols);
            expectedRows = EditorPrefs.GetInt(PrefsKey + "expectedRows", DefaultExpectedRows);
            manualFrameBase = EditorPrefs.GetInt(PrefsKey + "manualFrameBase", 0);
            colliderType = (Tile.ColliderType)EditorPrefs.GetInt(PrefsKey + "colliderType", (int)Tile.ColliderType.None);
            overwriteIfExists = EditorPrefs.GetBool(PrefsKey + "overwriteIfExists", false);
            baseNameOverride = EditorPrefs.GetString(PrefsKey + "baseNameOverride", "");
        }
    }
}
#endif
