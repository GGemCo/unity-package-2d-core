using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Editor
{
    [CustomEditor(typeof(GGemCo.Scripts.PopupManager))]
    public class PopupManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Common.GUILineBlue(1);

            Common.OnGUITitleBold("생성하기");
            // GGemCo.Scripts.SceneIntro sceneIntro = (GGemCo.Scripts.SceneIntro)target;

            var canvasPopupField = typeof(Scripts.PopupManager)
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