namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 전용 아이콘.
    /// - 스킬/아이템 모두 표시 가능 (ProviderRegistry 기반)
    /// - Core 는 Skill 패키지를 직접 참조하지 않는다.
    /// </summary>
    public class UIIconQuickSlot : UIIcon
    {
        protected override void Awake()
        {
            base.Awake();
            IconType = IconConstants.Type.QuickSlot; // 퀵슬롯은 컨텐츠 타입이 슬롯마다 달라질 수 있음
        }

        /// <summary>
        /// 퀵슬롯 엔트리를 적용한다.
        /// </summary>
        public bool ApplyEntry(IconConstants.Type iconType, int iconUid, int iconCount, int iconLevel = 0, bool iconIsLearn = false, long iconInstanceId = 0)
        {
            IconType = iconType;
            return ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, 0, iconInstanceId);
        }
        
        public override bool ChangeInfoByUid(
            int iconUid,
            int iconCount = 0,
            int iconLevel = 0,
            bool iconIsLearn = false,
            int remainCoolTime = 0,
            long iconInstanceId = 0)
        {
            if (!base.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, remainCoolTime, iconInstanceId))
                return false;

            UpdateInfo();
            return true;
        }

        public void ClearEntry()
        {
            IconType = IconConstants.Type.None;
            base.ChangeInfoByUid(0, 0, 0, false, 0, 0);
        }

        /// <summary>
        /// 각 아이콘 별로 처리한다.
        /// </summary>
        protected override void UpdateIconImage()
        {
        }

        // 퀵슬롯은 스킬/아이템 공용이므로 기본 path 기반 로더는 사용하지 않음
        protected override string GetIconImagePath() => null;
    }
}
