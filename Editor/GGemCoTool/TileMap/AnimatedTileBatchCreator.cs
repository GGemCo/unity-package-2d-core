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
    internal class AnimatedTileBatchCreator : EditorWindow
    {
        private class SpriteInfo
        {
            public string assetPath;
            public readonly Sprite sprite;
            public readonly string baseName;
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
        private const string WindowTitle = "Animated Tile Batch Creator";
        private const string PrefsKey = "GGemCo_AnimTileBatch_";
        private static readonly Regex NameRegex =
            new(@"^(?<name>.+)_(?<row>\d+)_(?<col>\d+)_(?<frame>\d+)$", RegexOptions.Compiled);

        // Defaults: 3x4(Cols x Rows)
        private const int DefaultExpectedCols = 3;
        private const int DefaultExpectedRows = 4;

        // -------- UI State --------
        [SerializeField] private Sprite inputSprite;          // ⬅ 아틀라스/폴더 → Sprite(서브 스프라이트)로 변경
        [SerializeField] private DefaultAsset outputFolder;   // 생성 .asset 저장 위치
        [SerializeField] private int expectedCols = DefaultExpectedCols;
        [SerializeField] private int expectedRows = DefaultExpectedRows;

        [SerializeField] private bool strictLayoutCheck = true;
        [SerializeField] private bool detectFrameBase = true; // auto-detect 0- or 1-based frame
        [SerializeField] private int manualFrameBase = 0;     // used when detectFrameBase=false

        [SerializeField] private float minSpeed = 1f;
        [SerializeField] private float maxSpeed = 1f;
        [SerializeField] private Tile.ColliderType colliderType = Tile.ColliderType.None;

        [SerializeField] private bool overwriteIfExists = false;

        private Vector2 _scroll;

        // Cache / Preview
        private readonly List<SpriteInfo> _loadedSprites = new();
        private readonly Dictionary<GroupKey, List<SpriteFrame>> _grouped = new();
        private string _previewSummary = "";

        // Reflection (Extras availability check)
        private static Type _animatedTileType;

        // ----- Data structures -----
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
        [MenuItem("Tools/GGemCo/Animated Tile Batch Creator")]
        public static void ShowWindow()
        {
            var win = GetWindow<AnimatedTileBatchCreator>(utility: false, title: WindowTitle);
            win.minSize = new Vector2(540, 440);
            win.LoadPrefs();
            win.TryAutoAssignFromSelection();
            win.Focus();
        }

        // Project window context menu (Sprite)
        [MenuItem("Assets/Create/2D/Tiles/Animated Tiles (from Sliced Sprite)", true)]
        private static bool ValidateContextCreate()
        {
            return Selection.objects.OfType<Sprite>().Any();
        }

        [MenuItem("Assets/Create/2D/Tiles/Animated Tiles (from Sliced Sprite)")]
        private static void ContextCreate()
        {
            var selected = Selection.objects.OfType<Sprite>().FirstOrDefault();
            if (selected == null) return;

            var win = GetWindow<AnimatedTileBatchCreator>(utility: false, title: WindowTitle);
            win.minSize = new Vector2(540, 440);
            win.LoadPrefs();
            win.inputSprite = selected;

            // default output to same folder as the texture asset
            var texPath = AssetDatabase.GetAssetPath(selected);
            var folder = Path.GetDirectoryName(texPath)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
            {
                win.outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            }
            win.Focus();
        }

        private void OnEnable()
        {
            // Find AnimatedTile type via reflection (to handle missing Extras gracefully)
            _animatedTileType = Type.GetType("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras");
        }

        private void OnGUI()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            DrawHeader();
            DrawConfig();
            DrawActions();
            DrawPreview();
        }

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

        private void DrawConfig()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);

            inputSprite = (Sprite)EditorGUILayout.ObjectField(
                new GUIContent("Input Sprite", "슬라이스된 스프라이트 시트의 서브 스프라이트 중 하나를 지정"),
                inputSprite, typeof(Sprite), false);

            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                new GUIContent("Output Folder", "생성될 Animated Tile .asset 저장 위치"),
                outputFolder, typeof(DefaultAsset), false);

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

            overwriteIfExists = EditorGUILayout.Toggle(
                new GUIContent("Overwrite If Exists", "동일 경로의 자산이 있으면 덮어쓰기"),
                overwriteIfExists);
        }

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

        private void DrawPreview()
        {
            EditorGUILayout.Space(10);
            if (!string.IsNullOrEmpty(_previewSummary))
            {
                EditorGUILayout.LabelField("Preview / Log", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_previewSummary, MessageType.None);
            }
        }

        private void DoDryRun()
        {
            if (!ValidateInputs(out var spritePath, out var outPath)) return;

            LoadSpritesFromSlicedTexture(inputSprite);
            GroupSprites();
            _previewSummary = BuildSummary(includeFrames: true);
            Repaint();
        }

        private void DoCreate()
        {
            if (!ValidateInputs(out var spritePath, out var outPath)) return;

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
                foreach (var (key, frames) in _grouped)
                {
                    frames.Sort();

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

                    var spritesArray = frames.Select(f => f.Sprite).ToArray();
                    fSprites.SetValue(tile, spritesArray);
                    fMin.SetValue(tile, minSpeed);
                    fMax.SetValue(tile, Mathf.Max(minSpeed, maxSpeed));
                    fCol.SetValue(tile, colliderType);

                    // Build path: <out>/<name>/AnimatedTile_<name>_rX_cY.asset
                    var folderForName = EnsureFolder(Path.Combine(outPath, key.Name));
                    var assetName = $"AnimatedTile_{key.Name}_r{key.Row}_c{key.Col}.asset";
                    var assetPath = Path.Combine(folderForName, assetName).Replace("\\", "/");

                    var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (existing != null)
                    {
                        if (!overwriteIfExists)
                        {
                            skipped++;
                            continue;
                        }
                        AssetDatabase.DeleteAsset(assetPath);
                        overwritten++;
                    }

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

            EditorGUIUtility.PingObject(outputFolder);
            Debug.Log($"[AnimTileBatch] 완료 — Created:{created}, Overwritten:{overwritten}, Skipped:{skipped}, Errors:{errors}");
        }

        // -------- Helpers --------

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
                {
                    outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
                }
            }
        }

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
        /// 선택된 서브 스프라이트가 속한 원본 텍스처 경로에서 모든 서브 스프라이트(Sprite) 수집
        /// </summary>
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
            {
                // Unity 버전에 따라 본체 포함 LoadAllAssetsAtPath가 더 잘 동작할 수 있음
                reps = AssetDatabase.LoadAllAssetsAtPath(textureAssetPath);
            }

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

        private void GroupSprites()
        {
            _grouped.Clear();

            if (_loadedSprites.Count == 0) return;

            // Determine frame base if requested
            int frameBase = manualFrameBase;
            if (detectFrameBase)
            {
                int minFrame = _loadedSprites.Min(s => s.frame);
                frameBase = (minFrame == 0) ? 0 : 1;
            }

            foreach (var s in _loadedSprites)
            {
                // Layout validation
                if (strictLayoutCheck)
                {
                    if (s.col < 0 || s.col >= expectedCols || s.row < 0 || s.row >= expectedRows)
                    {
                        Debug.LogError($"[AnimTileBatch] Out-of-range (row,col): {s.baseName} ({s.row},{s.col}) → 예상 ({expectedRows} rows, {expectedCols} cols)");
                        continue;
                    }
                }

                var key = new GroupKey(s.baseName, s.row, s.col);
                if (!_grouped.TryGetValue(key, out var list))
                {
                    list = new List<SpriteFrame>();
                    _grouped[key] = list;
                }

                int normalizedFrame = s.frame - frameBase;
                list.Add(new SpriteFrame(s.sprite, normalizedFrame));
            }
        }

        private string BuildSummary(bool includeFrames)
        {
            if (_grouped.Count == 0) return "No valid sprites found on the sliced texture with naming rule 'name_row_col_frame'.";

            var lines = new List<string>
            {
                $"Groups: {_grouped.Count} (name,row,col), Expected Layout: {expectedCols}x{expectedRows}, AutoFrameBase: {detectFrameBase}"
            };

            var byName = _grouped.GroupBy(kv => kv.Key.Name).OrderBy(g => g.Key);
            foreach (var g in byName)
            {
                lines.Add($"- {g.Key}");
                foreach (var (key, frames) in g.OrderBy(kv => kv.Key.Row).ThenBy(kv => kv.Key.Col))
                {
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
                else
                {
                    if (folderAbsOrProjectPath.Contains("/Assets/"))
                    {
                        projRel = folderAbsOrProjectPath.Substring(folderAbsOrProjectPath.IndexOf("Assets/", StringComparison.Ordinal));
                    }
                }
            }

            var segments = projRel.Replace("\\", "/").Split('/');
            string path = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{path}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(path, segments[i]);
                }
                path = next;
            }

            return path;
        }

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
        }

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
        }
    }
}
#endif
