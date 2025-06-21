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
        private static readonly Dictionary<Transform, Vector3> PreviousPositions = new Dictionary<Transform, Vector3>();

        static CharacterInfoWatcher()
        {
            // 에디터가 업데이트될 때마다 위치 체크
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            // 모든 Npc 컴포넌트를 가진 오브젝트 검사
#if UNITY_6000_0_OR_NEWER
            var characterBases = Object.FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
#else
            var characterBases = GameObject.FindObjectsOfType<CharacterBase>();
#endif
            
            foreach (var npc in characterBases)
            {
                Transform npcTransform = npc.transform;
                if (!PreviousPositions.ContainsKey(npcTransform))
                {
                    PreviousPositions[npcTransform] = npcTransform.position;
                    continue;
                }

                if (PreviousPositions[npcTransform] != npcTransform.position)
                {
                    // 위치가 바뀌었을 때
                    UpdateInfoText(npc);
                    PreviousPositions[npcTransform] = npcTransform.position;
                }
            }
        }

        private static void UpdateInfoText(CharacterBase characterBase)
        {
            var text = characterBase.GetComponentInChildren<TextMeshProUGUI>();
            if (!text) return;
            Vector3 pos = characterBase.transform.position;
            Vector3 scale = characterBase.transform.localScale;
            text.text = $"Uid: {characterBase.uid}\nPos: ({pos.x:F2}, {pos.y:F2})\nScale: {scale.x:F2}";
        }
    }
}
#endif