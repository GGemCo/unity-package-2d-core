#if UNITY_EDITOR
using System.Collections.Generic;
using GGemCo2DCore;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
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
            // 모든 캐릭터 컴포넌트를 가진 오브젝트 검사
            var characterBases = CompatObjectFind.FindAll<CharacterBase>();            
            foreach (var characterBase in characterBases)
            {
                Transform characterTransform = characterBase.transform;
                if (!PreviousPositions.ContainsKey(characterTransform))
                {
                    PreviousPositions[characterTransform] = characterTransform.position;
                    continue;
                }

                if (PreviousPositions[characterTransform] != characterTransform.position)
                {
                    // 위치가 바뀌었을 때
                    UpdateInfoText(characterBase);
                    PreviousPositions[characterTransform] = characterTransform.position;
                }
            }
        }

        /// <summary>
        /// 캐릭터 오버레이 텍스트를 현재 상태로 갱신합니다.
        /// NPC와 몬스터는 맵 배치 표시 정책을 포함해 출력하고, 그 외 타입은 기존 포맷을 유지합니다.
        /// </summary>
        /// <param name="characterBase">텍스트를 갱신할 캐릭터입니다.</param>
        private static void UpdateInfoText(CharacterBase characterBase)
        {
            if (characterBase == null) return;
            Npc npc = characterBase as Npc;
            if (npc != null)
            {
                NpcPlacementEditorUtility.UpdateInfoText(npc);
                return;
            }

            Monster monster = characterBase as Monster;
            if (monster != null)
            {
                MonsterPlacementEditorUtility.UpdateInfoText(monster);
                return;
            }

            var text = characterBase.GetComponentInChildren<TextMeshProUGUI>();
            if (!text) return;
            Vector3 pos = characterBase.transform.position;
            Vector3 scale = characterBase.transform.localScale;
            text.text = $"Uid: {characterBase.uid}\nPos: ({pos.x:F2}, {pos.y:F2})\nScale: {scale.x:F2}";
        }
    }
}
#endif
