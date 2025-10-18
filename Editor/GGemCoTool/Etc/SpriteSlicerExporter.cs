#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// PNG 스프라이트 슬라이서 & 내보내기 툴
    /// - Source PNG(Texture2D) 하나를 선택
    /// - Cell 폭/높이 기준으로 격자 자르기
    /// - (옵션) 여백/패딩, 부분 조각 포함 여부
    /// - 내보내기 폴더 선택 후 개별 PNG로 저장
    /// - 저장된 PNG는 Sprite(importer)로 자동 설정
    ///
    /// 성능/유지보수:
    /// - 대용량 텍스처는 진행바 표시 및 취소 지원
    /// - 임포터 Read/Write 임시 활성화 후 복구
    /// - 명명 규칙 템플릿 제공: {name},{row},{col},{index}
    /// </summary>
    internal class SpriteSlicerExporter : EditorWindow
    {
        private const string Title = "PNG 슬라이서 & 내보내기";
        private const string PrefKey = "GGemCo_SpriteSlicerExporter_";

        [Header("입력")]
        [Tooltip("슬라이스할 원본 PNG(Texture2D)")]
        [SerializeField] private Texture2D source;

        [Header("슬라이스 설정")]
        [Tooltip("각 타일(셀)의 폭(px)")]
        [SerializeField] private int cellWidth = 32;
        [Tooltip("각 타일(셀)의 높이(px)")]
        [SerializeField] private int cellHeight = 32;

        [Space]
        [Tooltip("원본 좌상단 기준 좌/상 여백(px)")]
        [SerializeField] private int marginLeft = 0;
        [SerializeField] private int marginTop = 0;

        [Tooltip("셀 사이 패딩(px): 좌우/상하 동일 간격")]
        [SerializeField] private int paddingX = 0;
        [SerializeField] private int paddingY = 0;

        [Tooltip("끝부분에 남는 영역이 셀 크기보다 작아도 내보낼지 여부")]
        [SerializeField] private bool includePartial = false;

        [Header("내보내기")]
        [Tooltip("내보낼 폴더(프로젝트 외부/내부 모두 가능)")]
        [SerializeField] private string exportFolder = "Assets/Sliced";

        [Tooltip("파일 이름 템플릿: {name} {row} {col} {index} 사용 가능")]
        [SerializeField] private string fileNameTemplate = "{name}_{row}_{col}";
        
        [Tooltip("내보낸 PNG를 Sprite 타입으로 임포트 (프로젝트 안에 저장한 경우)")]
        [SerializeField] private bool importAsSprite = true;

        [Tooltip("Sprite Pixels Per Unit 값")]
        [SerializeField] private float pixelsPerUnit = 32f;

        [Tooltip("PNG 파일이 이미 존재하면 덮어쓰기")]
        [SerializeField] private bool overwrite = true;

        // 캐시
        private string _lastSystemFolder = "";

        [MenuItem(ConfigEditor.NameToolSpriteSlicerExporter, false, (int)ConfigEditor.ToolOrdering.SpriteSlicerExporter)]
        public static void ShowWindow()
        {
            var win = GetWindow<SpriteSlicerExporter>(Title);
            win.minSize = new Vector2(420, 420);
            win.LoadPrefs();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                source = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Source PNG"), source, typeof(Texture2D), false);
                if (GUILayout.Button("선택", GUILayout.Width(60)))
                {
                    var obj = Selection.activeObject as Texture2D;
                    if (obj) source = obj;
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("슬라이스 설정", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                cellWidth  = EditorGUILayout.IntField(new GUIContent("Cell Width(px)"),  Mathf.Max(1, cellWidth));
                cellHeight = EditorGUILayout.IntField(new GUIContent("Cell Height(px)"), Mathf.Max(1, cellHeight));
                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("여백/패딩(px)");
                using (new EditorGUILayout.HorizontalScope())
                {
                    marginLeft = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Left"),  marginLeft));
                    marginTop  = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Top"),   marginTop));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    paddingX = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Padding X"), paddingX));
                    paddingY = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Padding Y"), paddingY));
                }

                includePartial = EditorGUILayout.ToggleLeft(new GUIContent("부분 조각 포함(include partial)"), includePartial);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("내보내기", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    exportFolder = EditorGUILayout.TextField("Export Folder", exportFolder);
                    if (GUILayout.Button("찾기...", GUILayout.Width(72)))
                    {
                        var start = Directory.Exists(_lastSystemFolder) ? _lastSystemFolder : Application.dataPath;
                        var chosen = EditorUtility.OpenFolderPanel("내보낼 폴더 선택", start, "");
                        if (!string.IsNullOrEmpty(chosen))
                        {
                            exportFolder = chosen;
                            _lastSystemFolder = chosen;
                        }
                    }
                }
                fileNameTemplate = EditorGUILayout.TextField(new GUIContent("파일명 템플릿"), fileNameTemplate);
                importAsSprite = EditorGUILayout.ToggleLeft(new GUIContent("프로젝트 내부 저장시 Sprite로 임포트"), importAsSprite);
                pixelsPerUnit  = EditorGUILayout.FloatField(new GUIContent("Pixels Per Unit"), Mathf.Max(1f, pixelsPerUnit));
                overwrite      = EditorGUILayout.ToggleLeft(new GUIContent("덮어쓰기 허용(Overwrite)"), overwrite);
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("슬라이스 미리보기(격자 정보)"))
                {
                    ShowPreviewInfo();
                }
                var canExport = source && cellWidth > 0 && cellHeight > 0 && !string.IsNullOrEmpty(exportFolder);
                EditorGUI.BeginDisabledGroup(!canExport);
                if (GUILayout.Button("슬라이스 & 내보내기", GUILayout.Height(32)))
                {
                    TryExport();
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "파일명 템플릿 키: {name} = 원본 텍스처 이름, {row}, {col} = 0부터 시작, {index} = 0부터 증가.\n" +
                "프로젝트 내부(Assets 하위)로 저장하면 자동 임포트 및 Sprite 설정 옵션을 적용할 수 있습니다.", 
                MessageType.Info);

            SavePrefsIfChanged();
        }

        #region Prefs
        private void LoadPrefs()
        {
            cellWidth     = EditorPrefs.GetInt(PrefKey + "cellWidth", cellWidth);
            cellHeight    = EditorPrefs.GetInt(PrefKey + "cellHeight", cellHeight);
            marginLeft    = EditorPrefs.GetInt(PrefKey + "marginLeft", marginLeft);
            marginTop     = EditorPrefs.GetInt(PrefKey + "marginTop", marginTop);
            paddingX      = EditorPrefs.GetInt(PrefKey + "paddingX", paddingX);
            paddingY      = EditorPrefs.GetInt(PrefKey + "paddingY", paddingY);
            includePartial= EditorPrefs.GetBool(PrefKey + "includePartial", includePartial);
            exportFolder  = EditorPrefs.GetString(PrefKey + "exportFolder", exportFolder);
            fileNameTemplate = EditorPrefs.GetString(PrefKey + "fileNameTemplate", fileNameTemplate);
            importAsSprite   = EditorPrefs.GetBool(PrefKey + "importAsSprite", importAsSprite);
            pixelsPerUnit    = EditorPrefs.GetFloat(PrefKey + "ppu", pixelsPerUnit);
            overwrite        = EditorPrefs.GetBool(PrefKey + "overwrite", overwrite);
            _lastSystemFolder= EditorPrefs.GetString(PrefKey + "lastSysFolder", _lastSystemFolder);
        }

        private void SavePrefsIfChanged()
        {
            EditorPrefs.SetInt(PrefKey + "cellWidth", cellWidth);
            EditorPrefs.SetInt(PrefKey + "cellHeight", cellHeight);
            EditorPrefs.SetInt(PrefKey + "marginLeft", marginLeft);
            EditorPrefs.SetInt(PrefKey + "marginTop", marginTop);
            EditorPrefs.SetInt(PrefKey + "paddingX", paddingX);
            EditorPrefs.SetInt(PrefKey + "paddingY", paddingY);
            EditorPrefs.SetBool(PrefKey + "includePartial", includePartial);
            EditorPrefs.SetString(PrefKey + "exportFolder", exportFolder);
            EditorPrefs.SetString(PrefKey + "fileNameTemplate", fileNameTemplate);
            EditorPrefs.SetBool(PrefKey + "importAsSprite", importAsSprite);
            EditorPrefs.SetFloat(PrefKey + "ppu", pixelsPerUnit);
            EditorPrefs.SetBool(PrefKey + "overwrite", overwrite);
            EditorPrefs.SetString(PrefKey + "lastSysFolder", _lastSystemFolder);
        }
        #endregion

        #region Preview
        private void ShowPreviewInfo()
        {
            if (!source)
            {
                EditorUtility.DisplayDialog(Title, "Source PNG을 선택해주세요.", "확인");
                return;
            }
            var texPath = AssetDatabase.GetAssetPath(source);
            var (w, h) = (source.width, source.height);
            var counts = ComputeGridCounts(w, h);
            EditorUtility.DisplayDialog(
                "격자 미리보기",
                $"텍스처: {source.name}\n경로: {texPath}\n크기: {w}x{h}\n" +
                $"셀: {cellWidth}x{cellHeight} | 마진(L:{marginLeft}, T:{marginTop}) | 패딩({paddingX},{paddingY})\n" +
                $"결과 타일 수(행 x 열): {counts.rows} x {counts.cols} {(includePartial ? "(부분 포함)" : "")}",
                "확인");
        }
        #endregion

        #region Export
        private void TryExport()
        {
            if (!source)
            {
                EditorUtility.DisplayDialog(Title, "Source PNG을 선택해주세요.", "확인");
                return;
            }
            if (cellWidth <= 0 || cellHeight <= 0)
            {
                EditorUtility.DisplayDialog(Title, "Cell Width/Height는 1 이상이어야 합니다.", "확인");
                return;
            }
            if (string.IsNullOrEmpty(exportFolder))
            {
                EditorUtility.DisplayDialog(Title, "내보낼 폴더를 지정해주세요.", "확인");
                return;
            }

            // 폴더 준비
            Directory.CreateDirectory(exportFolder);

            // Read/Write 활성화(임시)
            var texPath = AssetDatabase.GetAssetPath(source);
            TextureImporter importer = null;
            bool changedReadable = false;
            if (!string.IsNullOrEmpty(texPath))
            {
                importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    changedReadable = true;
                    var so = new SerializedObject(importer);
                    importer.isReadable = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    importer.SaveAndReimport();
                }
            }

            try
            {
                ExportByGrid(source, exportFolder);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpriteSlicerExporter] Export failed: {ex}");
                EditorUtility.DisplayDialog(Title, "내보내기 중 오류가 발생했습니다. 콘솔을 확인해주세요.", "확인");
            }
            finally
            {
                // Read/Write 복구
                if (importer != null && changedReadable)
                {
                    var so = new SerializedObject(importer);
                    importer.isReadable = false;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    importer.SaveAndReimport();
                }
                EditorUtility.ClearProgressBar();
            }
        }

        private (int rows, int cols) ComputeGridCounts(int texW, int texH)
        {
            int usableW = Mathf.Max(0, texW - marginLeft);
            int usableH = Mathf.Max(0, texH - marginTop);

            int stepX = cellWidth + paddingX;
            int stepY = cellHeight + paddingY;

            int cols = 0, rows = 0;

            // 가로
            if (stepX > 0)
            {
                cols = (usableW + (includePartial ? stepX - 1 : 0)) / stepX;
            }
            // 세로
            if (stepY > 0)
            {
                rows = (usableH + (includePartial ? stepY - 1 : 0)) / stepY;
            }
            return (rows, cols);
        }

        private void ExportByGrid(Texture2D tex, string outFolder)
        {
            int texW = tex.width;
            int texH = tex.height;

            var counts = ComputeGridCounts(texW, texH);
            int total = counts.rows * counts.cols;
            if (total <= 0)
            {
                EditorUtility.DisplayDialog(Title, "슬라이스할 셀이 없습니다. 설정을 확인해주세요.", "확인");
                return;
            }

            string srcNameSafe = SafeName(tex.name);
            int index = 0;

            // Unity 텍스처는 좌하단(0,0) 기준이므로, Top 마진 적용을 위해 y 시작을 texH - marginTop - cellHeight에서 감소
            for (int r = 0; r < counts.rows; r++)
            {
                for (int c = 0; c < counts.cols; c++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "슬라이스 & 내보내는 중...",
                        $"Row {r}, Col {c} 처리 중...",
                        (float)index / Mathf.Max(1, total)))
                    {
                        throw new OperationCanceledException("사용자 취소");
                    }

                    // 좌상단 기준 좌표 → 유니티 좌하단 기준으로 변환
                    int x = marginLeft + c * (cellWidth + paddingX);
                    int yTop = marginTop + r * (cellHeight + paddingY);
                    int y = texH - yTop - cellHeight; // 좌상단 -> 좌하단 기준 변환

                    int sliceW = Mathf.Min(cellWidth, texW - x);
                    int sliceH = Mathf.Min(cellHeight, y + cellHeight <= texH ? cellHeight : texH - y);

                    if (sliceW <= 0 || sliceH <= 0)
                        continue;

                    // 부분 포함이 아니고, 셀 크기보다 작으면 스킵
                    if (!includePartial && (sliceW < cellWidth || sliceH < cellHeight))
                        continue;

                    // 픽셀 추출
                    var pixels = tex.GetPixels(x, Mathf.Max(0, y), Mathf.Max(1, sliceW), Mathf.Max(1, sliceH));

                    // 완전 투명(Alpha = 0)인 셀은 건너뛰기
                    bool isEmpty = true;
                    for (int p = 0; p < pixels.Length; p++)
                    {
                        if (pixels[p].a > 0.001f) // 알파 임계값(0.001)
                        {
                            isEmpty = false;
                            break;
                        }
                    }
                    if (isEmpty)
                    {
                        index++;
                        continue; // 아무것도 없는 셀 → 스킵
                    }
                    
                    // 새 텍스처 구성
                    var tile = new Texture2D(sliceW, sliceH, TextureFormat.RGBA32, false);
                    tile.SetPixels(pixels);
                    tile.Apply(false, false);

                    // 파일명 템플릿
                    string fileName = fileNameTemplate;
                    fileName = fileName.Replace("{name}", srcNameSafe)
                                       .Replace("{row}", r.ToString())
                                       .Replace("{col}", c.ToString())
                                       .Replace("{index}", index.ToString());
                    fileName = SafeName(fileName);

                    string outPath = Path.Combine(outFolder, $"{fileName}.png");

                    // 덮어쓰기 체크
                    if (!overwrite && File.Exists(outPath))
                    {
                        index++;
                        UnityEngine.Object.DestroyImmediate(tile);
                        continue;
                    }

                    // PNG로 저장
                    var data = tile.EncodeToPNG();
                    File.WriteAllBytes(outPath, data);
                    UnityEngine.Object.DestroyImmediate(tile);

                    // 프로젝트 내부(Assets 하위)일 경우 임포트 설정
                    if (IsUnderAssets(outPath))
                    {
                        // OS 절대경로 → 프로젝트 상대경로
                        string relPath = AbsoluteToProjectRelative(outPath);
                        AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceUpdate);

                        if (importAsSprite)
                        {
                            var ti = AssetImporter.GetAtPath(relPath) as TextureImporter;
                            if (ti != null)
                            {
                                ti.textureType = TextureImporterType.Sprite;
                                ti.spritePixelsPerUnit = pixelsPerUnit;
                                ti.spriteImportMode = SpriteImportMode.Single;
                                ti.alphaIsTransparency = true;
                                ti.filterMode = FilterMode.Point; // 일반 픽셀아트 가정. 필요시 변경 가능
                                ti.wrapMode = TextureWrapMode.Clamp;
                                ti.SaveAndReimport();
                            }
                        }
                    }

                    index++;
                }
            }

            // 에셋 데이터베이스 갱신
            if (IsUnderAssets(outFolder))
                AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(Title, $"완료: 총 {index}개의 PNG를 내보냈습니다.", "확인");
        }

        private static string SafeName(string s)
        {
            // 파일명 안전 치환
            return Regex.Replace(s, @"[^a-zA-Z0-9_\-\.]", "_");
        }

        private static bool IsUnderAssets(string absolutePath)
        {
            var proj = Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/');
            var abs  = Path.GetFullPath(absolutePath).Replace('\\', '/');
            return abs.StartsWith(proj + "/Assets", StringComparison.OrdinalIgnoreCase);
        }

        private static string AbsoluteToProjectRelative(string absolutePath)
        {
            var proj = Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/');
            var abs  = Path.GetFullPath(absolutePath).Replace('\\', '/');
            if (abs.StartsWith(proj, StringComparison.OrdinalIgnoreCase))
            {
                var rel = abs.Substring(proj.Length + 1); // +1 for slash
                return rel.Replace('\\', '/');
            }
            return absolutePath; // 프로젝트 외부면 그대로
        }
        #endregion
    }
}
#endif
