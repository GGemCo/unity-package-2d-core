#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Editor로 Slice 된 Texture2D의 각 Sub Sprite 영역을 좌우 반전하여 새 Multiple Sprite Atlas를 생성하는 EditorWindow입니다.
    /// </summary>
    internal sealed class SpriteSliceFlipExporterWindow : EditorWindow
    {
        /// <summary>
        /// 에디터 창 제목입니다.
        /// </summary>
        private const string Title = "Sprite Slice 좌우 반전";

        /// <summary>
        /// EditorPrefs 저장 키 접두사입니다.
        /// </summary>
        private const string PrefKey = "GGemCo_SpriteSliceFlipExporter_";

        /// <summary>
        /// 좌우 반전할 원본 Texture2D입니다.
        /// </summary>
        [Tooltip("Sprite Editor에서 Multiple Sprite로 Slice 된 원본 Texture2D입니다.")]
        [SerializeField] private Texture2D sourceTexture;

        /// <summary>
        /// 출력 및 처리 옵션입니다.
        /// </summary>
        [SerializeField] private SpriteSliceFlipSettings settings = new SpriteSliceFlipSettings();

        /// <summary>
        /// 에디터 창 스크롤 위치입니다.
        /// </summary>
        private Vector2 _scrollPosition;

        /// <summary>
        /// 현재 원본 텍스처에서 읽은 Slice 정보 캐시입니다.
        /// </summary>
        private IReadOnlyList<SpriteSliceInfo> _cachedSlices = Array.Empty<SpriteSliceInfo>();

        /// <summary>
        /// Slice 정보 캐시를 만든 원본 텍스처 에셋 경로입니다.
        /// </summary>
        private string _cachedSourcePath;

        /// <summary>
        /// 마지막으로 연 폴더 선택 경로입니다.
        /// </summary>
        private string _lastSystemFolder;

        /// <summary>
        /// Sprite Slice 좌우 반전 창을 Unity 메뉴에 등록하고 표시합니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolSpriteSliceFlipExporter, false, (int)ConfigEditor.ToolOrdering.SpriteSliceFlipExporter)]
        public static void Open()
        {
            var window = GetWindow<SpriteSliceFlipExporterWindow>(Title);
            window.minSize = new Vector2(460f, 520f);
            window.Show();
        }

        /// <summary>
        /// 창이 활성화될 때 저장된 설정을 불러오고 현재 선택 텍스처를 반영합니다.
        /// </summary>
        private void OnEnable()
        {
            LoadPrefs();
            if (sourceTexture == null && Selection.activeObject is Texture2D selectedTexture)
            {
                sourceTexture = selectedTexture;
            }

            RefreshSlices();
        }

        /// <summary>
        /// 에디터 창 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawHeader();
            DrawSourceField();
            DrawOutputSettings();
            DrawSliceInfo();
            DrawActions();
            EditorGUILayout.EndScrollView();
            SavePrefs();
        }

        /// <summary>
        /// 창 상단 제목과 사용 범위 안내를 표시합니다.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(Title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Sprite Editor로 Slice 된 Texture2D를 선택하면 각 Sub Sprite rect 내부 픽셀만 좌우 반전한 새 PNG Atlas를 생성합니다. 원본 텍스처는 수정하지 않습니다.",
                MessageType.Info);
        }

        /// <summary>
        /// 원본 Texture2D 선택 필드를 표시합니다.
        /// </summary>
        private void DrawSourceField()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("입력", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUI.BeginChangeCheck();
                using (new EditorGUILayout.HorizontalScope())
                {
                    sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
                        new GUIContent("Source Texture", "Sprite Editor에서 Multiple Sprite로 Slice 된 Texture2D입니다."),
                        sourceTexture,
                        typeof(Texture2D),
                        false);

                    if (GUILayout.Button("선택", GUILayout.Width(56f)))
                    {
                        if (Selection.activeObject is Texture2D selectedTexture)
                        {
                            sourceTexture = selectedTexture;
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    RefreshSlices();
                }

                if (sourceTexture != null)
                {
                    EditorGUILayout.LabelField("경로", AssetDatabase.GetAssetPath(sourceTexture));
                }
            }
        }

        /// <summary>
        /// 출력 및 메타데이터 보정 옵션 UI를 표시합니다.
        /// </summary>
        private void DrawOutputSettings()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("출력 설정", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    settings.outputFolder = EditorGUILayout.TextField(
                        new GUIContent("Output Folder", "생성된 PNG Atlas를 저장할 Assets 하위 폴더입니다."),
                        settings.outputFolder);

                    if (GUILayout.Button("찾기...", GUILayout.Width(72f)))
                    {
                        var startFolder = string.IsNullOrWhiteSpace(_lastSystemFolder) ? Application.dataPath : _lastSystemFolder;
                        var selectedFolder = EditorUtility.OpenFolderPanel("출력 폴더 선택", startFolder, string.Empty);
                        if (SpriteSliceFlipAssetWriter.TryConvertToAssetFolderPath(selectedFolder, out var assetFolderPath))
                        {
                            settings.outputFolder = assetFolderPath;
                            _lastSystemFolder = selectedFolder;
                        }
                        else if (!string.IsNullOrEmpty(selectedFolder))
                        {
                            EditorUtility.DisplayDialog(Title, "출력 폴더는 현재 Unity 프로젝트의 Assets 폴더 하위여야 합니다.", "확인");
                        }
                    }
                }

                settings.outputNameSuffix = EditorGUILayout.TextField(
                    new GUIContent("Output Suffix", "원본 텍스처 이름 뒤에 붙일 접미사입니다."),
                    settings.outputNameSuffix);
                settings.appendSuffixToSpriteNames = EditorGUILayout.ToggleLeft(
                    new GUIContent("Sub Sprite 이름에도 Suffix 적용", "생성되는 Sub Sprite 이름에도 출력 접미사를 붙입니다."),
                    settings.appendSuffixToSpriteNames);
                settings.mirrorPivot = EditorGUILayout.ToggleLeft(
                    new GUIContent("Pivot 좌우 반전", "Pivot X를 1 - X로 보정합니다."),
                    settings.mirrorPivot);
                settings.mirrorBorder = EditorGUILayout.ToggleLeft(
                    new GUIContent("Border 좌우 반전", "9-Slice Border의 Left/Right 값을 교체합니다."),
                    settings.mirrorBorder);
                settings.includeFullyTransparentSprites = EditorGUILayout.ToggleLeft(
                    new GUIContent("완전 투명 Slice 포함", "알파가 모두 0인 Slice도 출력 Sprite 메타데이터에 포함합니다."),
                    settings.includeFullyTransparentSprites);
                settings.restoreSourceReadable = EditorGUILayout.ToggleLeft(
                    new GUIContent("원본 Read/Write 설정 복구", "작업 중 Read/Write Enabled를 켠 경우 완료 후 원래 값으로 복구합니다."),
                    settings.restoreSourceReadable);
                settings.overwriteExisting = EditorGUILayout.ToggleLeft(
                    new GUIContent("같은 이름 덮어쓰기", "같은 이름의 PNG가 있으면 덮어씁니다. 비활성화 시 고유 경로를 생성합니다."),
                    settings.overwriteExisting);

                settings.Normalize();
            }
        }

        /// <summary>
        /// 감지된 Slice 목록과 개수를 표시합니다.
        /// </summary>
        private void DrawSliceInfo()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Slice 정보", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("감지된 Sub Sprite", _cachedSlices.Count.ToString());
                    if (GUILayout.Button("새로고침", GUILayout.Width(80f)))
                    {
                        RefreshSlices(force: true);
                    }
                }

                if (sourceTexture == null)
                {
                    EditorGUILayout.HelpBox("Source Texture를 선택해주세요.", MessageType.Warning);
                    return;
                }

                if (_cachedSlices.Count == 0)
                {
                    EditorGUILayout.HelpBox("감지된 Sprite Slice가 없습니다. Texture Import Mode가 Multiple이고 Sprite Editor에서 Slice가 저장되어 있는지 확인해주세요.", MessageType.Warning);
                    return;
                }

                var previewCount = Mathf.Min(_cachedSlices.Count, 12);
                for (var i = 0; i < previewCount; i++)
                {
                    var slice = _cachedSlices[i];
                    EditorGUILayout.LabelField(
                        $"{i + 1}. {slice.Name}",
                        $"Rect: {FormatRect(slice.Rect)} / Pivot: {slice.Pivot.x:0.###}, {slice.Pivot.y:0.###}");
                }

                if (_cachedSlices.Count > previewCount)
                {
                    EditorGUILayout.LabelField($"... 외 {_cachedSlices.Count - previewCount}개");
                }
            }
        }

        /// <summary>
        /// 실행 버튼 영역을 표시합니다.
        /// </summary>
        private void DrawActions()
        {
            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(sourceTexture == null))
                {
                    if (GUILayout.Button("Slice 정보 새로고침", GUILayout.Height(30f)))
                    {
                        RefreshSlices(force: true);
                    }
                }

                using (new EditorGUI.DisabledScope(!CanGenerate()))
                {
                    if (GUILayout.Button("좌우 반전 Atlas 생성", GUILayout.Height(30f)))
                    {
                        TryGenerateFlippedAtlas();
                    }
                }
            }
        }

        /// <summary>
        /// 현재 설정으로 좌우 반전 Atlas를 생성할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>생성 가능하면 true입니다.</returns>
        private bool CanGenerate()
        {
            return sourceTexture != null && _cachedSlices.Count > 0 && !string.IsNullOrWhiteSpace(settings.outputFolder);
        }

        /// <summary>
        /// 원본 Texture2D에서 Sprite Editor Slice 정보를 다시 읽어옵니다.
        /// </summary>
        /// <param name="force">동일한 경로여도 강제로 다시 읽을지 여부입니다.</param>
        private void RefreshSlices(bool force = false)
        {
            _cachedSlices = Array.Empty<SpriteSliceInfo>();
            _cachedSourcePath = null;

            if (sourceTexture == null)
            {
                return;
            }

            var sourcePath = AssetDatabase.GetAssetPath(sourceTexture);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return;
            }

            if (!force && _cachedSourcePath == sourcePath && _cachedSlices.Count > 0)
            {
                return;
            }

            try
            {
                _cachedSlices = SpriteSliceFlipMetadataUtility.ReadSlices(sourceTexture);
                _cachedSourcePath = sourcePath;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _cachedSlices = Array.Empty<SpriteSliceInfo>();
            }
        }

        /// <summary>
        /// 현재 Source Texture 기준으로 좌우 반전 Atlas를 생성합니다.
        /// </summary>
        private void TryGenerateFlippedAtlas()
        {
            Texture2D flippedTexture = null;
            try
            {
                settings.Normalize();
                ValidateBeforeGenerate();

                var sourcePath = AssetDatabase.GetAssetPath(sourceTexture);
                using (new SpriteSliceFlipSourceReadableScope(sourceTexture, settings.restoreSourceReadable))
                {
                    var readableSource = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                    var sourceSlices = SpriteSliceFlipMetadataUtility.ReadSlices(readableSource);
                    flippedTexture = SpriteSliceFlipProcessor.CreateFlippedTexture(
                        readableSource,
                        sourceSlices,
                        settings,
                        out var processedSlices,
                        out var skippedTransparentCount);

                    var assetPath = SpriteSliceFlipAssetWriter.SaveFlippedAtlas(
                        readableSource,
                        flippedTexture,
                        processedSlices,
                        settings);

                    var result = new SpriteSliceFlipResult(assetPath, processedSlices.Count, skippedTransparentCount);
                    SelectCreatedAsset(result.AssetPath);
                    EditorUtility.DisplayDialog(
                        Title,
                        $"좌우 반전 Sprite Atlas를 생성했습니다.\n\n경로: {result.AssetPath}\n처리 Sprite: {result.ProcessedSpriteCount}\n투명으로 건너뜀: {result.SkippedTransparentSpriteCount}",
                        "확인");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SpriteSliceFlipExporter] 사용자가 작업을 취소했습니다.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(Title, "좌우 반전 Atlas 생성 중 오류가 발생했습니다. 콘솔을 확인해주세요.\n" + exception.Message, "확인");
            }
            finally
            {
                if (flippedTexture != null)
                {
                    DestroyImmediate(flippedTexture);
                }

                EditorUtility.ClearProgressBar();
                RefreshSlices(force: true);
            }
        }

        /// <summary>
        /// 생성 전 필수 입력값을 검증합니다.
        /// </summary>
        private void ValidateBeforeGenerate()
        {
            if (sourceTexture == null)
            {
                throw new InvalidOperationException("Source Texture를 선택해주세요.");
            }

            var sourcePath = AssetDatabase.GetAssetPath(sourceTexture);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new InvalidOperationException("Source Texture의 AssetDatabase 경로를 찾을 수 없습니다.");
            }

            var normalizedOutputFolder = settings.outputFolder.Replace('\\', '/').TrimEnd('/');
            if (!string.Equals(normalizedOutputFolder, "Assets", StringComparison.Ordinal)
                && !normalizedOutputFolder.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Output Folder는 Assets 폴더 하위여야 합니다.");
            }

            if (_cachedSlices.Count == 0)
            {
                RefreshSlices(force: true);
            }

            if (_cachedSlices.Count == 0)
            {
                throw new InvalidOperationException("좌우 반전할 Sprite Slice 정보가 없습니다.");
            }
        }

        /// <summary>
        /// 생성된 PNG 에셋을 Project 창에서 선택하고 Ping합니다.
        /// </summary>
        /// <param name="assetPath">생성된 에셋 경로입니다.</param>
        private static void SelectCreatedAsset(string assetPath)
        {
            var createdAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (createdAsset == null)
            {
                return;
            }

            Selection.activeObject = createdAsset;
            EditorGUIUtility.PingObject(createdAsset);
        }

        /// <summary>
        /// Rect 정보를 UI 표시용 문자열로 변환합니다.
        /// </summary>
        /// <param name="rect">표시할 Rect입니다.</param>
        /// <returns>픽셀 기준 Rect 문자열입니다.</returns>
        private static string FormatRect(Rect rect)
        {
            return $"({rect.x:0}, {rect.y:0}, {rect.width:0}, {rect.height:0})";
        }

        /// <summary>
        /// EditorPrefs에서 툴 설정을 불러옵니다.
        /// </summary>
        private void LoadPrefs()
        {
            settings.outputFolder = EditorPrefs.GetString(PrefKey + nameof(settings.outputFolder), settings.outputFolder);
            settings.outputNameSuffix = EditorPrefs.GetString(PrefKey + nameof(settings.outputNameSuffix), settings.outputNameSuffix);
            settings.appendSuffixToSpriteNames = EditorPrefs.GetBool(PrefKey + nameof(settings.appendSuffixToSpriteNames), settings.appendSuffixToSpriteNames);
            settings.mirrorPivot = EditorPrefs.GetBool(PrefKey + nameof(settings.mirrorPivot), settings.mirrorPivot);
            settings.mirrorBorder = EditorPrefs.GetBool(PrefKey + nameof(settings.mirrorBorder), settings.mirrorBorder);
            settings.overwriteExisting = EditorPrefs.GetBool(PrefKey + nameof(settings.overwriteExisting), settings.overwriteExisting);
            settings.includeFullyTransparentSprites = EditorPrefs.GetBool(PrefKey + nameof(settings.includeFullyTransparentSprites), settings.includeFullyTransparentSprites);
            settings.restoreSourceReadable = EditorPrefs.GetBool(PrefKey + nameof(settings.restoreSourceReadable), settings.restoreSourceReadable);
            _lastSystemFolder = EditorPrefs.GetString(PrefKey + nameof(_lastSystemFolder), Application.dataPath);
            settings.Normalize();
        }

        /// <summary>
        /// 현재 툴 설정을 EditorPrefs에 저장합니다.
        /// </summary>
        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefKey + nameof(settings.outputFolder), settings.outputFolder);
            EditorPrefs.SetString(PrefKey + nameof(settings.outputNameSuffix), settings.outputNameSuffix);
            EditorPrefs.SetBool(PrefKey + nameof(settings.appendSuffixToSpriteNames), settings.appendSuffixToSpriteNames);
            EditorPrefs.SetBool(PrefKey + nameof(settings.mirrorPivot), settings.mirrorPivot);
            EditorPrefs.SetBool(PrefKey + nameof(settings.mirrorBorder), settings.mirrorBorder);
            EditorPrefs.SetBool(PrefKey + nameof(settings.overwriteExisting), settings.overwriteExisting);
            EditorPrefs.SetBool(PrefKey + nameof(settings.includeFullyTransparentSprites), settings.includeFullyTransparentSprites);
            EditorPrefs.SetBool(PrefKey + nameof(settings.restoreSourceReadable), settings.restoreSourceReadable);
            EditorPrefs.SetString(PrefKey + nameof(_lastSystemFolder), _lastSystemFolder ?? Application.dataPath);
        }
    }
}
#endif
