using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터 전용 공통 UI Helper 모음.
    /// - 제목 라벨
    /// - 구분선(Line)
    /// - Addressables 유틸
    /// - 토글(체크박스) 레이아웃 헬퍼
    /// </summary>
    public static class HelperEditorUI
    {
        private static readonly Color DefaultLineColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static GUIStyle _wrapLabelStyle;

        /// <summary>
        /// 내부에서 사용하는 WordWrap 라벨 스타일을 반환합니다.
        /// 한 번 생성 후 재사용합니다.
        /// </summary>
        private static GUIStyle WrapLabelStyle
        {
            get
            {
                if (_wrapLabelStyle == null)
                {
                    _wrapLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true
                    };
                }

                return _wrapLabelStyle;
            }
        }

        #region Title

        /// <summary>
        /// 헤더 타이틀을 굵고 큰 흰색 글자로 출력합니다.
        /// </summary>
        public static void OnGUITitle(string title)
        {
            GUILayout.Label($"[ {title} ]", EditorStyles.whiteLargeLabel);
        }

        /// <summary>
        /// 굵은 텍스트로 타이틀을 출력합니다.
        /// </summary>
        public static void OnGUITitleBold(string title)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
        }

        #endregion

        #region Line

        /// <summary>
        /// 수평 구분선을 그립니다.
        /// </summary>
        /// <param name="lineHeight">라인 두께(픽셀)</param>
        /// <param name="hexCode">색상(HEX). 빈 값이면 기본 회색 사용.</param>
        public static void GUILine(int lineHeight = 1, string hexCode = "")
        {
            EditorGUILayout.Space();

            Rect rect = EditorGUILayout.GetControlRect(false, lineHeight);
            rect.height = lineHeight;

            Color color = DefaultLineColor;
            if (!string.IsNullOrEmpty(hexCode))
            {
                color = ColorHelper.HexToColor(hexCode, Color.white);
            }

            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 상‧하단에 여백이 있는 파란색 구분선을 그립니다.
        /// </summary>
        public static void GUILineBlue(int height = 1)
        {
            GUILayout.Space(10f);
            GUILine(height, "94D8F6");
            GUILayout.Space(10f);
        }

        #endregion

        #region Addressables

        /// <summary>
        /// 주어진 에셋 경로가 Addressables 에 등록되어 있는지 검사합니다.
        /// </summary>
        /// <param name="path">프로젝트 내 에셋 경로(Assets/…)</param>
        /// <returns>등록되어 있으면 true, 아니면 false</returns>
        public static bool ExistAddressableByPath(string path)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("Addressable 설정이 되어있지 않습니다.");
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"유효하지 않은 에셋 경로입니다. (경로: {path})");
                return false;
            }

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                Debug.LogWarning($"Addressable 에 등록되지 않았습니다. (경로: {path})");
                return false;
            }

            return true;
        }

        #endregion

        #region Toggle Helpers

        /// <summary>
        /// 체크박스를 왼쪽, 라벨을 오른쪽에 배치한 토글을 그립니다.
        /// 라벨은 자동 줄바꿈이 적용됩니다.
        /// </summary>
        /// <param name="label">표시할 텍스트</param>
        /// <param name="value">현재 값</param>
        /// <param name="tooltip">툴팁(선택)</param>
        /// <param name="toggleWidth">체크박스 영역 너비</param>
        /// <returns>변경된 토글 값</returns>
        public static bool ToggleLeft(string label, bool value, string tooltip = null, float toggleWidth = 18f)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 체크박스(왼쪽)
                bool newValue = GUILayout.Toggle(value, GUIContent.none, GUILayout.Width(toggleWidth));

                // 라벨(오른쪽, 줄바꿈)
                var content = string.IsNullOrEmpty(tooltip)
                    ? new GUIContent(label)
                    : new GUIContent(label, tooltip);

                EditorGUILayout.LabelField(content, WrapLabelStyle);

                return newValue;
            }
        }

        /// <summary>
        /// 라벨을 왼쪽, 체크박스를 오른쪽에 배치한 토글을 그립니다.
        /// 기본 EditorGUILayout.Toggle 와 유사하지만 라벨에 WordWrap 을 적용할 수 있습니다.
        /// </summary>
        /// <param name="label">표시할 텍스트</param>
        /// <param name="value">현재 값</param>
        /// <param name="tooltip">툴팁(선택)</param>
        /// <param name="useWordWrapLabel">true 이면 라벨에 줄바꿈 적용</param>
        /// <returns>변경된 토글 값</returns>
        public static bool ToggleRight(string label, bool value, string tooltip = null, bool useWordWrapLabel = false)
        {
            var content = string.IsNullOrEmpty(tooltip)
                ? new GUIContent(label)
                : new GUIContent(label, tooltip);

            if (!useWordWrapLabel)
            {
                // Unity 기본 형태(라벨 왼쪽, 토글 오른쪽)
                return EditorGUILayout.Toggle(content, value);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                // 라벨(왼쪽, 줄바꿈)
                EditorGUILayout.LabelField(content, WrapLabelStyle);

                // 토글(오른쪽)
                return EditorGUILayout.Toggle(value, GUILayout.Width(18f));
            }
        }

        #endregion
    }
}
