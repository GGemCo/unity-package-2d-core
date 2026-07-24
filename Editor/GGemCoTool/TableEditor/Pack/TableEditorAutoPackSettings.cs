using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 데이터 테이블 에디터와 외부 테이블 제작 도구가 공유하는 Auto Pack 설정입니다.
    /// </summary>
    public static class TableEditorAutoPackSettings
    {
        private const string EditorPrefsKey =
            "GGemCo.TableEditor.AutoPackOnSave";

        /// <summary>
        /// 실제 테이블 변경 후 런타임 테이블 pack을 자동 재생성할지 여부입니다.
        /// 기존 데이터 에디터의 EditorPrefs 키를 유지하여 사용자 설정과 호환됩니다.
        /// </summary>
        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(EditorPrefsKey, false);
            set => EditorPrefs.SetBool(EditorPrefsKey, value);
        }
    }
}
