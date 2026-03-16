using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Debug HUD 출력 전용 Presenter 입니다.
    /// Manager에서 구성한 스냅샷을 화면에 렌더링합니다.
    /// </summary>
    public sealed class GGemCoDebugHudRoot : MonoBehaviour
    {
        private GUIStyle _boxStyle;
        private Texture2D _backgroundTexture;
        private int _appliedFontSize = -1;
        private Vector2 _appliedPadding;
        private Color _appliedBackgroundColor = Color.clear;

        private static GGemCoDebugHudRoot _instance;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (_instance != null && _instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            GGemCoDebugHudManager.Tick(Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            string snapshot = GGemCoDebugHudManager.BuildSnapshot();
            if (string.IsNullOrWhiteSpace(snapshot))
            {
                return;
            }

            GUIContent content = new GUIContent(snapshot);
            GUIStyle style = GetStyle();
            Vector2 padding = GetPadding();
            Vector2 size = style.CalcSize(content);
            Rect rect = new Rect(
                padding.x,
                padding.y,
                Mathf.Min(size.x + 10f, Screen.width - padding.x * 2f),
                Mathf.Min(size.y + 10f, Screen.height - padding.y * 2f));

            GUI.Box(rect, content, style);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            if (_backgroundTexture != null)
            {
                DestroyImmediate(_backgroundTexture);
                _backgroundTexture = null;
            }
        }

        public void MarkStyleDirty()
        {
            _boxStyle = null;
            if (_backgroundTexture != null)
            {
                DestroyImmediate(_backgroundTexture);
                _backgroundTexture = null;
            }
            _appliedFontSize = -1;
            _appliedPadding = Vector2.zero;
            _appliedBackgroundColor = Color.clear;
        }

        private GUIStyle GetStyle()
        {
            GGemCoSettings settings = GGemCoDebugHudManager.CurrentSettings;
            int fontSize = settings != null ? Mathf.Clamp(settings.debugHudFontSize, 8, 32) : 12;
            Color backgroundColor = settings != null ? settings.debugHudBackgroundColor : new Color(0f, 0f, 0f, 0.55f);

            if (_boxStyle == null || _appliedFontSize != fontSize || _appliedBackgroundColor != backgroundColor)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = fontSize,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = false
                };
                _appliedFontSize = fontSize;

                if (_backgroundTexture == null)
                {
                    _backgroundTexture = new Texture2D(1, 1)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }

                _backgroundTexture.SetPixel(0, 0, backgroundColor);
                _backgroundTexture.Apply();
                _boxStyle.normal.background = _backgroundTexture;
                _appliedBackgroundColor = backgroundColor;
            }

            return _boxStyle;
        }

        private Vector2 GetPadding()
        {
            GGemCoSettings settings = GGemCoDebugHudManager.CurrentSettings;
            Vector2 padding = settings != null ? settings.debugHudPadding : new Vector2(8f, 8f);
            _appliedPadding = padding;
            return padding;
        }
    }
}
