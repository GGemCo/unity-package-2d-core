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
        private CameraShakeType _shakeType = CameraShakeType.Common;
        private float _duration = 0.2f;
        private float _strength = 0.1f;
        private Vector2 _axisStrength = new Vector2(1f, 0.5f);
        private int _repeatCount = 3;
        private bool _randomStartPhase = true;
        private bool _useUnscaledTime;
        private CameraShakeDecayMode _decayMode = CameraShakeDecayMode.Linear;
        private AnimationCurve _impulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        private CameraShakeChannel _channel = CameraShakeChannel.Default;

        [Header("Direction")]
        private CameraShakeDirectionSource _directionSource = CameraShakeDirectionSource.Preset;
        private Vector2 _fixedDirection = Vector2.right;
        private bool _horizontalOnly = true;
        private Transform _caster;
        private Transform _target;

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

                    DrawDirectionSection();
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

        /// <summary>
        /// 카메라 Shake 프리셋 선택 영역을 그립니다.
        /// </summary>
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

        /// <summary>
        /// 카메라 Shake 타입, 세기, 감쇠 등 프리셋 대체 값을 편집하는 영역을 그립니다.
        /// </summary>
        private void DrawOverrideSection()
        {
            EditorGUILayout.LabelField("Override", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _useOverride = EditorGUILayout.ToggleLeft("Override 사용", _useOverride);

                using (new EditorGUI.DisabledScope(!_useOverride && _preset != null))
                {
                    _shakeType = (CameraShakeType)EditorGUILayout.EnumPopup("Shake Type", _shakeType);
                    _duration = Mathf.Max(0f, EditorGUILayout.FloatField("Duration", _duration));
                    _strength = Mathf.Max(0f, EditorGUILayout.FloatField("Strength", _strength));
                    _axisStrength = Vector2.Max(Vector2.zero, EditorGUILayout.Vector2Field("Axis Strength", _axisStrength));
                    _repeatCount = Mathf.Max(1, EditorGUILayout.IntField("Repeat Count", _repeatCount));
                    _randomStartPhase = EditorGUILayout.Toggle("Random Start Phase", _randomStartPhase);
                    _useUnscaledTime = EditorGUILayout.Toggle("Use Unscaled Time", _useUnscaledTime);
                    _decayMode = (CameraShakeDecayMode)EditorGUILayout.EnumPopup("Decay Mode", _decayMode);
                    _impulseCurve = EditorGUILayout.CurveField("Impulse Curve", _impulseCurve);
                    _channel = (CameraShakeChannel)EditorGUILayout.EnumPopup("Channel", _channel);
                }
            }
        }

        /// <summary>
        /// 방향성 카메라 Shake 테스트에 필요한 방향 계산 정책을 편집하는 영역을 그립니다.
        /// </summary>
        private void DrawDirectionSection()
        {
            EditorGUILayout.LabelField("Direction", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _directionSource = (CameraShakeDirectionSource)EditorGUILayout.EnumPopup("Direction Source", _directionSource);
                _horizontalOnly = EditorGUILayout.Toggle("Horizontal Only", _horizontalOnly);

                using (new EditorGUI.DisabledScope(_directionSource != CameraShakeDirectionSource.FixedDirection))
                {
                    _fixedDirection = EditorGUILayout.Vector2Field("Fixed Direction", _fixedDirection);
                }

                using (new EditorGUI.DisabledScope(_directionSource != CameraShakeDirectionSource.CasterToTarget && _directionSource != CameraShakeDirectionSource.TargetToCaster))
                {
                    _caster = (Transform)EditorGUILayout.ObjectField("Caster", _caster, typeof(Transform), true);
                    _target = (Transform)EditorGUILayout.ObjectField("Target", _target, typeof(Transform), true);
                }

                if (_directionSource == CameraShakeDirectionSource.Preset)
                {
                    EditorGUILayout.HelpBox("Preset 방향을 사용합니다. Common은 일반 흔들림, Directional 타입은 기본 오른쪽 방향으로 실행됩니다.", MessageType.Info);
                }
                else if (_directionSource == CameraShakeDirectionSource.FixedDirection && _fixedDirection.sqrMagnitude <= 0.0001f)
                {
                    EditorGUILayout.HelpBox("Fixed Direction 값이 0이면 유효한 방향을 계산할 수 없습니다.", MessageType.Warning);
                }
                else if ((_directionSource == CameraShakeDirectionSource.CasterToTarget || _directionSource == CameraShakeDirectionSource.TargetToCaster) && (_caster == null || _target == null))
                {
                    EditorGUILayout.HelpBox("Caster와 Target을 모두 지정해야 방향을 계산할 수 있습니다. 실패하면 Preset 방향으로 대체됩니다.", MessageType.Warning);
                }
            }
        }

        /// <summary>
        /// 현재 입력값으로 생성되는 카메라 Shake 요청 데이터를 문자열로 표시합니다.
        /// </summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                CameraShakeRequest request = BuildRequest();
                var sb = new StringBuilder();
                sb.AppendLine("[Resolved Camera Shake]");
                sb.AppendLine($"- Preset: {(_preset != null ? _preset.name : "(none)")}");
                sb.AppendLine($"- ShakeType: {request.ShakeType}");
                sb.AppendLine($"- Duration: {request.Duration}");
                sb.AppendLine($"- Strength: {request.Strength}");
                sb.AppendLine($"- AxisStrength: {request.AxisStrength}");
                sb.AppendLine($"- DirectionSource: {_directionSource}");
                sb.AppendLine($"- Direction: {request.Direction}");
                sb.AppendLine($"- RepeatCount: {request.RepeatCount}");
                sb.AppendLine($"- RandomStartPhase: {request.RandomStartPhase}");
                sb.AppendLine($"- UseUnscaledTime: {request.UseUnscaledTime}");
                sb.AppendLine($"- DecayMode: {request.DecayMode}");
                sb.AppendLine($"- Channel: {request.Channel}");
                sb.AppendLine($"- Valid: {request.IsValid}");

                _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.MinHeight(180f));
                EditorGUILayout.TextArea(sb.ToString());
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 카메라 Shake 실행, 중지 버튼 영역을 그립니다.
        /// </summary>
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
                            EditorUtility.DisplayDialog(Title, "유효한 Shake 값이 아닙니다. Duration/Strength/Direction 값을 확인해주세요.", "OK");
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
                                EditorUtility.DisplayDialog(Title, "유효한 Shake 값이 아닙니다. Duration/Strength/Direction 값을 확인해주세요.", "OK");
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

        /// <summary>
        /// 선택된 프리셋 값을 Override 편집 필드로 복사합니다.
        /// </summary>
        private void CopyPresetToOverride()
        {
            if (_preset == null)
            {
                return;
            }

            _shakeType = _preset.ShakeType;
            _duration = _preset.Duration;
            _strength = _preset.Strength;
            _axisStrength = _preset.AxisStrength;
            _repeatCount = _preset.RepeatCount;
            _randomStartPhase = _preset.RandomStartPhase;
            _useUnscaledTime = _preset.UseUnscaledTime;
            _impulseCurve = _preset.ImpulseCurve;
            _decayMode = _preset.DecayMode;
        }

        /// <summary>
        /// 현재 프리셋, Override, 방향 설정을 조합하여 카메라 Shake 요청을 생성합니다.
        /// </summary>
        /// <returns>카메라 매니저가 재생할 수 있는 Shake 요청 데이터입니다.</returns>
        private CameraShakeRequest BuildRequest()
        {
            CameraShakeRequest request = BuildBaseRequest();
            request.Channel = _channel;

            if (_directionSource == CameraShakeDirectionSource.Preset)
            {
                return request;
            }

            if (TryResolveDirection(out Vector2 direction))
            {
                request.Direction = direction;
                if (request.ShakeType == CameraShakeType.Common)
                {
                    request.ShakeType = CameraShakeType.DirectionalImpulse;
                }
            }

            return request;
        }

        /// <summary>
        /// 프리셋 또는 Override 값만 반영한 기본 카메라 Shake 요청을 생성합니다.
        /// </summary>
        /// <returns>방향 정책 적용 전의 기본 Shake 요청 데이터입니다.</returns>
        private CameraShakeRequest BuildBaseRequest()
        {
            if (_preset != null && !_useOverride)
            {
                return _preset.ToRequest(_channel);
            }

            return new CameraShakeRequest
            {
                ShakeType = _shakeType,
                Duration = Mathf.Max(0f, _duration),
                Strength = Mathf.Max(0f, _strength),
                AxisStrength = Vector2.Max(Vector2.zero, _axisStrength),
                Direction = Vector2.right,
                RepeatCount = Mathf.Max(1, _repeatCount),
                RandomStartPhase = _randomStartPhase,
                Channel = _channel,
                UseUnscaledTime = _useUnscaledTime,
                ImpulseCurve = _impulseCurve,
                DecayMode = _decayMode,
            };
        }

        /// <summary>
        /// 에디터에 입력된 방향 정책을 기준으로 실제 Shake 방향을 계산합니다.
        /// </summary>
        /// <param name="direction">정규화된 방향 벡터입니다.</param>
        /// <returns>유효한 방향 계산에 성공했으면 true입니다.</returns>
        private bool TryResolveDirection(out Vector2 direction)
        {
            direction = Vector2.zero;

            switch (_directionSource)
            {
                case CameraShakeDirectionSource.FixedDirection:
                    direction = _fixedDirection;
                    break;
                case CameraShakeDirectionSource.CasterToTarget:
                    if (_caster == null || _target == null)
                    {
                        return false;
                    }

                    direction = _target.position - _caster.position;
                    break;
                case CameraShakeDirectionSource.TargetToCaster:
                    if (_caster == null || _target == null)
                    {
                        return false;
                    }

                    direction = _caster.position - _target.position;
                    break;
                default:
                    return false;
            }

            if (_horizontalOnly)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f && _caster != null && _target != null)
            {
                direction = new Vector2(Mathf.Sign(_target.position.x - _caster.position.x), 0f);
                if (_directionSource == CameraShakeDirectionSource.TargetToCaster)
                {
                    direction = -direction;
                }
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }

        /// <summary>
        /// 현재 게임 씬에서 카메라 매니저를 조회합니다.
        /// </summary>
        /// <param name="cameraManager">조회된 카메라 매니저입니다.</param>
        /// <returns>Play Mode에서 유효한 카메라 매니저를 찾았으면 true입니다.</returns>
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
