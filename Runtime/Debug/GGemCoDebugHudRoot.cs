#if UNITY_EDITOR
using UnityEngine;

namespace GGemCo2DCore
{
    public class GGemCoDebugHudRoot : MonoBehaviour
    {
        [Range(8, 32)] public int fontSize = 12;
        public Vector2 padding = new(8, 8);
        public Color backgroundColor = new(0, 0, 0, 0.55f);

        private GUIStyle _box;
        private Texture2D _bgTex;
        private int _appliedFontSize = -1;
        private Color _appliedBg;

        // ★ 싱글턴 인스턴스
        private static GGemCoDebugHudRoot _instance;

        private void Awake()
        {
            // 플레이 중에는 DDOL로 전환(씬 전환 생존)
            if (Application.isPlaying)
            {
                if (_instance != null && _instance != this)
                {
                    // 기존 인스턴스가 이미 DDOL로 존재 → 중복 생성 방지
                    Destroy(gameObject);
                    return;
                }
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
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

        public void DrawBox(Rect r, GUIContent content)
        {
            GUI.Box(r, content, GetStyle());
        }

        private void OnValidate()
        {
            Invalidate();
            UnityEditor.EditorApplication.delayCall += RepaintAllGameViews;
        }

        private void Invalidate()
        {
            _box = null;
            if (_bgTex) DestroyImmediate(_bgTex);
            _bgTex = null;
        }

        private static void RepaintAllGameViews()
        {
            var gameViewType = System.Type.GetType("UnityEditor.GameView, UnityEditor");
            if (gameViewType == null) return;
            foreach (var gv in Resources.FindObjectsOfTypeAll(gameViewType))
                gameViewType.GetMethod("Repaint")?.Invoke(gv, null);
        }
    }
}
#endif
