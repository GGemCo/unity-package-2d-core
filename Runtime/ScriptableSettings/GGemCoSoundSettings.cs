using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCore
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
    
    [CreateAssetMenu(fileName = ConfigScriptableObject.Sound.FileName, menuName = ConfigScriptableObject.Sound.MenuName,
        order = ConfigScriptableObject.Sound.Ordering)]
    public class GGemCoSoundSettings : ScriptableObject
    {
        [Serializable]
        public class MappingButtonClickSound
        {
            public SoundConstants.UIButtonType type;
            public int soundUid;
        }
        [Tooltip("버튼 Type별로 사운드를 설정합니다.")]
        public List<MappingButtonClickSound> buttonClickSounds;
        
        /// <summary>
        /// 기존 값이 비어있을 때만 기본값을 설정
        /// </summary>
        private void OnEnable()
        {
            // defaultSoundButtonClick = 0;
            // defaultSoundButtonClickConfirm = 0;
            // defaultSoundButtonClickCancel = 0;
            // defaultSoundButtonClickWindowClose = 0;
        }

        public int GetSoundButtonClickUid(SoundConstants.UIButtonType buttonType)
        {
            var info = buttonClickSounds.Find(x => x.type == buttonType);
            return info?.soundUid ?? 0;
        }

        public int GetDefaultButtonClick()
        {
            return GetSoundButtonClickUid(SoundConstants.UIButtonType.Default);
        }
    }
}