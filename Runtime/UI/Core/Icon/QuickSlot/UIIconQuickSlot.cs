using UnityEngine.EventSystems;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 전용 아이콘.
    /// - 스킬/아이템 모두 표시 가능 (ProviderRegistry 기반)
    /// - Core 는 Skill 패키지를 직접 참조하지 않는다.
    /// </summary>
    public class UIIconQuickSlot : UIIcon, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        protected override void Awake()
        {
            base.Awake();
            IconType = IconConstants.Type.QuickSlot;
        }

        public override bool ChangeInfoByUid(
            int iconUid,
            int iconCount = 0,
            int iconLevel = 0,
            bool iconIsLearn = false,
            int remainCoolTime = 0,
            long iconInstanceId = 0,
            IconConstants.Type iconType = IconConstants.Type.None)
        {
            if (!base.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, remainCoolTime, iconInstanceId,
                    iconType))
                return false;

            UpdateInfo();
            return true;
        }

        public void ClearEntry()
        {
            IconType = IconConstants.Type.None;
            base.ChangeInfoByUid(0, 0, 0, false, 0, 0);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowOverImage(true);
            HandlePointerEnterEffect(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ShowOverImage(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!PossibleClick || IsLock()) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (window != null)
                {
                    window.SetSelectedIcon(index);
                }
                HandlePointerClickEffect(eventData);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                window?.OnRightClick(this);
            }
        }

        protected override void UpdateIconImage()
        {
        }

        protected override string GetIconImagePath() => null;
    }
}
