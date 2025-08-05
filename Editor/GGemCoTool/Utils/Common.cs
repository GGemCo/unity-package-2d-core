using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public abstract class Common
    {
        public static void OnGUITitle(string title)
        {
            GUILayout.Label($"[ {title} ]", EditorStyles.whiteLargeLabel);
        }

        public static void OnGUITitleBold(string title)
        {
            GUILayout.Label($"{title}", EditorStyles.boldLabel);
        }

        public static void GUILine(int lineHeight = 1, string hexCode = "")
        {
            EditorGUILayout.Space();
            Rect rect = EditorGUILayout.GetControlRect(false, lineHeight);
            rect.height = lineHeight;
            if (!string.IsNullOrEmpty(hexCode))
                EditorGUI.DrawRect(rect, ColorHelper.HexToColor(hexCode));
            else
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space();
        }

        public static void GUILineBlue(int height = 1)
        {
            GUILayout.Space(10);
            GUILine(height, "94D8F6");
            GUILayout.Space(10);
        }

        public static bool ExistAddressableByPath(string path)
        {
            // Addressable 에 등록되어있는지 체크 
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings)
            {
                AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                if (entry == null)
                {
                    Debug.LogWarning($"Addressable 에 등록되지 않았습니다. (경로: {path})");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"Addressable 설정이 되어있지 않습니다.");
                return false;
            }

            return true;
        }
    }
}
