using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 전용 아이콘.
    /// - 스킬/아이템 모두 표시 가능 (ProviderRegistry 기반)
    /// - Core 는 Skill 패키지를 직접 참조하지 않는다.
    /// </summary>
    public class UIIconQuickSlot : UIIcon
    {
        private QuickSlotContentKind _kind = QuickSlotContentKind.None;

        protected override void Awake()
        {
            base.Awake();
            IconType = IconConstants.Type.None; // 퀵슬롯은 컨텐츠 타입이 슬롯마다 달라질 수 있음
        }

        /// <summary>
        /// 퀵슬롯 엔트리를 적용한다.
        /// </summary>
        public bool ApplyEntry(QuickSlotContentKind kind, int iconUid, int iconCount, int iconLevel = 0, bool iconIsLearn = false, long iconInstanceId = 0)
        {
            _kind = kind;
            IconType = kind == QuickSlotContentKind.Item ? IconConstants.Type.Item : IconConstants.Type.Skill;
            return base.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, 0, iconInstanceId);
        }

        public void ClearEntry()
        {
            _kind = QuickSlotContentKind.None;
            IconType = IconConstants.Type.None;
            base.ChangeInfoByUid(0, 0, 0, false, 0, 0);
        }

        protected override void UpdateIconImage()
        {
            if (ImageIcon == null) return;

            if (_kind == QuickSlotContentKind.None || uid <= 0 || count <= 0)
            {
                ImageIcon.sprite = null;
                return;
            }

            var sprite = QuickSlotContentProviderRegistry.TryGetIconSprite(_kind, uid, GetLevel(), instanceId);
            ImageIcon.sprite = sprite;
        }

        // 퀵슬롯은 스킬/아이템 공용이므로 기본 path 기반 로더는 사용하지 않음
        protected override string GetIconImagePath() => null;
    }
}
