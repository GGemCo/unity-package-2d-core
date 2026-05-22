using System;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="GGemCoMonsterSettings"/> 전용 인스펙터입니다.
    /// - CharacterConstants.Grade 멀티 선택(MaskField) UI를 제공합니다.
    /// </summary>
    [CustomEditor(typeof(GGemCoMonsterSettings))]
    public sealed class GGemCoMonsterSettingsEditor : Editor
    {
        private SerializedProperty _useBattleHud;
        private SerializedProperty _useBattleHudGradeMask;

        private SerializedProperty _useCutsceneDie;
        private SerializedProperty _useCutsceneDieGradeMask;
        private SerializedProperty _cutsceneUidDie;

        private SerializedProperty _breakResetMode;
        private SerializedProperty _breakResetModeGradeMask;
        private SerializedProperty _perAttackConsumeCooldown;

        private static readonly string[] GradeNames = Enum.GetNames(typeof(CharacterConstants.Grade));

        private void OnEnable()
        {
            _breakResetMode = serializedObject.FindProperty("breakResetMode");
            _breakResetModeGradeMask = serializedObject.FindProperty("breakResetModeGradeMask");
            _perAttackConsumeCooldown = serializedObject.FindProperty("perAttackConsumeCooldown");
            
            _useBattleHud = serializedObject.FindProperty("useBattleHud");
            _useBattleHudGradeMask = serializedObject.FindProperty("useBattleHudGradeMask");

            _useCutsceneDie = serializedObject.FindProperty("useCutsceneDie");
            _useCutsceneDieGradeMask = serializedObject.FindProperty("useCutsceneDieGradeMask");
            _cutsceneUidDie = serializedObject.FindProperty("cutsceneUidDie");
        }

        /// <summary>
        /// 몬스터 설정 에셋 인스펙터를 렌더링하고 Grade 마스크 전용 UI를 그립니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script", "breakResetMode", "breakResetModeGradeMask", "perAttackConsumeCooldown", "useBattleHud", "useBattleHudGradeMask",
                "useCutsceneDie", "useCutsceneDieGradeMask", "cutsceneUidDie");

            if (_breakResetMode != null)
                EditorGUILayout.PropertyField(_breakResetMode);
            
            if (_breakResetModeGradeMask != null)
            {
                var content = new GUIContent("Break Reset Mode Grades", "breakResetMode를 적용할 몬스터 등급(멀티 선택, 미선택 시 전체 적용)");
                var mask = _breakResetModeGradeMask.intValue;
                var newMask = EditorGUILayout.MaskField(content, mask, GradeNames);

                // None(0)은 실제 몬스터 등급 선택에서 제외합니다.
                if ((newMask & 1) != 0)
                    newMask &= ~1;

                _breakResetModeGradeMask.intValue = newMask;
            }
            
            if (_perAttackConsumeCooldown != null)
                EditorGUILayout.PropertyField(_perAttackConsumeCooldown);
            
            if (_useBattleHud != null)
                EditorGUILayout.PropertyField(_useBattleHud);

            using (new EditorGUI.DisabledScope(_useBattleHud != null && !_useBattleHud.boolValue))
            {
                if (_useBattleHudGradeMask != null)
                {
                    var content = new GUIContent("Use Battle Hud Grades", "전투 HUD를 사용할 몬스터 등급(멀티 선택)");
                    var mask = _useBattleHudGradeMask.intValue;
                    var newMask = EditorGUILayout.MaskField(content, mask, GradeNames);

                    // None(0)은 실제 몬스터 등급 선택에서 제외합니다.
                    if ((newMask & 1) != 0)
                        newMask &= ~1;

                    _useBattleHudGradeMask.intValue = newMask;
                }
            }

            if (_useCutsceneDie != null)
                EditorGUILayout.PropertyField(_useCutsceneDie);

            using (new EditorGUI.DisabledScope(_useCutsceneDie != null && !_useCutsceneDie.boolValue))
            {
                if (_useCutsceneDieGradeMask != null)
                {
                    var content = new GUIContent("Use Cutscene Die Grades", "사망 연출을 사용할 몬스터 등급(멀티 선택)");
                    var mask = _useCutsceneDieGradeMask.intValue;
                    var newMask = EditorGUILayout.MaskField(content, mask, GradeNames);

                    // None(0)은 실제 몬스터 등급 선택에서 제외합니다.
                    if ((newMask & 1) != 0)
                        newMask &= ~1;

                    _useCutsceneDieGradeMask.intValue = newMask;
                }
                if (_cutsceneUidDie != null)
                    EditorGUILayout.PropertyField(_cutsceneUidDie);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
