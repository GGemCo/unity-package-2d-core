using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="UIEffectPreset"/>을 생성/편집하고 간단한 미리보기를 제공하는 에디터 툴입니다.
    /// </summary>
    public sealed class CreateUIEffectPresetWindow : EditorWindow
    {
        private const string Title = "UI 효과 프리셋";
        private const float PreviewBaseWidth = 120f;
        private const float PreviewBaseHeight = 56f;
        private const float PreviewPadding = 18f;

        private UIEffectPreset _preset;
        private SerializedObject _serializedObject;
        private Vector2 _scrollPosition;

        private bool _autoPlay = true;
        private bool _loopPreview = true;
        private bool _previewPlaying;
        private double _previewStartTime;
        private float _previewDuration = 0.2f;

        private bool _showGeneral = true;
        private bool _showFade = true;
        private bool _showMove = true;
        private bool _showScale = true;
        private bool _showPunch = true;
        private bool _showShake = true;
        private bool _showFlash = true;

        [MenuItem(ConfigEditor.NameToolCreateUIEffectPreset, false, (int)ConfigEditor.ToolOrdering.CreateUIEffectPreset)]
        public static void ShowWindow()
        {
            var window = GetWindow<CreateUIEffectPresetWindow>(Title);
            window.minSize = new Vector2(720f, 720f);
            window.Focus();
        }

        /// <summary>
        /// 외부에서 특정 프리셋을 바로 열 때 사용합니다.
        /// </summary>
        public static void Open(UIEffectPreset preset)
        {
            var window = GetWindow<CreateUIEffectPresetWindow>(Title);
            window.minSize = new Vector2(720f, 720f);
            window.SetPreset(preset);
            window.Focus();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            if (_preset == null)
            {
                TryAssignPresetFromSelection();
            }

            SyncSerializedObject();
            RestartPreview();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnSelectionChange()
        {
            if (_preset != null)
            {
                return;
            }

            if (TryAssignPresetFromSelection())
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);

            if (_preset == null)
            {
                DrawEmptyState();
                return;
            }

            SyncSerializedObject();
            _serializedObject.Update();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawPresetSummary();
            DrawPreviewSection();
            DrawGeneralSection();
            DrawFadeSection();
            DrawMoveSection();
            DrawScaleSection();
            DrawPunchSection();
            DrawShakeSection();
            DrawFlashSection();
            EditorGUILayout.EndScrollView();

            if (_serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_preset);
                RefreshPreviewDuration();
                if (_autoPlay)
                {
                    RestartPreview();
                }
                else
                {
                    Repaint();
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("새 프리셋", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    CreateNewPreset();
                }

                using (new EditorGUI.DisabledScope(_preset == null))
                {
                    if (GUILayout.Button("복제", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                    {
                        DuplicatePreset();
                    }

                    if (GUILayout.Button("선택", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                    {
                        Selection.activeObject = _preset;
                        EditorGUIUtility.PingObject(_preset);
                    }

                    if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                    {
                        SavePreset();
                    }
                }

                GUILayout.FlexibleSpace();

                var nextPreset = (UIEffectPreset)EditorGUILayout.ObjectField(
                    _preset,
                    typeof(UIEffectPreset),
                    false,
                    GUILayout.Width(260f));

                if (nextPreset != _preset)
                {
                    SetPreset(nextPreset);
                }
            }
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.HelpBox(
                "편집할 UIEffectPreset 에셋을 선택하거나, 상단의 '새 프리셋' 버튼으로 새 에셋을 생성해주세요.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택된 프리셋 불러오기", GUILayout.Height(28f)))
                {
                    TryAssignPresetFromSelection();
                }

                if (GUILayout.Button("새 프리셋 만들기", GUILayout.Height(28f)))
                {
                    CreateNewPreset();
                }
            }
        }

        private void DrawPresetSummary()
        {
            string assetPath = AssetDatabase.GetAssetPath(_preset);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Name", _preset.name);
                EditorGUILayout.LabelField("Path", string.IsNullOrEmpty(assetPath) ? "(임시 인스턴스)" : assetPath);
            }
        }

        private void DrawPreviewSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool nextAutoPlay = EditorGUILayout.ToggleLeft("값 변경 시 자동 재생", _autoPlay, GUILayout.Width(120f));
                    if (nextAutoPlay != _autoPlay)
                    {
                        _autoPlay = nextAutoPlay;
                        if (_autoPlay)
                        {
                            RestartPreview();
                        }
                    }

                    _loopPreview = EditorGUILayout.ToggleLeft("루프", _loopPreview, GUILayout.Width(60f));
                    _previewPlaying = EditorGUILayout.ToggleLeft("재생", _previewPlaying, GUILayout.Width(60f));

                    if (GUILayout.Button("다시 재생", GUILayout.Width(80f)))
                    {
                        RestartPreview();
                    }
                }

                EditorGUILayout.LabelField("예상 재생 길이", $"{_previewDuration:0.###}초");
                Rect previewRect = GUILayoutUtility.GetRect(10f, 220f, GUILayout.ExpandWidth(true));
                DrawPreviewCanvas(previewRect);
            }
        }

        private void DrawGeneralSection()
        {
            _showGeneral = EditorGUILayout.BeginFoldoutHeaderGroup(_showGeneral, "General");
            if (_showGeneral)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawProperty("useUnscaledTime");
                    DrawProperty("channel");
                    DrawProperty("playPolicy");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawFadeSection()
        {
            _showFade = EditorGUILayout.BeginFoldoutHeaderGroup(_showFade, "Fade");
            if (_showFade)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawProperty("useFade");
                    using (new EditorGUI.DisabledScope(!FindProperty("useFade").boolValue))
                    {
                        DrawProperty("fadeStartAlpha");
                        DrawProperty("fadeTargetAlpha");
                        DrawProperty("fadeDuration");
                        DrawProperty("fadeEaseType");
                        DrawProperty("fadeUpdateInteractableOnComplete");
                        DrawProperty("fadeUpdateBlocksRaycastsOnComplete");
                        DrawProperty("fadeDisableInputWhenInvisible");
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawMoveSection()
        {
            _showMove = EditorGUILayout.BeginFoldoutHeaderGroup(_showMove, "Move");
            if (_showMove)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawProperty("useMove");
                    using (new EditorGUI.DisabledScope(!FindProperty("useMove").boolValue))
                    {
                        DrawProperty("moveMode");
                        DrawProperty("moveFromOffset");
                        DrawProperty("moveDuration");
                        DrawProperty("moveEaseType");
                        DrawProperty("moveSnapToTargetOnComplete");
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawScaleSection()
        {
            _showScale = EditorGUILayout.BeginFoldoutHeaderGroup(_showScale, "Scale");
            if (_showScale)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawProperty("useScale");
                    using (new EditorGUI.DisabledScope(!FindProperty("useScale").boolValue))
                    {
                        DrawProperty("scaleFrom");
                        DrawProperty("scaleTo");
                        DrawProperty("scaleDuration");
                        DrawProperty("scaleEaseType");
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPunchSection()
        {
            _showPunch = EditorGUILayout.BeginFoldoutHeaderGroup(_showPunch, "Punch");
            if (_showPunch)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawProperty("usePunchScale");
                    using (new EditorGUI.DisabledScope(!FindProperty("usePunchScale").boolValue))
                    {
                        DrawProperty("punchScale");
                        DrawProperty("punchDuration");
                        DrawProperty("punchEaseType");
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawShakeSection()
        {
            _showShake = EditorGUILayout.BeginFoldoutHeaderGroup(_showShake, "Shake");
            if (_showShake)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawProperty("useShake");
                    using (new EditorGUI.DisabledScope(!FindProperty("useShake").boolValue))
                    {
                        DrawProperty("shakeStrength");
                        DrawProperty("shakeDuration");
                        DrawProperty("shakeVibrato");
                        DrawProperty("shakeDirectionMode");
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawFlashSection()
        {
            _showFlash = EditorGUILayout.BeginFoldoutHeaderGroup(_showFlash, "Flash");
            if (_showFlash)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawProperty("useFlash");
                    using (new EditorGUI.DisabledScope(!FindProperty("useFlash").boolValue))
                    {
                        DrawProperty("flashColor");
                        DrawProperty("flashPeakAlpha");
                        DrawProperty("flashDuration");
                        DrawProperty("flashEaseType");
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPreviewCanvas(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));

            Rect contentRect = new Rect(
                rect.x + PreviewPadding,
                rect.y + PreviewPadding,
                rect.width - (PreviewPadding * 2f),
                rect.height - (PreviewPadding * 2f));

            EditorGUI.DrawRect(contentRect, new Color(0.16f, 0.16f, 0.16f, 1f));

            if (_preset == null)
            {
                return;
            }

            float progress = GetPreviewProgress();
            PreviewFrame frame = EvaluatePreviewFrame(progress);

            Rect baseRect = new Rect(
                contentRect.center.x - (PreviewBaseWidth * 0.5f),
                contentRect.center.y - (PreviewBaseHeight * 0.5f),
                PreviewBaseWidth,
                PreviewBaseHeight);

            Vector2 center = baseRect.center + frame.PositionOffset;
            float width = Mathf.Max(1f, PreviewBaseWidth * frame.Scale.x);
            float height = Mathf.Max(1f, PreviewBaseHeight * frame.Scale.y);
            Rect widgetRect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);

            Color widgetColor = new Color(0.22f, 0.57f, 0.98f, frame.Alpha);
            if (_preset.useFlash)
            {
                widgetColor = Color.LerpUnclamped(widgetColor, frame.FlashColor, frame.FlashWeight);
                widgetColor.a = Mathf.Clamp01(frame.Alpha);
            }

            EditorGUI.DrawRect(widgetRect, widgetColor);
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.08f);
            Handles.DrawSolidRectangleWithOutline(widgetRect, Color.clear, new Color(1f, 1f, 1f, 0.14f));
            Handles.EndGUI();

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, Mathf.Clamp01(frame.Alpha)) }
            };
            GUI.Label(widgetRect, "Preview", labelStyle);
        }

        private PreviewFrame EvaluatePreviewFrame(float progress)
        {
            PreviewFrame frame = PreviewFrame.Default;

            if (_preset.useFade)
            {
                float startAlpha = _preset.fadeStartAlpha >= 0f ? Mathf.Clamp01(_preset.fadeStartAlpha) : 1f;
                float endAlpha = Mathf.Clamp01(_preset.fadeTargetAlpha);
                float eased = EvaluateProgress(progress, _preset.fadeDuration, _preset.fadeEaseType);
                frame.Alpha = Mathf.LerpUnclamped(startAlpha, endAlpha, eased);
            }

            if (_preset.useMove)
            {
                float eased = EvaluateProgress(progress, _preset.moveDuration, _preset.moveEaseType);
                switch (_preset.moveMode)
                {
                    case UIEffectMoveMode.FromBaseToOffset:
                        frame.PositionOffset += Vector2.LerpUnclamped(Vector2.zero, _preset.moveFromOffset, eased);
                        break;

                    case UIEffectMoveMode.FromOffsetToBase:
                    default:
                        frame.PositionOffset += Vector2.LerpUnclamped(_preset.moveFromOffset, Vector2.zero, eased);
                        break;
                }
            }

            if (_preset.useScale)
            {
                float eased = EvaluateProgress(progress, _preset.scaleDuration, _preset.scaleEaseType);
                frame.Scale = Vector3.LerpUnclamped(_preset.scaleFrom, _preset.scaleTo, eased);
            }

            if (_preset.usePunchScale)
            {
                float duration = Mathf.Max(0.0001f, _preset.punchDuration);
                float normalized = Mathf.Clamp01(progress / duration);
                float pingPong = normalized <= 0.5f ? normalized * 2f : (1f - normalized) * 2f;
                float eased = Mathf.Clamp01(Easing.Apply(pingPong, _preset.punchEaseType));
                Vector3 punchScale = Vector3.one + (_preset.punchScale * eased);
                frame.Scale = Vector3.Scale(frame.Scale, punchScale);
            }

            if (_preset.useShake)
            {
                float duration = Mathf.Max(0.0001f, _preset.shakeDuration);
                float normalized = Mathf.Clamp01(progress / duration);
                float attenuation = 1f - normalized;
                int safeVibrato = Mathf.Max(1, _preset.shakeVibrato);
                float angle = normalized * safeVibrato * Mathf.PI * 2f;
                float horizontalSign = ResolvePreviewShakeHorizontalSign(_preset.shakeDirectionMode);
                float x = Mathf.Sin(angle) * _preset.shakeStrength * attenuation * horizontalSign;
                float y = Mathf.Cos(angle * 0.73f) * _preset.shakeStrength * 0.5f * attenuation;
                frame.PositionOffset += new Vector2(x, y);
            }

            if (_preset.useFlash)
            {
                float duration = Mathf.Max(0.0001f, _preset.flashDuration);
                float normalized = Mathf.Clamp01(progress / duration);
                float pingPong = normalized <= 0.5f ? normalized * 2f : (1f - normalized) * 2f;
                frame.FlashWeight = Mathf.Clamp01(Easing.Apply(pingPong, _preset.flashEaseType)) * Mathf.Clamp01(_preset.flashPeakAlpha);
                frame.FlashColor = _preset.flashColor;
            }

            return frame;
        }

        private float EvaluateProgress(float progress, float duration, Easing.EaseType easeType)
        {
            float safeDuration = Mathf.Max(0.0001f, duration);
            float normalized = Mathf.Clamp01(progress / safeDuration);
            return Mathf.Clamp01(Easing.Apply(normalized, easeType));
        }

        private static float ResolvePreviewShakeHorizontalSign(UIEffectShakeDirectionMode directionMode)
        {
            switch (directionMode)
            {
                case UIEffectShakeDirectionMode.Left:
                    return -1f;

                case UIEffectShakeDirectionMode.Right:
                    return 1f;

                case UIEffectShakeDirectionMode.RandomHorizontal:
                default:
                    return 1f;
            }
        }

        private float GetPreviewProgress()
        {
            if (_previewDuration <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)(EditorApplication.timeSinceStartup - _previewStartTime) / _previewDuration);
        }

        private void OnEditorUpdate()
        {
            if (_preset == null || !_previewPlaying)
            {
                return;
            }

            if (_previewDuration <= 0f)
            {
                _previewPlaying = false;
                Repaint();
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - _previewStartTime;
            if (elapsed >= _previewDuration)
            {
                if (_loopPreview)
                {
                    _previewStartTime = EditorApplication.timeSinceStartup;
                }
                else
                {
                    _previewPlaying = false;
                }
            }

            Repaint();
        }

        private void RestartPreview()
        {
            RefreshPreviewDuration();
            _previewStartTime = EditorApplication.timeSinceStartup;
            _previewPlaying = true;
            Repaint();
        }

        private void RefreshPreviewDuration()
        {
            if (_preset == null)
            {
                _previewDuration = 0f;
                return;
            }

            float duration = 0f;
            if (_preset.useFade) duration = Mathf.Max(duration, _preset.fadeDuration);
            if (_preset.useMove) duration = Mathf.Max(duration, _preset.moveDuration);
            if (_preset.useScale) duration = Mathf.Max(duration, _preset.scaleDuration);
            if (_preset.usePunchScale) duration = Mathf.Max(duration, _preset.punchDuration);
            if (_preset.useShake) duration = Mathf.Max(duration, _preset.shakeDuration);
            if (_preset.useFlash) duration = Mathf.Max(duration, _preset.flashDuration);
            _previewDuration = Mathf.Max(0.05f, duration);
        }

        private void CreateNewPreset()
        {
            string initialFolder = GetInitialFolder();
            string path = EditorUtility.SaveFilePanelInProject(
                "UIEffectPreset 생성",
                "UIEffectPreset",
                "asset",
                "UI 효과 프리셋 저장 경로를 선택해주세요.",
                initialFolder);

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            UIEffectPreset created = CreateInstance<UIEffectPreset>();
            ApplyDefaultPreset(created);
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetPreset(created);
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }

        private void DuplicatePreset()
        {
            if (_preset == null)
            {
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(_preset);
            string initialFolder = string.IsNullOrEmpty(sourcePath) ? GetInitialFolder() : Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            string defaultName = $"{_preset.name}_Copy";
            string path = EditorUtility.SaveFilePanelInProject(
                "UIEffectPreset 복제",
                defaultName,
                "asset",
                "복제본 저장 경로를 선택해주세요.",
                initialFolder);

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            UIEffectPreset duplicated = Instantiate(_preset);
            duplicated.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(duplicated, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetPreset(duplicated);
            Selection.activeObject = duplicated;
            EditorGUIUtility.PingObject(duplicated);
        }

        private void SavePreset()
        {
            if (_preset == null)
            {
                return;
            }

            EditorUtility.SetDirty(_preset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SetPreset(UIEffectPreset preset)
        {
            _preset = preset;
            SyncSerializedObject();
            RefreshPreviewDuration();
            if (_preset != null)
            {
                RestartPreview();
            }
            Repaint();
        }

        private void SyncSerializedObject()
        {
            _serializedObject = _preset != null ? new SerializedObject(_preset) : null;
        }

        private bool TryAssignPresetFromSelection()
        {
            if (Selection.activeObject is UIEffectPreset preset)
            {
                SetPreset(preset);
                return true;
            }

            return false;
        }

        private void DrawProperty(string propertyName)
        {
            SerializedProperty property = FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }

        private SerializedProperty FindProperty(string propertyName)
        {
            return _serializedObject?.FindProperty(propertyName);
        }

        private string GetInitialFolder()
        {
            if (_preset != null)
            {
                string currentPath = AssetDatabase.GetAssetPath(_preset);
                if (!string.IsNullOrEmpty(currentPath))
                {
                    return Path.GetDirectoryName(currentPath)?.Replace('\\', '/');
                }
            }

            if (Selection.activeObject != null)
            {
                string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (AssetDatabase.IsValidFolder(selectionPath))
                {
                    return selectionPath;
                }

                if (!string.IsNullOrEmpty(selectionPath))
                {
                    return Path.GetDirectoryName(selectionPath)?.Replace('\\', '/');
                }
            }

            return "Assets";
        }

        private static void ApplyDefaultPreset(UIEffectPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            preset.useUnscaledTime = true;
            preset.channel = UIEffectChannel.Default;
            preset.playPolicy = UIEffectPlayPolicy.StopSameChannelAndPlay;

            preset.useFade = false;
            preset.fadeStartAlpha = -1f;
            preset.fadeTargetAlpha = 1f;
            preset.fadeDuration = 0.2f;
            preset.fadeEaseType = Easing.EaseType.Linear;
            preset.fadeUpdateInteractableOnComplete = true;
            preset.fadeUpdateBlocksRaycastsOnComplete = true;
            preset.fadeDisableInputWhenInvisible = true;

            preset.useMove = false;
            preset.moveMode = UIEffectMoveMode.FromOffsetToBase;
            preset.moveFromOffset = Vector2.zero;
            preset.moveDuration = 0.2f;
            preset.moveEaseType = Easing.EaseType.EaseOutCubic;
            preset.moveSnapToTargetOnComplete = true;

            preset.useScale = false;
            preset.scaleFrom = Vector3.one;
            preset.scaleTo = Vector3.one;
            preset.scaleDuration = 0.15f;
            preset.scaleEaseType = Easing.EaseType.EaseOutCubic;

            preset.usePunchScale = false;
            preset.punchScale = new Vector3(0.08f, 0.08f, 0f);
            preset.punchDuration = 0.15f;
            preset.punchEaseType = Easing.EaseType.EaseOutBack;

            preset.useShake = false;
            preset.shakeStrength = 8f;
            preset.shakeDuration = 0.15f;
            preset.shakeVibrato = 14;
            preset.shakeDirectionMode = UIEffectShakeDirectionMode.RandomHorizontal;

            preset.useFlash = false;
            preset.flashColor = Color.white;
            preset.flashPeakAlpha = 0.8f;
            preset.flashDuration = 0.2f;
            preset.flashEaseType = Easing.EaseType.EaseOutCubic;
        }

        private struct PreviewFrame
        {
            public static PreviewFrame Default => new PreviewFrame
            {
                Alpha = 1f,
                Scale = Vector3.one,
                PositionOffset = Vector2.zero,
                FlashWeight = 0f,
                FlashColor = Color.white,
            };

            public float Alpha;
            public Vector3 Scale;
            public Vector2 PositionOffset;
            public float FlashWeight;
            public Color FlashColor;
        }
    }
}
