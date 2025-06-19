using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 연출 클립 inspector 
    /// </summary>
    [CustomPropertyDrawer(typeof(CutsceneEvent))]
    public class CutsceneEventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("type");

            var cameraMoveProp = property.FindPropertyRelative("cameraMove");
            var cameraZoomProp = property.FindPropertyRelative("cameraZoom");
            var cameraShakeProp = property.FindPropertyRelative("cameraShake");
            var cameraChangeTargetProp = property.FindPropertyRelative("cameraChangeTarget");
            
            var characterMoveProp = property.FindPropertyRelative("characterMove");
            var characterAnimationProp = property.FindPropertyRelative("characterAnimation");
            
            var dialogueBalloonProp = property.FindPropertyRelative("dialogueBalloon");

            var line = pos;
            line.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(line, typeProp);
            line.y += line.height + 2;

            CutsceneEventType cutsceneEventType = (CutsceneEventType)typeProp.enumValueIndex;

            switch (cutsceneEventType)
            {
                case CutsceneEventType.CameraMove:
                    EditorGUI.PropertyField(line, cameraMoveProp, true);
                    break;
                case CutsceneEventType.CameraZoom:
                    EditorGUI.PropertyField(line, cameraZoomProp, true);
                    break;
                case CutsceneEventType.CameraShake:
                    EditorGUI.PropertyField(line, cameraShakeProp, true);
                    break;
                case CutsceneEventType.CameraChangeTarget:
                    EditorGUI.PropertyField(line, cameraChangeTargetProp, true);
                    break;
                case CutsceneEventType.CharacterMove:
                    EditorGUI.PropertyField(line, characterMoveProp, true);
                    break;
                case CutsceneEventType.CharacterAnimation:
                    EditorGUI.PropertyField(line, characterAnimationProp, true);
                    break;
                case CutsceneEventType.DialogueBalloon:
                    EditorGUI.PropertyField(line, dialogueBalloonProp, true);
                    break;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("type");
            CutsceneEventType cutsceneEventType = (CutsceneEventType)typeProp.enumValueIndex;

            float baseHeight = EditorGUIUtility.singleLineHeight * 2 + 6;
            switch (cutsceneEventType)
            {
                case CutsceneEventType.CameraMove:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cameraMove"));
                case CutsceneEventType.CameraZoom:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cameraZoom"));
                case CutsceneEventType.CameraShake:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cameraShake"));
                case CutsceneEventType.CameraChangeTarget:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cameraChangeTarget"));
                
                case CutsceneEventType.CharacterMove: 
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("characterMove"));
                case CutsceneEventType.CharacterAnimation: 
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("characterAnimation"));
                
                case CutsceneEventType.DialogueBalloon: 
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("dialogueBalloon"));
                default: return baseHeight;
            }
        }
    }

}