#if UNITY_EDITOR
using System.Collections.Generic;
using GGemCo.Scripts;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    [InitializeOnLoad]
    public static class CharacterInfoWatcher
    {
        private static Dictionary<Transform, Vector3> previousPositions = new Dictionary<Transform, Vector3>();

        static CharacterInfoWatcher()
        {
            // 에디터가 업데이트될 때마다 위치 체크
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            // 모든 Npc 컴포넌트를 가진 오브젝트 검사
#if UNITY_6000_0_OR_NEWER
            var characterBases = GameObject.FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
#else
            var characterBases = GameObject.FindObjectsOfType<CharacterBase>();
#endif
            
            foreach (var npc in characterBases)
            {
                Transform npcTransform = npc.transform;
                if (!previousPositions.ContainsKey(npcTransform))
                {
                    previousPositions[npcTransform] = npcTransform.position;
                    continue;
                }

                if (previousPositions[npcTransform] != npcTransform.position)
                {
                    // 위치가 바뀌었을 때
                    UpdateInfoText(npc);
                    previousPositions[npcTransform] = npcTransform.position;
                }
            }
        }

        private static void UpdateInfoText(CharacterBase characterBase)
        {
            var text = characterBase.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                Vector3 pos = characterBase.transform.position;
                Vector3 scale = characterBase.transform.localScale;
                text.text = $"Uid: {characterBase.uid}\nPos: ({pos.x:F2}, {pos.y:F2})\nScale: {scale.x:F2}";
            }
        }
    }
}
#endif