using GGemCo2DCore;
using TMPro;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 배치툴에서 NPC 배치 정책(DefaultVisible/Flip/MapVisibilityPolicy) 편집을 일관되게 처리하는 유틸리티입니다.
    /// 에디터와 JSON 내보내기 경로가 동일한 기준값(CharacterRegenData)을 사용하도록 보장합니다.
    /// </summary>
    public static class NpcPlacementEditorUtility
    {
        /// <summary>
        /// NPC에 리젠 데이터가 없으면 현재 상태를 기반으로 기본 리젠 데이터를 생성하여 연결합니다.
        /// </summary>
        /// <param name="npc">대상 NPC</param>
        /// <param name="fallbackMapUid">맵 UID 추론 실패 시 사용할 대체 맵 UID</param>
        /// <returns>보정된 리젠 데이터, 실패 시 null</returns>
        public static CharacterRegenData EnsureRegenData(Npc npc, int fallbackMapUid)
        {
            if (!npc)
            {
                return null;
            }

            if (npc.CharacterRegenData != null)
            {
                return npc.CharacterRegenData;
            }

            int mapUid = ResolveMapUid(npc, fallbackMapUid);
            CharacterRegenData regenData = new CharacterRegenData(
                npc.uid,
                npc.transform.position,
                npc.isFlip,
                mapUid,
                defaultVisible: true,
                mapVisibilityPolicy: npc.MapVisibilityPolicy);

            npc.CharacterRegenData = regenData;
            return regenData;
        }

        /// <summary>
        /// NPC의 현재 기본 보임 정책을 조회합니다.
        /// 리젠 데이터가 비어 있으면 생성 후 기본값(true)을 반환합니다.
        /// </summary>
        /// <param name="npc">조회할 NPC</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID</param>
        /// <returns>기본 보임 여부</returns>
        public static bool GetDefaultVisible(Npc npc, int fallbackMapUid)
        {
            CharacterRegenData regenData = EnsureRegenData(npc, fallbackMapUid);
            if (regenData == null)
            {
                return true;
            }

            return regenData.DefaultVisible;
        }

        /// <summary>
        /// NPC의 현재 Flip 정책 값을 조회합니다.
        /// 리젠 데이터가 비어 있으면 생성 후 현재 NPC의 Flip 상태를 기준으로 반환합니다.
        /// </summary>
        /// <param name="npc">조회할 NPC</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID</param>
        /// <returns>Flip 여부</returns>
        public static bool GetFlip(Npc npc, int fallbackMapUid)
        {
            CharacterRegenData regenData = EnsureRegenData(npc, fallbackMapUid);
            if (regenData == null)
            {
                return false;
            }

            return regenData.IsFlip;
        }

        /// <summary>
        /// NPC의 현재 맵 표시 정책을 조회합니다.
        /// 리젠 데이터가 비어 있으면 현재 NPC의 런타임 정책을 기준으로 생성합니다.
        /// </summary>
        /// <param name="npc">조회할 NPC입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <returns>현재 맵 표시 정책입니다.</returns>
        public static MapCharacterVisibilityPolicy GetMapVisibilityPolicy(Npc npc, int fallbackMapUid)
        {
            CharacterRegenData regenData = EnsureRegenData(npc, fallbackMapUid);
            if (regenData == null)
            {
                return MapCharacterVisibilityPolicy.DefaultCulling;
            }

            return regenData.MapVisibilityPolicy;
        }

        /// <summary>
        /// NPC의 배치 정책을 한 번에 적용하고 오버레이 텍스트를 갱신합니다.
        /// Flip과 맵 표시 정책은 NPC 런타임 상태에도 즉시 반영합니다.
        /// </summary>
        /// <param name="npc">적용 대상 NPC입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <param name="defaultVisible">기본 보임 여부입니다.</param>
        /// <param name="isFlip">Flip 여부입니다.</param>
        /// <param name="mapVisibilityPolicy">맵 표시 정책입니다.</param>
        public static void ApplyPlacementPolicy(
            Npc npc,
            int fallbackMapUid,
            bool defaultVisible,
            bool isFlip,
            MapCharacterVisibilityPolicy mapVisibilityPolicy)
        {
            if (!npc)
            {
                return;
            }

            CharacterRegenData regenData = EnsureRegenData(npc, fallbackMapUid);
            if (regenData == null)
            {
                return;
            }

            regenData.MapUid = ResolveMapUid(npc, fallbackMapUid);
            regenData.DefaultVisible = defaultVisible;
            regenData.IsFlip = isFlip;
            regenData.MapVisibilityPolicy = mapVisibilityPolicy;
            regenData.x = npc.transform.position.x;
            regenData.y = npc.transform.position.y;
            regenData.z = npc.transform.position.z;

            npc.SetFlip(isFlip);
            npc.SetMapVisibilityPolicy(mapVisibilityPolicy);
            UpdateInfoText(npc);
        }

        /// <summary>
        /// 에디터에서 표시하는 NPC 오버레이 텍스트를 현재 정책 값으로 갱신합니다.
        /// </summary>
        /// <param name="npc">텍스트를 갱신할 NPC</param>
        public static void UpdateInfoText(Npc npc)
        {
            if (!npc)
            {
                return;
            }

            TextMeshProUGUI text = npc.GetComponentInChildren<TextMeshProUGUI>();
            if (!text)
            {
                return;
            }

            CharacterRegenData regenData = npc.CharacterRegenData;
            bool defaultVisible = regenData == null || regenData.DefaultVisible;
            bool isFlip = regenData != null ? regenData.IsFlip : npc.isFlip;
            MapCharacterVisibilityPolicy mapVisibilityPolicy = regenData != null
                ? regenData.MapVisibilityPolicy
                : npc.MapVisibilityPolicy;
            Vector3 pos = npc.transform.position;
            float scaleX = npc.transform.localScale.x;
            text.text =
                $"Uid: {npc.uid}\nPos: ({pos.x:F2}, {pos.y:F2})\nScale: {scaleX:F2}\nFlip: {isFlip}\nDefaultVisible: {defaultVisible}\nVisibilityPolicy: {mapVisibilityPolicy}";
        }

        /// <summary>
        /// NPC가 속한 맵 UID를 안전하게 계산합니다.
        /// </summary>
        /// <param name="npc">대상 NPC</param>
        /// <param name="fallbackMapUid">대체 맵 UID</param>
        /// <returns>확정된 맵 UID</returns>
        private static int ResolveMapUid(Npc npc, int fallbackMapUid)
        {
            if (!npc)
            {
                return fallbackMapUid;
            }

            CharacterRegenData regenData = npc.CharacterRegenData;
            if (regenData != null && regenData.MapUid > 0)
            {
                return regenData.MapUid;
            }

            DefaultMap map = npc.GetComponentInParent<DefaultMap>();
            if (map)
            {
                int mapUid = map.GetChapterNumber();
                if (mapUid > 0)
                {
                    return mapUid;
                }
            }

            return fallbackMapUid;
        }
    }
}
