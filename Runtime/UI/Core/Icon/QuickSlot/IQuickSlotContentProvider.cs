using UnityEngine;

namespace GGemCo2DCore
{
    public interface IQuickSlotContentProvider
    {
        int Priority { get; }
        bool CanProvide(QuickSlotContentKind kind);

        /// <summary>
        /// uid/level/instanceId 기반으로 퀵슬롯 아이콘을 반환한다.
        /// 반환값이 null 이면 다음 Provider 를 시도한다.
        /// </summary>
        Sprite GetIconSprite(QuickSlotContentKind kind, int uid, int level, long instanceId);
    }
}