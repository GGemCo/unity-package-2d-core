using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public abstract class Common
    {
        public static void OnGUITitle(string title)
        {
            GUILayout.Label($"[ {title} ]", EditorStyles.whiteLargeLabel);
        }
        public static void GUILine( int lineHeight = 1, string hexCode = "" ) {
            EditorGUILayout.Space();
            Rect rect = EditorGUILayout.GetControlRect(false, lineHeight );
            rect.height = lineHeight;
            if (hexCode != "")
            {
                EditorGUI.DrawRect(rect, ColorHelper.HexToColor(hexCode));
            }
            else
            {
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            }

            EditorGUILayout.Space();
        }

        public static void GUILineBlue(int height)
        {
            GUILayout.Space(10);
            GUILine(height, "94D8F6");
            GUILayout.Space(10);
        }
    }
}