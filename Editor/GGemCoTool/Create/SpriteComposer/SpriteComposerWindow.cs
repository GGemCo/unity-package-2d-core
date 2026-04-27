using System;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Hierarchy에서 선택한 여러 SpriteRenderer와 UGUI Image 오브젝트를 하나의 Sprite 에셋으로 합성하는 EditorWindow입니다.
    /// </summary>
    internal sealed class SpriteComposerWindow : EditorWindow
    {
        /// <summary>
        /// 출력 및 렌더링 옵션입니다.
        /// </summary>
        private readonly SpriteComposerSettings _settings = new SpriteComposerSettings();

        /// <summary>
        /// 현재 Hierarchy 선택을 기준으로 수집된 합성 대상 정보입니다.
        /// </summary>
        private SpriteComposerSelection _selection;

        /// <summary>
        /// 에디터 창 스크롤 위치입니다.
        /// </summary>
        private Vector2 _scrollPosition;

        /// <summary>
        /// 마지막으로 생성한 미리보기 결과입니다.
        /// </summary>
        private SpriteComposerRenderResult _previewResult;

        /// <summary>
        /// Sprite Composer 창을 Unity 메뉴에 등록하고 표시합니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolCreateSpriteComposer, false, (int)ConfigEditor.ToolOrdering.CreateSpriteComposer)]
        public static void Open()
        {
            var window = GetWindow<SpriteComposerWindow>();
            window.titleContent = new GUIContent("Sprite Composer");
            window.minSize = new Vector2(420f, 540f);
            window.Show();
        }

        /// <summary>
        /// 창이 활성화될 때 현재 선택 상태를 수집합니다.
        /// </summary>
        private void OnEnable()
        {
            RefreshSelection();
        }

        /// <summary>
        /// 창이 닫힐 때 미리보기 텍스처를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            ClearPreview();
        }

        /// <summary>
        /// Hierarchy 선택이 변경되면 합성 대상 정보를 다시 수집합니다.
        /// </summary>
        private void OnSelectionChange()
        {
            RefreshSelection();
            Repaint();
        }

        /// <summary>
        /// Sprite Composer 에디터 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawHeader();
            DrawSelectionInfo();
            DrawSettings();
            DrawActions();
            DrawPreview();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 창 상단 제목과 사용 범위 안내를 표시합니다.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Sprite Composer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Hierarchy에서 선택한 SpriteRenderer와 UGUI Image 오브젝트들을 투명 배경 PNG로 렌더링한 뒤 Sprite로 Import합니다. UI Image는 임시 World Space Canvas로 복제해 함께 렌더링합니다.", MessageType.Info);
        }

        /// <summary>
        /// 현재 선택된 루트 오브젝트와 합성 가능한 SpriteRenderer, UGUI Image 개수를 표시합니다.
        /// </summary>
        private void DrawSelectionInfo()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("선택 정보", EditorStyles.boldLabel);

            var rootCount = _selection != null && _selection.Roots != null ? _selection.Roots.Length : 0;
            var rendererCount = _selection != null && _selection.Renderers != null ? _selection.Renderers.Length : 0;
            var imageCount = _selection != null && _selection.Images != null ? _selection.Images.Length : 0;
            EditorGUILayout.LabelField("선택 루트", rootCount.ToString());
            EditorGUILayout.LabelField("합성 대상 SpriteRenderer", rendererCount.ToString());
            EditorGUILayout.LabelField("합성 대상 UI Image", imageCount.ToString());

            if (_selection == null || !_selection.HasRenderableItems)
            {
                EditorGUILayout.HelpBox("SpriteRenderer 또는 UGUI Image와 Sprite가 포함된 GameObject를 Hierarchy에서 선택해주세요.", MessageType.Warning);
            }
        }

        /// <summary>
        /// 출력 폴더, 파일 이름, 해상도 등 사용자 설정 필드를 표시합니다.
        /// </summary>
        private void DrawSettings()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("출력 설정", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            DrawOutputFolderField();
            _settings.FileName = EditorGUILayout.TextField("파일 이름", _settings.FileName);
            _settings.PixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", _settings.PixelsPerUnit);
            _settings.Padding = EditorGUILayout.IntField("Padding", _settings.Padding);
            _settings.MaxTextureSize = EditorGUILayout.IntPopup("Max Texture Size", _settings.MaxTextureSize, new[] { "1024", "2048", "4096", "8192" }, new[] { 1024, 2048, 4096, 8192 });
            _settings.AntiAliasing = EditorGUILayout.IntPopup("Anti Aliasing", _settings.AntiAliasing, new[] { "1", "2", "4", "8" }, new[] { 1, 2, 4, 8 });
            _settings.FilterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", _settings.FilterMode);
            _settings.IncludeInactive = EditorGUILayout.Toggle("비활성 포함", _settings.IncludeInactive);
            _settings.OverwriteExisting = EditorGUILayout.Toggle("같은 이름 덮어쓰기", _settings.OverwriteExisting);

            if (EditorGUI.EndChangeCheck())
            {
                _settings.Normalize();
                RefreshSelection();
                ClearPreview();
            }
        }

        /// <summary>
        /// 출력 폴더 입력 필드와 폴더 선택 버튼을 표시합니다.
        /// </summary>
        private void DrawOutputFolderField()
        {
            EditorGUILayout.BeginHorizontal();
            _settings.OutputFolder = EditorGUILayout.TextField("출력 폴더", _settings.OutputFolder);
            if (GUILayout.Button("선택", GUILayout.Width(56f)))
            {
                var selectedFolder = EditorUtility.OpenFolderPanel("출력 폴더 선택", Application.dataPath, string.Empty);
                string assetFolderPath;
                if (SpriteComposerAssetWriter.TryConvertToAssetFolderPath(selectedFolder, out assetFolderPath))
                {
                    _settings.OutputFolder = assetFolderPath;
                }
                else if (!string.IsNullOrEmpty(selectedFolder))
                {
                    EditorUtility.DisplayDialog("Sprite Composer", "출력 폴더는 현재 Unity 프로젝트의 Assets 폴더 하위여야 합니다.", "확인");
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 선택 새로고침, 미리보기, Sprite 생성 버튼을 표시합니다.
        /// </summary>
        private void DrawActions()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("선택 새로고침", GUILayout.Height(28f)))
            {
                RefreshSelection();
                ClearPreview();
            }

            using (new EditorGUI.DisabledScope(_selection == null || !_selection.HasRenderableItems))
            {
                if (GUILayout.Button("미리보기 생성", GUILayout.Height(28f)))
                {
                    GeneratePreview();
                }

                if (GUILayout.Button("Sprite 생성", GUILayout.Height(28f)))
                {
                    GenerateSpriteAsset();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 생성된 미리보기 텍스처와 렌더링 정보를 표시합니다.
        /// </summary>
        private void DrawPreview()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);

            if (_previewResult == null || _previewResult.Texture == null)
            {
                EditorGUILayout.HelpBox("미리보기를 생성하면 이 영역에 합성 결과가 표시됩니다.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("출력 크기", _previewResult.Width + " x " + _previewResult.Height);
            EditorGUILayout.LabelField("실제 PPU", _previewResult.EffectivePixelsPerUnit.ToString("0.###"));

            var aspect = _previewResult.Width / Mathf.Max(1f, (float)_previewResult.Height);
            var previewRect = GUILayoutUtility.GetAspectRect(aspect, GUILayout.ExpandWidth(true));
            previewRect.height = Mathf.Min(previewRect.height, 360f);
            EditorGUI.DrawPreviewTexture(previewRect, _previewResult.Texture, null, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// 현재 Hierarchy 선택에서 합성 대상을 다시 수집합니다.
        /// </summary>
        private void RefreshSelection()
        {
            _selection = SpriteComposerSelectionCollector.CollectCurrentSelection(_settings.IncludeInactive);
        }

        /// <summary>
        /// 현재 선택과 설정을 기준으로 미리보기 텍스처를 생성합니다.
        /// </summary>
        private void GeneratePreview()
        {
            try
            {
                ClearPreview();
                EditorUtility.DisplayProgressBar("Sprite Composer", "미리보기를 생성하는 중입니다.", 0.5f);
                _previewResult = SpriteComposerRenderService.Render(_selection, _settings);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Sprite Composer", exception.Message, "확인");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 현재 선택과 설정을 기준으로 PNG 파일을 저장하고 Sprite Import 설정을 적용합니다.
        /// </summary>
        private void GenerateSpriteAsset()
        {
            SpriteComposerRenderResult renderResult = null;
            try
            {
                EditorUtility.DisplayProgressBar("Sprite Composer", "Sprite 에셋을 생성하는 중입니다.", 0.5f);
                renderResult = SpriteComposerRenderService.Render(_selection, _settings);
                var assetPath = SpriteComposerAssetWriter.SaveTextureAsSprite(renderResult.Texture, _settings, renderResult.EffectivePixelsPerUnit);
                var createdAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (createdAsset != null)
                {
                    Selection.activeObject = createdAsset;
                    EditorGUIUtility.PingObject(createdAsset);
                }

                EditorUtility.DisplayDialog("Sprite Composer", "Sprite 에셋을 생성했습니다.\n" + assetPath, "확인");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Sprite Composer", exception.Message, "확인");
            }
            finally
            {
                if (renderResult != null)
                {
                    renderResult.Dispose();
                }

                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 기존 미리보기 결과를 정리합니다.
        /// </summary>
        private void ClearPreview()
        {
            if (_previewResult == null)
            {
                return;
            }

            _previewResult.Dispose();
            _previewResult = null;
        }
    }
}
