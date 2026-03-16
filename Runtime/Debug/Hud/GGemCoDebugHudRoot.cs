#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디버그 HUD 프레젠터입니다. 실제 측정은 프로바이더가 담당하고, 이 클래스는 화면 표시만 담당합니다.
    /// </summary>
    public class GGemCoDebugHudRoot : MonoBehaviour
    {
        [Range(8, 32)] public int fontSize = 12;
        public Vector2 padding = new(8, 8);
        public Color backgroundColor = new(0, 0, 0, 0.55f);

        private GUIStyle _box;
        private Texture2D _bgTex;
        private int _appliedFontSize = -1;
        private Color _appliedBg;
        private static GGemCoDebugHudRoot _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            ApplySettings(GGemCoDebugHudManager.Settings);
        }

        private void Update()
        {
            GGemCoSettings settings = GGemCoDebugHudManager.Settings;
            if (settings == null || !DebugOptionRuntimeUtility.Resolve(settings.enableDebugHud) || !GGemCoDebugHudManager.HasAnyEnabledProvider(settings))
            {
                return;
            }

            ApplySettings(settings);

            float deltaTime = Time.unscaledDeltaTime;
            foreach (IDebugHudProvider provider in GGemCoDebugHudManager.RegisteredProviders)
            {
                if (!provider.IsEnabled(settings))
                {
                    continue;
                }

                provider.Tick(deltaTime, settings);
            }
        }

        private void OnGUI()
        {
            GGemCoSettings settings = GGemCoDebugHudManager.Settings;
            if (settings == null || !DebugOptionRuntimeUtility.Resolve(settings.enableDebugHud))
            {
                return;
            }

            foreach (IDebugHudProvider provider in GGemCoDebugHudManager.RegisteredProviders)
            {
                if (!provider.IsEnabled(settings))
                {
                    continue;
                }

                string text = provider.GetText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                DrawAnchoredBox(provider.Anchor, new GUIContent(text));
            }
        }

        public void ApplySettings(GGemCoSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            fontSize = Mathf.Clamp(settings.debugHudFontSize, 8, 32);
            padding = new Vector2(Mathf.Max(0f, settings.debugHudPaddingX), Mathf.Max(0f, settings.debugHudPaddingY));
            backgroundColor = settings.debugHudBackgroundColor;
            Invalidate();
        }

        public GUIStyle GetStyle()
        {
            if (_box == null || _appliedFontSize != fontSize)
            {
                _box = new GUIStyle(GUI.skin.box)
                {
                    fontSize = fontSize,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = false
                };
                _appliedFontSize = fontSize;
            }

            if (_bgTex == null || _appliedBg != backgroundColor)
            {
                if (_bgTex == null)
                {
                    _bgTex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                }

                _bgTex.SetPixel(0, 0, backgroundColor);
                _bgTex.Apply();
                _appliedBg = backgroundColor;
                _box.normal.background = _bgTex;
            }

            return _box;
        }

        public void DrawBox(Rect rect, GUIContent content)
        {
            GUI.Box(rect, content, GetStyle());
        }

        private void DrawAnchoredBox(DebugHudAnchor anchor, GUIContent content)
        {
            GUIStyle style = GetStyle();
            Vector2 size = style.CalcSize(content);
            float width = Mathf.Min(size.x + 10f, Screen.width - padding.x * 2f);
            float height = size.y + 10f;

            Rect rect = anchor switch
            {
                DebugHudAnchor.TopLeft => new Rect(padding.x, padding.y, width, height),
                DebugHudAnchor.TopRight => new Rect(Screen.width - width - padding.x, padding.y, width, height),
                DebugHudAnchor.BottomLeft => new Rect(padding.x, Screen.height - height - padding.y, width, height),
                DebugHudAnchor.BottomRight => new Rect(Screen.width - width - padding.x, Screen.height - height - padding.y, width, height),
                _ => new Rect(padding.x, padding.y, width, height),
            };

            DrawBox(rect, content);
        }

        private void Invalidate()
        {
            _box = null;
            if (_bgTex != null)
            {
                DestroyImmediate(_bgTex);
            }

            _bgTex = null;
        }
    }
}
#endif
