using System;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="GGemCoMonsterSettings"/> 전용 인스펙터.
    /// - CharacterConstants.Grade 멀티 선택(MaskField) UI 제공.
    /// </summary>
    [CustomEditor(typeof(GGemCoMonsterSettings))]
    public sealed class GGemCoMonsterSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _useBattleHud;
        private SerializedProperty _useBattleHudGradeMask;
        
        private SerializedProperty _useCutsceneDie;
        private SerializedProperty _useCutsceneDieGradeMask;
        private SerializedProperty _cutsceneUidDie;

        private static readonly string[] GradeNames = Enum.GetNames(typeof(CharacterConstants.Grade));

        private void OnEnable()
        {
            _useBattleHud = serializedObject.FindProperty("useBattleHud");
            _useBattleHudGradeMask = serializedObject.FindProperty("useBattleHudGradeMask");
            
            _useCutsceneDie = serializedObject.FindProperty("useCutsceneDie");
            _useCutsceneDieGradeMask = serializedObject.FindProperty("useCutsceneDieGradeMask");
            _cutsceneUidDie = serializedObject.FindProperty("cutsceneUidDie");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script", "useBattleHud", "useBattleHudGradeMask",
                "useCutsceneDie", "useCutsceneDieGradeMask", "cutsceneUidDie");

            // EditorGUILayout.Space(8);

            if (_useBattleHud != null)
                EditorGUILayout.PropertyField(_useBattleHud);

            using (new EditorGUI.DisabledScope(_useBattleHud != null && !_useBattleHud.boolValue))
            {
                if (_useBattleHudGradeMask != null)
                {
                    var content = new GUIContent("Use Battle Hud Grades", "전투 HUD를 사용할 몬스터 등급(멀티 선택)");
                    var mask = _useBattleHudGradeMask.intValue;
                    var newMask = EditorGUILayout.MaskField(content, mask, GradeNames);

                    // None(0) 선택 방지
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

                    // None(0) 선택 방지
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
