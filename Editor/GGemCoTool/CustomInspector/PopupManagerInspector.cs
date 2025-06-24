using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    [CustomEditor(typeof(GGemCo2DCore.PopupManager))]
    public class PopupManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Common.GUILineBlue(1);

            Common.OnGUITitleBold("생성하기");
            // GGemCo2DCore.SceneIntro sceneIntro = (GGemCo2DCore.SceneIntro)target;

            var canvasPopupField = typeof(GGemCo2DCore.PopupManager)
                .GetField("canvasPopup", BindingFlags.NonPublic | BindingFlags.Instance);
            Button currentValueButtonNewGame = canvasPopupField?.GetValue(target) as Button;
            
            if (canvasPopupField != null && !currentValueButtonNewGame)
            {
                if (GUILayout.Button("canvasPopup 연결하기"))
                {
                }
            }
        }
    }
}