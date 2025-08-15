using System;
using System.Reflection;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    [CustomPropertyDrawer(typeof(SoundConstants.UIButtonType))]
    public class EnumTooltipDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Type enumType = fieldInfo.FieldType;
            string[] enumNames = property.enumDisplayNames;

            // Enum 항목마다 Tooltip 가져오기
            GUIContent[] displayedOptions = new GUIContent[enumNames.Length];
            var enumValues = Enum.GetValues(enumType);

            for (int i = 0; i < enumNames.Length; i++)
            {
                string enumName = enumValues.GetValue(i).ToString();
                var memberInfo = enumType.GetMember(enumName);
                string tooltip = "";

                if (memberInfo.Length > 0)
                {
                    var tooltipAttr = memberInfo[0].GetCustomAttribute<TooltipAttribute>();
                    if (tooltipAttr != null)
                        tooltip = tooltipAttr.tooltip;
                }

                displayedOptions[i] = new GUIContent(enumNames[i], tooltip);
            }

            EditorGUI.BeginProperty(position, label, property);
            property.enumValueIndex = EditorGUI.Popup(position, label, property.enumValueIndex, displayedOptions);
            EditorGUI.EndProperty();
        }
    }
}