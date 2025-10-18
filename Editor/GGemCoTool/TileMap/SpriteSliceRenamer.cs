// Unity 2022 LTS ~ Unity 6
// 경고 제거: TextureImporter.spritesheet 미사용 → UnityEditor.U2D.Sprites.ISpriteEditorDataProvider 사용
// 입력: cellWidth, cellHeight, frameCols, frameRows, framesAcross, frameStartIndex
// 의미: framesAcross = 한 행에 배치된 "프레임 세트"의 개수(좌→우). 각 세트는 frameCols x frameRows 셀 크기의 한 "프레임".
// 이름 결과: BaseName_row_col_frame  (frame은 항상 0부터, frameStartIndex를 기준으로 원형 재매핑)

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 슬라이스된 스프라이트들의 이름을 일괄 변환하는 EditorWindow.
    /// <para>
    /// <see cref="ISpriteEditorDataProvider"/> 를 통해 SpriteRect 메타데이터를 직접 수정하며,
    /// "BaseName_row_col_frame" 규칙으로 재명명합니다.
    /// </para>
    /// <remarks>
    /// - 텍스처 좌표계는 좌하(0,0) 기준이며, 상→하 스캔을 위해 TL기준 보정 로직을 사용합니다.<br/>
    /// - Unity 공식 문서: Sprite Editor Data Provider API를 참고하세요.
    /// </remarks>
    /// </summary>
    internal class SpriteSliceRenamer : EditorWindow
    {
        private const string Title = "스프라이트 이름정리기";
        private const string Prefs = "GGemCo_SliceRenamer_Min_DP_";

        // ---- Targets ----
        [Header("Targets")]
        [SerializeField] private List<Texture2D> textures = new();

        // ---- Naming ----
        [Header("Naming")]
        [Tooltip("비우면 텍스처 파일명 사용")]
        [SerializeField] private string baseNameOverride = "";

        // ---- Required Inputs ----
        [Header("Required Inputs")]
        [Tooltip("셀(타일) 가로 픽셀")]
        [SerializeField] private int cellWidth = 16;
        [Tooltip("셀(타일) 세로 픽셀")]
        [SerializeField] private int cellHeight = 16;

        [Tooltip("한 프레임이 차지하는 셀의 가로 개수 (예: 3)")]
        [SerializeField] private int frameCols = 3;
        [Tooltip("한 프레임이 차지하는 셀의 세로 개수 (예: 4)")]
        [SerializeField] private int frameRows = 4;

        [Tooltip("한 행(row)에 배치된 프레임 세트 개수 (좌→우)")]
        [SerializeField] private int framesAcross = 8;

        private int _frameStartIndex = 0;
        private bool _strict = true;
        private Vector2 _scroll;
        private string _log = "";
        private Vector2 _scrollTextAreaPos;

        /// <summary>
        /// 툴 메인 메뉴에서 창을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolSpriteSliceRenamer, false, (int)ConfigEditor.ToolOrdering.SpriteSliceRenamer)]
        public static void Open()
        {
            var win = GetWindow<SpriteSliceRenamer>(Title);
            win.minSize = new Vector2(680, 500);
            win.LoadPrefs();
            win.TryInitFromSelection();
            win.Show();
        }

        /// <summary>
        /// Assets 컨텍스트 메뉴 유효성 검사. 선택 항목 중 <see cref="Texture2D"/> 가 있어야 활성화됩니다.
        /// </summary>
        private static bool ValidateContext() => Selection.objects.OfType<Texture2D>().Any();

        /// <summary>
        /// Assets 컨텍스트 메뉴에서 실행 시, 선택한 Texture2D들을 대상으로 창을 엽니다.
        /// </summary>
        [MenuItem("Assets/GGemCo/Rename Sliced Sprites")]
        private static void Context()
        {
            var win = GetWindow<SpriteSliceRenamer>(Title);
            win.minSize = new Vector2(680, 500);
            win.LoadPrefs();
            win.textures = Selection.objects.OfType<Texture2D>().Distinct().ToList();
            win.Show();
        }

        /// <summary>
        /// 에디터 윈도우 GUI 렌더링 루프.
        /// </summary>
        private void OnGUI()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            EditorGUILayout.LabelField("Sprite Slice Renamer (Minimal, DataProvider)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ISpriteEditorDataProvider 기반으로 슬라이스 이름을 `name_row_col_frame` 형식으로 일괄 변경합니다.\n" +
                "- framesAcross = 한 행에 배치된 '프레임 세트' 개수(좌→우)\n" +
                "- frame은 항상 0부터 시작(원형 재매핑), frameStartIndex는 '시작 raw 인덱스'를 의미",
                MessageType.Info);

            DrawTargets();
            DrawInputs();
            // DrawMisc();
            DrawActions();
            DrawLog();
            EditorGUILayout.Space(20);
        }

        /// <summary>
        /// 대상 텍스처 목록과 BaseName 입력 UI를 그립니다.
        /// </summary>
        private void DrawTargets()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);

            int removeIdx = -1;
            for (int i = 0; i < textures.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                textures[i] = (Texture2D)EditorGUILayout.ObjectField($"Texture {i}", textures[i], typeof(Texture2D), false);
                if (GUILayout.Button("X", GUILayout.Width(24))) removeIdx = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIdx >= 0) textures.RemoveAt(removeIdx);

            if (GUILayout.Button("Add Selected Textures"))
            {
                foreach (var t in Selection.objects.OfType<Texture2D>())
                    if (!textures.Contains(t)) textures.Add(t);
            }

            if (textures.Count == 0)
                EditorGUILayout.HelpBox("Texture2D를 추가하세요. (Sprite Mode=Multiple, Slice 완료)", MessageType.Warning);

            EditorGUILayout.Space(6);
            baseNameOverride = EditorGUILayout.TextField(new GUIContent("Base Name Override", "비우면 파일명 사용"), baseNameOverride);
        }

        /// <summary>
        /// 프레임/셀 관련 필수 입력 파라미터 UI를 그립니다.
        /// </summary>
        private void DrawInputs()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Required Inputs", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            cellWidth  = EditorGUILayout.IntField(new GUIContent("Cell Width (px)"),  cellWidth);
            cellHeight = EditorGUILayout.IntField(new GUIContent("Cell Height (px)"), cellHeight);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            frameCols = EditorGUILayout.IntField(new GUIContent("Frame Cols (cells)"), frameCols);
            frameRows = EditorGUILayout.IntField(new GUIContent("Frame Rows (cells)"), frameRows);
            EditorGUILayout.EndHorizontal();

            framesAcross    = EditorGUILayout.IntField(new GUIContent("Frames Across (frames/row)"), framesAcross);
            // _frameStartIndex = EditorGUILayout.IntField(new GUIContent("Frame Start Index (raw)"), _frameStartIndex);
        }

        /// <summary>
        /// 기타(Strict 모드 등) 설정 UI를 그립니다. (현재 비활성화)
        /// </summary>
        private void DrawMisc()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
            _strict = EditorGUILayout.Toggle(new GUIContent("Strict Mode", "Dry Run 실패가 있으면 Apply에서 오류 처리"), _strict);
        }

        /// <summary>
        /// Dry Run / Apply 실행 버튼 UI를 그립니다.
        /// </summary>
        private void DrawActions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dry Run")) { SavePrefs(); DoDryRun(); }
            if (GUILayout.Button("Apply (Rename)")) { SavePrefs(); DoApply(); }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 결과 미리보기 및 로그 텍스트 영역을 렌더링합니다.
        /// </summary>
        private void DrawLog()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview / Log", EditorStyles.boldLabel);
            var style = new GUIStyle(EditorStyles.textArea) { wordWrap = false };
            _scrollTextAreaPos = EditorGUILayout.BeginScrollView(_scrollTextAreaPos, GUILayout.Height(200));
            _log = EditorGUILayout.TextArea(_log, style, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 창 최초 열림 시 현재 선택된 <see cref="Texture2D"/> 들로 타겟을 초기화합니다.
        /// </summary>
        private void TryInitFromSelection()
        {
            if (textures.Count > 0) return;
            textures = Selection.objects.OfType<Texture2D>().Distinct().ToList();
        }

        /// <summary>
        /// 현재 설정값을 <see cref="EditorPrefs"/> 에 저장합니다.
        /// </summary>
        private void SavePrefs()
        {
            EditorPrefs.SetString(Prefs + "baseNameOverride", baseNameOverride);
            EditorPrefs.SetInt(Prefs + "cellWidth", cellWidth);
            EditorPrefs.SetInt(Prefs + "cellHeight", cellHeight);
            EditorPrefs.SetInt(Prefs + "frameCols", frameCols);
            EditorPrefs.SetInt(Prefs + "frameRows", frameRows);
            EditorPrefs.SetInt(Prefs + "framesAcross", framesAcross);
        }

        /// <summary>
        /// <see cref="EditorPrefs"/> 에 저장된 설정값을 불러옵니다.
        /// </summary>
        private void LoadPrefs()
        {
            baseNameOverride = EditorPrefs.GetString(Prefs + "baseNameOverride", "");
            cellWidth        = EditorPrefs.GetInt(Prefs + "cellWidth", 16);
            cellHeight       = EditorPrefs.GetInt(Prefs + "cellHeight", 16);
            frameCols        = EditorPrefs.GetInt(Prefs + "frameCols", 3);
            frameRows        = EditorPrefs.GetInt(Prefs + "frameRows", 4);
            framesAcross     = EditorPrefs.GetInt(Prefs + "framesAcross", 8);
        }

        // =====================================================================
        // Core (DataProvider 기반)
        // =====================================================================

        /// <summary>
        /// 실제 변경 없이 인덱싱/이름 매핑 결과를 로그로 미리 확인합니다.
        /// </summary>
        private void DoDryRun()
        {
            _log = "";
            foreach (var tex in textures.Where(t => t != null))
            {
                if (!TryGetProvider(tex, out var provider))
                {
                    Append($"[WARN] DataProvider 초기화 실패: {tex.name}");
                    continue;
                }

                var rects = provider.GetSpriteRects(); // IReadOnlyList<SpriteRect>
                if (rects == null || rects.Length == 0) { Append($"[WARN] No slices: {AssetDatabase.GetAssetPath(tex)}"); continue; }

                int frameW = frameCols * cellWidth;
                int frameH = frameRows * cellHeight;

                // 1) 아틀라스 영역(bounding box) 추정
                var atlas = ComputeAtlasBounds(rects);

                // 2) 인덱싱 & 고유 raw 프레임 추출
                var uniqueRaw = new SortedSet<int>();
                var preview   = new List<string>();
                int ok = 0, fail = 0;

                foreach (var sr in rects)
                {
                    string oldName = sr.name;
                    
                    if (TryIndex(sr.rect, atlas, frameW, frameH, out int fx, out int fy, out int row, out int col))
                    {
                        int raw = fy * framesAcross + fx; // 좌→우, 상→하
                        uniqueRaw.Add(raw);
                        preview.Add($"    … {oldName} -> {baseNameOverride}_{row}_{col}_{raw}");
                        ok++;
                    }
                    else
                    {
                        preview.Add($"    ✗ {baseNameOverride} (rect {RectStr(sr.rect)})");
                        fail++;
                    }
                }

                // 3) 재매핑 테이블 (frameStartIndex → 0)
                var map = BuildFrameRemap(uniqueRaw, _frameStartIndex, out int usedCount, out string warn);
                if (!string.IsNullOrEmpty(warn)) Append($"[WARN] {warn}");

                string baseName = string.IsNullOrEmpty(baseNameOverride) ? tex.name : baseNameOverride;
                var p = AssetDatabase.GetAssetPath(tex);
                Append($"[FILE] {p}");
                Append($"  - Atlas: {atlas.size.x}x{atlas.size.y} at ({atlas.x},{atlas.y})  FrameSize: {frameW}x{frameH}");
                Append($"  - framesAcross: {framesAcross}, uniqueRawFrames: {usedCount}, startRaw: {_frameStartIndex} → label 0");
                foreach (var line in preview) Append(line);
                Append($"  - Result: OK={ok}, Fail={fail}");
                if (_strict && fail > 0) Append("  - STRICT: Apply 시 이 파일은 오류 처리됩니다.");
            }
        }

        /// <summary>
        /// 실제로 SpriteRect 이름을 갱신하고, Import를 강제하여 변경 사항을 저장합니다.
        /// <remarks>
        /// Strict 모드에서 실패 항목이 있으면 해당 파일은 갱신을 중단합니다.
        /// </remarks>
        /// </summary>
        private void DoApply()
        {
            _log = "";
            foreach (var tex in textures.Where(t => t != null))
            {
                if (!TryGetProvider(tex, out var provider))
                {
                    Append($"[WARN] DataProvider 초기화 실패: {tex.name}");
                    continue;
                }

                var rects = provider.GetSpriteRects();
                if (rects == null || rects.Length == 0) { Append($"[WARN] No slices: {AssetDatabase.GetAssetPath(tex)}"); continue; }

                int frameW = frameCols * cellWidth;
                int frameH = frameRows * cellHeight;

                var atlas = ComputeAtlasBounds(rects);

                // 고유 raw 프레임 수집
                var uniqueRaw = new SortedSet<int>();
                int failScan = 0;
                foreach (var sr in rects)
                {
                    if (!TryIndex(sr.rect, atlas, frameW, frameH, out int fx0, out int fy0, out _, out _))
                    {
                        failScan++;
                        continue;
                    }

                    int raw = fy0 * framesAcross + fx0; // 좌→우, 상→하
                    uniqueRaw.Add(raw);
                }

                var map = BuildFrameRemap(uniqueRaw, _frameStartIndex, out int usedCount, out string warn);
                if (!string.IsNullOrEmpty(warn)) Append($"[WARN] {warn}");

                string baseName = string.IsNullOrEmpty(baseNameOverride) ? tex.name : baseNameOverride;

                int ok = 0, fail = 0;
                for (int i = 0; i < rects.Length; i++)
                {
                    var sr = rects[i];
                    if (!TryIndex(sr.rect, atlas, frameW, frameH, out int fx, out int fy, out int row, out int col))
                    {
                        fail++;
                        continue;
                    }

                    int raw = fy * framesAcross + fx;
                    if (!map.TryGetValue(raw, out int newFrame))
                    {
                        fail++;
                        continue;
                    }
                    if (raw >= framesAcross) continue;

                    sr.name = $"{baseName}_{row}_{col}_{newFrame}";
                    rects[i] = sr; // 값형 구조체라 재할당 필요
                    ok++;
                }

                if (_strict && fail > 0)
                {
                    Append($"[ERROR] STRICT: 실패 {fail}개 — 적용 중단: {AssetDatabase.GetAssetPath(tex)}");
                    continue;
                }

                // 변경 반영
                provider.SetSpriteRects(rects);
                provider.Apply(); // 변경 저장
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tex), ImportAssetOptions.ForceUpdate);

                Append($"[APPLY] {AssetDatabase.GetAssetPath(tex)} — Renamed OK={ok}, Fail={fail}, UniqueRaw={usedCount}, StartRaw={_frameStartIndex}");
            }
        }

        // ---------- DataProvider 도우미 ----------

        /// <summary>
        /// 주어진 Texture2D에서 <see cref="ISpriteEditorDataProvider"/> 를 초기화합니다.
        /// </summary>
        /// <param name="tex">대상 텍스처</param>
        /// <param name="provider">초기화된 데이터 프로바이더 (성공 시)</param>
        /// <returns>성공 여부</returns>
        private static bool TryGetProvider(Texture2D tex, out ISpriteEditorDataProvider provider)
        {
            var factories = new SpriteDataProviderFactories();
            provider = factories.GetSpriteEditorDataProviderFromObject(tex);
            if (provider == null) return false;

            provider.InitSpriteEditorDataProvider();
            return true;
        }

        /// <summary>
        /// SpriteRect들의 경계 박스(bounding box)를 계산합니다.
        /// </summary>
        /// <param name="rects">슬라이스된 스프라이트 사각형 목록</param>
        /// <returns>텍스처 좌하 기준의 경계 영역</returns>
        private static BoundsInt ComputeAtlasBounds(IReadOnlyList<SpriteRect> rects)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var sr in rects)
            {
                var r = sr.rect;
                int x0 = Mathf.RoundToInt(r.x);
                int y0 = Mathf.RoundToInt(r.y);
                int x1 = Mathf.RoundToInt(r.x + r.width);
                int y1 = Mathf.RoundToInt(r.y + r.height);

                if (x0 < minX) minX = x0;
                if (y0 < minY) minY = y0;
                if (x1 > maxX) maxX = x1;
                if (y1 > maxY) maxY = y1;
            }

            return new BoundsInt(minX, minY, 0, maxX - minX, maxY - minY, 1);
        }

        /// <summary>
        /// 단일 <see cref="Rect"/> 가 어느 프레임(fx, fy)과 프레임 내부 셀(row, col)에 속하는지 계산합니다.
        /// </summary>
        /// <param name="r">대상 사각형(SpriteRect.rect)</param>
        /// <param name="atlas">사각형 전체의 경계 박스</param>
        /// <param name="frameW">프레임 가로 픽셀(= frameCols * cellWidth)</param>
        /// <param name="frameH">프레임 세로 픽셀(= frameRows * cellHeight)</param>
        /// <param name="fx">프레임 X 인덱스(좌→우)</param>
        /// <param name="fy">프레임 Y 인덱스(상→하)</param>
        /// <param name="row">프레임 내부 셀의 행 인덱스(Top-Left 기준)</param>
        /// <param name="col">프레임 내부 셀의 열 인덱스(Left 기준)</param>
        /// <returns>인덱싱 성공 여부</returns>
        private bool TryIndex(Rect r, BoundsInt atlas, int frameW, int frameH,
                              out int fx, out int fy, out int row, out int col)
        {
            float rx = r.x - atlas.x;
            float ry = r.y - atlas.y;

            if (rx < 0f || ry < 0f || rx + r.width > atlas.size.x || ry + r.height > atlas.size.y)
            { fx = fy = row = col = -1; return false; }

            // 프레임 인덱스(Top-Left 기준 fy)
            float topY = atlas.size.y - (ry + r.height);
            fx = Mathf.FloorToInt((rx + 0.0001f) / frameW);
            fy = Mathf.FloorToInt((topY + 0.0001f) / frameH);
            if (fx < 0 || fy < 0) { row = col = -1; return false; }

            // 프레임 내부 로컬 → 셀 인덱스
            float localX = rx - fx * frameW;
            float localYbl = ry - (atlas.size.y - ((fy + 1) * frameH));  // BL 기준
            float localYtl = frameH - (localYbl + r.height);            // TL 기준 row

            col = Mathf.FloorToInt((localX + 0.0001f) / cellWidth);
            row = Mathf.FloorToInt((localYtl + 0.0001f) / cellHeight);

            if (col < 0 || row < 0 || col >= frameCols || row >= frameRows)
            { fx = fy = row = col = -1; return false; }

            return true;
        }

        /// <summary>
        /// 고유 raw 프레임 집합(좌→우, 상→하 인덱스)을 기준으로,
        /// <paramref name="startRaw"/> 부터 0,1,2… 로 원형 재매핑 테이블을 생성합니다.
        /// </summary>
        /// <param name="uniqueRawFrames">정렬된 고유 raw 프레임 집합</param>
        /// <param name="startRaw">시작 raw 인덱스</param>
        /// <param name="count">사용된 고유 프레임 수</param>
        /// <param name="warn">경고 메시지(필요 시)</param>
        /// <returns>raw → 재라벨(new frame index) 매핑</returns>
        private static Dictionary<int, int> BuildFrameRemap(SortedSet<int> uniqueRawFrames, int startRaw,
                                                            out int count, out string warn)
        {
            warn = "";
            var map = new Dictionary<int, int>();
            var raws = uniqueRawFrames.ToList();
            count = raws.Count;
            if (count == 0) { warn = "유효한 raw 프레임이 없습니다."; return map; }

            int startIdx = raws.IndexOf(startRaw);
            if (startIdx < 0)
            {
                warn = $"frameStartIndex({startRaw})에 해당하는 raw 프레임이 없습니다. 가장 작은 raw({raws[0]})를 0으로 사용합니다.";
                startIdx = 0;
            }

            int label = 0;
            for (int i = 0; i < count; i++)
            {
                int raw = raws[(startIdx + i) % count];
                map[raw] = label++;
            }
            return map;
        }

        /// <summary>
        /// 내부 로그 버퍼에 한 줄을 추가합니다.
        /// </summary>
        /// <param name="line">추가할 문자열</param>
        private void Append(string line) => _log += (line + "\n");

        /// <summary>
        /// <see cref="Rect"/> 정보를 간단한 문자열로 변환합니다.
        /// </summary>
        /// <param name="r">대상 사각형</param>
        /// <returns>좌표/크기 문자열</returns>
        private static string RectStr(Rect r) => $"({r.x},{r.y},{r.width},{r.height})";
    }
}
#endif
