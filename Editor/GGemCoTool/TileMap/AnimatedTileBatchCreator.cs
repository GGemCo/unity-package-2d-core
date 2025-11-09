#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 슬라이스된 스프라이트(Texture2D)의 순서를 바탕으로
    /// (name, row, col) 단위의 AnimatedTile 에셋을 일괄 생성하는 에디터 툴.
    /// - 한 Texture2D만 대상으로 한다.
    /// - 행(row)마다 시작할 X 인덱스를 달리 줄 수 있도록 frameStartIndex를 지원한다.
    /// </summary>
    internal class AnimatedTileBatchCreator : EditorWindow
    {
        private const string Title = "애니메이션 타일 일괄생성기";
        private const string Prefs = "GGemCo_Slice2AnimTile_";
        private const string PrefsFrameStartIndex = Prefs + "frameStartIndex";

        // 대상 텍스처
        private Texture2D _targetTexture;

        // 출력 설정
        [Header("AnimatedTile Output")]
        private DefaultAsset _outputFolder;
        private string _animatedTileBaseNameOverride = "";
        private bool _overwriteIfExists;

        // 레이아웃/검증
        [Header("Layout / Validation")]
        private int _framesAcross;            // 한 줄에 배치된 프레임 수
        private List<int> _frameStartIndex = new(); // 각 row마다 몇 번째 스프라이트부터 애니메이션으로 볼지
        private int _expectedCols = 3;            // AnimatedTile 1개를 만들 때 모을 컬럼 수
        private int _expectedRows = 4;            // 전체 Texture를 몇 행까지 읽을지
        private bool _strictLayoutCheck = true;

        // 애니메이션 옵션
        [Header("Animation")]
        private float _minSpeed = 1f;
        private float _maxSpeed = 1f;
        private Tile.ColliderType _colliderType = Tile.ColliderType.None;

        // 로그 출력
        private Vector2 _scroll;
        private Vector2 _logScroll;
        private string _log = "";

        // AnimatedTile 타입 캐시
        private static Type _animatedTileType;

        /// <summary>
        /// 메뉴에서 창을 연다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolAnimatedTileBatchCreator, false, (int)ConfigEditor.ToolOrdering.AnimatedTileBatchCreator)]
        public static void Open()
        {
            var win = GetWindow<AnimatedTileBatchCreator>(Title);
            win.LoadPrefs();
            win.Show();
        }

        /// <summary>
        /// 윈도우가 활성화될 때 AnimatedTile 타입을 캐시한다.
        /// </summary>
        private void OnEnable()
        {
            _animatedTileType = Type.GetType("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras");
        }

        /// <summary>
        /// 에디터 윈도우 GUI 렌더링 루프.
        /// </summary>
        private void OnGUI()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            EditorGUILayout.LabelField("Sprite Slice → AnimatedTile 생성기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "슬라이스된 Texture2D를 순서대로 읽어서 (name,row,col) 단위로 AnimatedTile을 생성합니다.\n" +
                "각 행(row)마다 시작할 인덱스를 따로 지정할 수 있습니다.\n" +
                "※ 2D Tilemap Extras 패키지가 필요합니다.",
                MessageType.Info);

            DrawTargetsGUI();
            DrawAnimatedTileGUI();
            DrawActionGUI();
            DrawLogGUI();
        }

        /// <summary>
        /// 대상 Texture2D를 선택하는 GUI를 그린다.
        /// </summary>
        private void DrawTargetsGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("1. 대상 Texture2D", EditorStyles.boldLabel);
            _targetTexture = (Texture2D)EditorGUILayout.ObjectField(GUIContent.none, _targetTexture, typeof(Texture2D), false);
        }

        /// <summary>
        /// AnimatedTile 생성에 필요한 옵션 GUI를 그린다.
        /// </summary>
        private void DrawAnimatedTileGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("2. AnimatedTile 생성 설정", EditorStyles.boldLabel);

            // 출력 경로
            _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                new GUIContent("Output Folder"), _outputFolder, typeof(DefaultAsset), false);

            _animatedTileBaseNameOverride = EditorGUILayout.TextField(
                new GUIContent("AnimatedTile BaseName Override", "비우면 스프라이트 이름의 name을 그대로 사용"),
                _animatedTileBaseNameOverride);

            _overwriteIfExists = EditorGUILayout.Toggle(
                new GUIContent("Overwrite If Exists"), _overwriteIfExists);

            // 레이아웃
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Layout / Validation", EditorStyles.miniBoldLabel);

            _expectedCols = EditorGUILayout.IntField(new GUIContent("Expected Cols", "AnimatedTile을 만들 때 한 묶음의 컬럼 수"), _expectedCols);

            // expectedRows가 바뀌면 frameStartIndex 길이를 다시 맞춰준다.
            EditorGUI.BeginChangeCheck();
            int newRows = EditorGUILayout.IntField(new GUIContent("Expected Rows", "읽을 행(Row) 수"), _expectedRows);
            if (EditorGUI.EndChangeCheck())
            {
                OnChangedExpectedRows(newRows);
            }
            _expectedRows = newRows;

            _strictLayoutCheck = EditorGUILayout.Toggle(new GUIContent("Strict Layout Check"), _strictLayoutCheck);

            _framesAcross = EditorGUILayout.IntField(new GUIContent("Frames Across (per row)", "한 줄에 배치된 실제 프레임 수"), _framesAcross);

            // 행별 시작 인덱스 입력
            for (int i = 0; i < _expectedRows; i++)
            {
                // 리스트 길이가 부족할 수 있으므로 방어
                if (i >= _frameStartIndex.Count) break;
                _frameStartIndex[i] = EditorGUILayout.IntField(new GUIContent($"Start X Index (row {i})"), _frameStartIndex[i]);
            }

            // 애니메이션 설정
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Animation", EditorStyles.miniBoldLabel);
            _minSpeed = EditorGUILayout.FloatField(new GUIContent("Min Speed"), _minSpeed);
            _maxSpeed = EditorGUILayout.FloatField(new GUIContent("Max Speed"), _maxSpeed);
            _colliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup(new GUIContent("Collider Type"), _colliderType);
        }

        /// <summary>
        /// 실행 버튼 GUI를 그린다.
        /// </summary>
        private void DrawActionGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("3. 실행", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("애니메이션 타일 생성하기"))
            {
                SavePrefs();
                DoCreateAnimatedTiles();
            }
            EditorGUILayout.EndHorizontal();

            if (_animatedTileType == null)
            {
                EditorGUILayout.HelpBox(
                    "2D Tilemap Extras 패키지의 AnimatedTile 타입을 찾지 못했습니다.\n" +
                    "Package Manager에서 설치 후 다시 시도하세요.",
                    MessageType.Error);
            }
        }

        /// <summary>
        /// 로그 영역 GUI를 그린다.
        /// </summary>
        private void DrawLogGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Log / Preview", EditorStyles.boldLabel);
            var style = new GUIStyle(EditorStyles.textArea) { wordWrap = false };
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(160));
            _log = EditorGUILayout.TextArea(_log, style);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// expectedRows가 변경되었을 때, 각 행(row)별 시작 인덱스 리스트의 길이를 새 값에 맞게 조정한다.
        /// </summary>
        /// <param name="newValue">새로운 행(Row) 수</param>
        private void OnChangedExpectedRows(int newValue)
        {
            if (newValue < 0) newValue = 0;

            // 부족하면 0으로 채우고, 많으면 뒤를 자른다.
            EnsureListSize(_frameStartIndex, newValue);
        }

        /// <summary>
        /// 실제로 AnimatedTile을 생성하는 메인 로직.
        /// - 입력값 검증
        /// - 텍스처의 스프라이트 나열
        /// - (name,row,col) 단위로 그룹핑
        /// - 에셋 생성
        /// </summary>
        private void DoCreateAnimatedTiles()
        {
            _log = "";

            // 1) 기본 검증
            if (!ValidateInputs(out string texturePath, out string outPath))
                return;

            // 2) 텍스처의 모든 서브 스프라이트를 가져온다.
            var sprites = LoadSpritesFromTexture(texturePath);
            if (sprites.Count == 0)
            {
                Append($"[WARN] 스프라이트를 찾지 못했습니다. path={texturePath}");
                return;
            }

            // 3) 현재 옵션(framesAcross, expectedRows, expectedCols, frameStartIndex)에 맞춰 그룹으로 묶는다.
            var grouped = GroupSprites(sprites);
            if (grouped.Count == 0)
            {
                Append("[WARN] 조건에 맞는 그룹을 만들지 못했습니다.");
                return;
            }

            // 4) 그룹별로 AnimatedTile 에셋을 실제로 만든다.
            CreateAnimatedTileAssets(grouped, outPath);

            // 5) 저장/갱신
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 유효한 입력값인지 검사하고, 필요한 경로를 반환한다.
        /// </summary>
        private bool ValidateInputs(out string texturePath, out string outputPath)
        {
            texturePath = null;
            outputPath = null;

            if (_targetTexture == null)
            {
                Append("[ERROR] 대상 Texture2D를 먼저 선택하세요.");
                return false;
            }

            texturePath = AssetDatabase.GetAssetPath(_targetTexture);
            if (string.IsNullOrEmpty(texturePath))
            {
                Append("[ERROR] Texture2D 경로를 찾을 수 없습니다.");
                return false;
            }

            if (_outputFolder == null)
            {
                Append("[ERROR] Output Folder를 지정하세요.");
                return false;
            }

            outputPath = AssetDatabase.GetAssetPath(_outputFolder);
            if (!AssetDatabase.IsValidFolder(outputPath))
            {
                Append("[ERROR] Output Folder 경로가 유효하지 않습니다.");
                return false;
            }

            if (_expectedRows <= 0 || _expectedCols <= 0)
            {
                Append("[ERROR] expectedRows, expectedCols는 1 이상이어야 합니다.");
                return false;
            }

            if (_framesAcross <= 0)
            {
                Append("[ERROR] Frames Across 값이 0 이하입니다. 실제 시트의 가로 프레임 수를 입력하세요.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 주어진 텍스처 경로에서 모든 Sprite 서브에셋을 읽어 리스트로 반환한다.
        /// </summary>
        private List<Sprite> LoadSpritesFromTexture(string texturePath)
        {
            var list = new List<Sprite>();

            var atlasData = AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath);
            if (atlasData == null || atlasData.Length == 0)
                atlasData = AssetDatabase.LoadAllAssetsAtPath(texturePath);

            foreach (var data in atlasData)
            {
                if (data is Sprite s)
                    list.Add(s);
            }

            return list;
        }

        /// <summary>
        /// 스프라이트들을 현재 설정에 따라 (name,row,col)로 그룹핑한다.
        /// - 이 버전은 스프라이트 이름에 의존하지 않고 "순서"로만 묶는다.
        /// - 각 행(row)에서 frameStartIndex[row] 이전의 스프라이트는 건너뛴다.
        /// </summary>
        private Dictionary<(string name, int row, int col), List<(Sprite sprite, int frame)>> GroupSprites(List<Sprite> sprites)
        {
            var grouped = new Dictionary<(string name, int row, int col), List<(Sprite sprite, int frame)>>();

            int index = 0;      // 전체 스프라이트 순회 인덱스
            int col = 0;        // 현재 그룹 내에서의 col
            int row = 0;        // 현재 행
            int frameX = 0;     // 한 행에서 몇 번째 프레임인지 (0 ~ framesAcross-1)

            foreach (var sprite in sprites)
            {
                // 행이 초과되면 중단
                if (row >= _expectedRows)
                    break;

                // 이 행에서 설정한 시작 인덱스 이전이면 건너뛴다.
                if (index < _frameStartIndex[row])
                {
                    index++;
                    continue;
                }

                // base 이름 결정
                string finalBase = string.IsNullOrEmpty(_animatedTileBaseNameOverride)
                    ? sprite.name
                    : _animatedTileBaseNameOverride;

                var key = (finalBase, row, col);
                if (!grouped.TryGetValue(key, out var list))
                {
                    list = new List<(Sprite sprite, int frame)>();
                    grouped[key] = list;
                }

                // frameX 를 프레임 번호로 사용
                list.Add((sprite, frameX));

                // 다음 컬럼
                col++;
                if (col >= _expectedCols)
                {
                    col = 0;
                    frameX++;
                }

                // 한 행에서 framesAcross 만큼 읽었으면 다음 행으로
                if (frameX >= _framesAcross)
                {
                    frameX = 0;
                    row++;
                }

                index++;
            }

            return grouped;
        }

        /// <summary>
        /// 그룹핑된 스프라이트 정보를 바탕으로 AnimatedTile 에셋을 생성한다.
        /// </summary>
        private void CreateAnimatedTileAssets(
            Dictionary<(string name, int row, int col), List<(Sprite sprite, int frame)>> grouped,
            string outPath)
        {
            int created = 0, skipped = 0, overwritten = 0, errors = 0;

            foreach (var kv in grouped
                         .OrderBy(x => x.Key.name)
                         .ThenBy(x => x.Key.row)
                         .ThenBy(x => x.Key.col))
            {
                var key = kv.Key;
                var frames = kv.Value;
                frames.Sort((a, b) => a.frame.CompareTo(b.frame));

                string assetName = $"{key.name}_{key.row}_{key.col}.asset";
                string assetPath = Path.Combine(outPath, assetName).Replace("\\", "/");

                var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (existing != null && !_overwriteIfExists)
                {
                    skipped++;
                    continue;
                }

                // 폴더 보장
                EnsureFolder(outPath);

                if (existing != null && _overwriteIfExists)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    overwritten++;
                }

                // AnimatedTile 생성
                var tile = ScriptableObject.CreateInstance(_animatedTileType);
                var fSprites = _animatedTileType.GetField("m_AnimatedSprites");
                var fMin = _animatedTileType.GetField("m_MinSpeed");
                var fMax = _animatedTileType.GetField("m_MaxSpeed");
                var fCol = _animatedTileType.GetField("m_TileColliderType");

                if (fSprites == null || fMin == null || fMax == null || fCol == null)
                {
                    errors++;
                    Debug.LogError("[AnimatedTileBatchCreator] AnimatedTile 필드를 찾지 못했습니다. Extras 패키지 버전을 확인하세요.");
                    UnityEngine.Object.DestroyImmediate(tile);
                    continue;
                }

                fSprites.SetValue(tile, frames.Select(f => f.sprite).ToArray());
                fMin.SetValue(tile, _minSpeed);
                fMax.SetValue(tile, Mathf.Max(_minSpeed, _maxSpeed));
                fCol.SetValue(tile, _colliderType);

                AssetDatabase.CreateAsset(tile, assetPath);
                created++;
            }

            Append($"[CREATE] Created={created}, Overwritten={overwritten}, Skipped={skipped}, Errors={errors}");
        }

        /// <summary>
        /// 리스트의 길이를 원하는 크기로 맞춘다. 부족하면 defaultValue로 채우고, 넘치면 자른다.
        /// </summary>
        private static void EnsureListSize<T>(List<T> list, int size, T defaultValue = default)
        {
            if (list == null) return;
            if (size < 0) size = 0;

            // 늘려야 하는 경우
            while (list.Count < size)
                list.Add(defaultValue);

            // 줄여야 하는 경우
            if (list.Count > size)
                list.RemoveRange(size, list.Count - size);
        }

        /// <summary>
        /// 에디터 환경설정(EditorPrefs)에 현재 설정을 저장한다.
        /// </summary>
        private void SavePrefs()
        {
            EditorPrefs.SetString(Prefs + "animatedTileBaseNameOverride", _animatedTileBaseNameOverride);
            EditorPrefs.SetBool(Prefs + "overwriteIfExists", _overwriteIfExists);
            EditorPrefs.SetInt(Prefs + "expectedCols", _expectedCols);
            EditorPrefs.SetInt(Prefs + "expectedRows", _expectedRows);
            EditorPrefs.SetInt(Prefs + "framesAcross", _framesAcross);
            EditorPrefs.SetBool(Prefs + "strictLayoutCheck", _strictLayoutCheck);
            EditorPrefs.SetFloat(Prefs + "minSpeed", _minSpeed);
            EditorPrefs.SetFloat(Prefs + "maxSpeed", _maxSpeed);
            EditorPrefs.SetInt(Prefs + "colliderType", (int)_colliderType);
            
            // frameStartIndex 저장 (예: "0|3|5|0")
            string joined = string.Join("|", _frameStartIndex);
            EditorPrefs.SetString(PrefsFrameStartIndex, joined);
        }

        /// <summary>
        /// 에디터 환경설정(EditorPrefs)에서 설정을 불러온다.
        /// </summary>
        private void LoadPrefs()
        {
            _animatedTileBaseNameOverride = EditorPrefs.GetString(Prefs + "animatedTileBaseNameOverride", "");
            _overwriteIfExists = EditorPrefs.GetBool(Prefs + "overwriteIfExists", false);
            _expectedCols = EditorPrefs.GetInt(Prefs + "expectedCols", 3);
            _expectedRows = EditorPrefs.GetInt(Prefs + "expectedRows", 4);
            _framesAcross = EditorPrefs.GetInt(Prefs + "framesAcross", 8);
            _strictLayoutCheck = EditorPrefs.GetBool(Prefs + "strictLayoutCheck", true);
            _minSpeed = EditorPrefs.GetFloat(Prefs + "minSpeed", 1f);
            _maxSpeed = EditorPrefs.GetFloat(Prefs + "maxSpeed", 1f);
            _colliderType = (Tile.ColliderType)EditorPrefs.GetInt(Prefs + "colliderType", (int)Tile.ColliderType.None);

            // frameStartIndex 복원
            _frameStartIndex.Clear();
            string saved = EditorPrefs.GetString(PrefsFrameStartIndex, string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                var parts = saved.Split('|');
                foreach (var p in parts)
                {
                    _frameStartIndex.Add(int.TryParse(p, out int v) ? v : 0);
                }
            }

            // 불러온 리스트 길이를 expectedRows에 맞춰 보정
            EnsureListSize(_frameStartIndex, _expectedRows);
        }

        /// <summary>
        /// 주어진 경로가 존재하도록 Project 내에 폴더를 생성한다.
        /// </summary>
        private static string EnsureFolder(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath)) return "Assets";

            var segments = projectPath.Replace("\\", "/").Split('/');
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
        /// 로그 문자열을 한 줄 추가한다.
        /// </summary>
        private void Append(string line)
        {
            _log += line + "\n";
        }
    }
}
#endif
