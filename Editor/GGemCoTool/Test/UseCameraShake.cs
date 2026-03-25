using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public sealed class UseCameraShake : DefaultEditorWindow
    {
        private const string Title = "카메라 Shake 사용툴";

        private CameraShakePreset _preset;

        [Header("Override")]
        private bool _useOverride = true;
        private float _duration = 0.2f;
        private int _repeatCount = 3;
        private float _leftStrength = 0.1f;
        private float _rightStrength = 0.1f;
        private float _downStrength = 0.05f;
        private float _upStrength = 0.05f;
        private bool _useUnscaledTime;
        private CameraShakeChannel _channel = CameraShakeChannel.Default;

        private Vector2 _scroll;
        private Vector2 _previewScroll;

        [MenuItem(ConfigEditor.NameToolUseCameraShake, false, (int)ConfigEditor.ToolOrdering.UseCameraShake)]
        public static void ShowWindow()
        {
            GetWindow<UseCameraShake>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Camera Shake 테스트", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4);

                    DrawPlayModeGate();
                    EditorGUILayout.Space(6);

                    DrawPresetSection();
                    EditorGUILayout.Space(6);

                    DrawOverrideSection();
                    EditorGUILayout.Space(6);

                    DrawPreviewSection();
                    EditorGUILayout.Space(6);

                    DrawActionSection();
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _preset = (CameraShakePreset)EditorGUILayout.ObjectField("Preset", _preset, typeof(CameraShakePreset), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Selection → Preset"))
                    {
                        _preset = Selection.activeObject as CameraShakePreset;
                    }

                    using (new EditorGUI.DisabledScope(_preset == null))
                    {
                        if (GUILayout.Button("Preset 값으로 Override 채우기"))
                        {
                            CopyPresetToOverride();
                        }
                    }
                }

                if (_preset == null)
                {
                    EditorGUILayout.HelpBox("Preset이 없으면 Override 값으로 직접 실행합니다.", MessageType.Info);
                }
                else if (!string.IsNullOrEmpty(_preset.Description))
                {
                    EditorGUILayout.HelpBox(_preset.Description, MessageType.None);
                }
            }
        }

        private void DrawOverrideSection()
        {
            EditorGUILayout.LabelField("Override", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _useOverride = EditorGUILayout.ToggleLeft("Override 사용", _useOverride);

                using (new EditorGUI.DisabledScope(!_useOverride && _preset != null))
                {
                    _duration = Mathf.Max(0f, EditorGUILayout.FloatField("Duration", _duration));
                    _repeatCount = Mathf.Max(1, EditorGUILayout.IntField("RepeatCount", _repeatCount));
                    _leftStrength = Mathf.Max(0f, EditorGUILayout.FloatField("LeftStrength", _leftStrength));
                    _rightStrength = Mathf.Max(0f, EditorGUILayout.FloatField("RightStrength", _rightStrength));
                    _downStrength = Mathf.Max(0f, EditorGUILayout.FloatField("DownStrength", _downStrength));
                    _upStrength = Mathf.Max(0f, EditorGUILayout.FloatField("UpStrength", _upStrength));
                    _useUnscaledTime = EditorGUILayout.Toggle("UseUnscaledTime", _useUnscaledTime);
                    _channel = (CameraShakeChannel)EditorGUILayout.EnumPopup("Channel", _channel);
                }
            }
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                CameraShakeRequest request = BuildRequest();
                var sb = new StringBuilder();
                sb.AppendLine("[Resolved Camera Shake]");
                sb.AppendLine($"- Preset: {(_preset != null ? _preset.name : "(none)")}");
                sb.AppendLine($"- Duration: {request.Duration}");
                sb.AppendLine($"- RepeatCount: {request.RepeatCount}");
                sb.AppendLine($"- LeftStrength: {request.LeftStrength}");
                sb.AppendLine($"- RightStrength: {request.RightStrength}");
                sb.AppendLine($"- DownStrength: {request.DownStrength}");
                sb.AppendLine($"- UpStrength: {request.UpStrength}");
                sb.AppendLine($"- UseUnscaledTime: {request.UseUnscaledTime}");
                sb.AppendLine($"- Channel: {request.Channel}");
                sb.AppendLine($"- Valid: {request.IsValid}");

                _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.MinHeight(180f));
                EditorGUILayout.TextArea(sb.ToString());
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawActionSection()
        {
            EditorGUILayout.LabelField("실행", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                bool hasManager = TryGetCameraManager(out CameraManager cameraManager);

                using (new EditorGUI.DisabledScope(!hasManager))
                {
                    if (GUILayout.Button("Camera Shake 실행", GUILayout.Height(28)))
                    {
                        var request = BuildRequest();
                        if (!request.IsValid)
                        {
                            EditorUtility.DisplayDialog(Title, "유효한 Shake 값이 아닙니다. Duration/Strength/RepeatCount를 확인해주세요.", "OK");
                            return;
                        }

                        cameraManager.PlayShake(request);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("동일 설정 3회 실행", GUILayout.Height(24)))
                        {
                            var request = BuildRequest();
                            if (!request.IsValid)
                            {
                                EditorUtility.DisplayDialog(Title, "유효한 Shake 값이 아닙니다. Duration/Strength/RepeatCount를 확인해주세요.", "OK");
                                return;
                            }

                            cameraManager.PlayShake(request);
                            cameraManager.PlayShake(request);
                            cameraManager.PlayShake(request);
                        }

                        if (GUILayout.Button("해당 Channel 중지", GUILayout.Height(24)))
                        {
                            cameraManager.StopShake(_channel);
                        }
                    }

                    if (GUILayout.Button("전체 Shake 중지", GUILayout.Height(24)))
                    {
                        cameraManager.StopAllShakes();
                    }
                }

                if (!hasManager)
                {
                    EditorGUILayout.HelpBox("Play Mode + SceneGame + CameraManager가 준비되어야 실행할 수 있습니다.", MessageType.Warning);
                }
            }
        }

        private void CopyPresetToOverride()
        {
            if (_preset == null)
            {
                return;
            }

            _duration = _preset.Duration;
            _repeatCount = _preset.RepeatCount;
            _leftStrength = _preset.LeftStrength;
            _rightStrength = _preset.RightStrength;
            _downStrength = _preset.DownStrength;
            _upStrength = _preset.UpStrength;
            _useUnscaledTime = _preset.UseUnscaledTime;
        }

        private CameraShakeRequest BuildRequest()
        {
            CameraShakeRequest request = _preset != null ? _preset.ToRequest(_channel) : default;

            if (_preset == null || _useOverride)
            {
                request.Duration = Mathf.Max(0f, _duration);
                request.RepeatCount = Mathf.Max(1, _repeatCount);
                request.LeftStrength = Mathf.Max(0f, _leftStrength);
                request.RightStrength = Mathf.Max(0f, _rightStrength);
                request.DownStrength = Mathf.Max(0f, _downStrength);
                request.UpStrength = Mathf.Max(0f, _upStrength);
                request.UseUnscaledTime = _useUnscaledTime;
                request.Channel = _channel;
            }

            return request;
        }

        private static bool TryGetCameraManager(out CameraManager cameraManager)
        {
            cameraManager = null;

            if (!Application.isPlaying)
            {
                return false;
            }

            if (SceneGame.Instance == null)
            {
                return false;
            }

            cameraManager = SceneGame.Instance.cameraManager;
            return cameraManager != null;
        }
    }
}
