using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인벤토리를 특정 선택 작업의 후보 목록으로 사용할 때 필요한 위임 계약입니다.
    /// Core 인벤토리는 이 인터페이스만 알고, 실제 장착/해제/등록 정책은 각 상위 기능이 구현합니다.
    /// </summary>
    public interface IInventorySelectionContext
    {
        /// <summary>
        /// 현재 문맥이 유효한지 확인합니다.
        /// false 이면 인벤토리는 일반 모드처럼 동작합니다.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 문맥 실행 버튼에 표시할 메시지 키입니다.
        /// </summary>
        string ActionMessageKey { get; }

        /// <summary>
        /// 문맥 해제 버튼에 표시할 메시지 키입니다.
        /// </summary>
        string UnequipMessageKey { get; }

        /// <summary>
        /// 인벤토리 슬롯의 저장 데이터와 아이템 테이블 데이터를 기준으로 후보 표시 여부를 결정합니다.
        /// </summary>
        bool CanDisplay(SaveDataIcon itemData, StruckTableItem itemTableData);

        /// <summary>
        /// 현재 선택된 아이콘으로 문맥 작업을 실행할 수 있는지 검사합니다.
        /// </summary>
        bool CanExecute(UIIconItem icon, out string failMessageKey);

        /// <summary>
        /// 아이콘이 이미 이 문맥 기준으로 사용 중인지 표시용으로 판정합니다.
        /// </summary>
        bool IsEquipped(UIIconItem icon);

        /// <summary>
        /// 인벤토리가 열릴 때 기본으로 선택할 아이템을 제공합니다.
        /// 스킬 슬롯 문맥에서는 현재 열었던 슬롯에 이미 장착된 아이템을 기본 선택 대상으로 반환합니다.
        /// </summary>
        bool TryGetDefaultSelection(out int itemUid, out long itemInstanceId);

        /// <summary>
        /// 선택된 아이콘에 대해 실제 문맥 작업을 실행합니다.
        /// </summary>
        ResultCommon Execute(UIIconItem icon);

        /// <summary>
        /// 현재 선택 아이콘이 이 문맥의 해제 대상과 같은지 검사합니다.
        /// 스킬 슬롯 문맥에서는 인벤토리를 열었던 슬롯에 장착된 아이템과 현재 선택한 아이템이 같아야 합니다.
        /// </summary>
        bool CanUnequip(UIIconItem icon, out string failMessageKey);

        /// <summary>
        /// 현재 선택 아이콘과 일치하는 문맥의 장착 데이터를 해제합니다.
        /// Core는 해제 대상이 어떤 저장 데이터인지는 모르고, 구현체가 실제 정책을 수행합니다.
        /// </summary>
        ResultCommon Unequip(UIIconItem icon);

        /// <summary>
        /// 인벤토리가 일반 모드로 돌아가거나 닫힐 때 문맥이 정리할 수 있는 훅입니다.
        /// </summary>
        void OnClosed();
    }

    /// <summary>
    /// 인벤토리 선택 문맥에서 아이템을 실행했을 때 개수를 차감할지 결정합니다.
    /// 기본값은 선택한 아이템을 1개 소비하는 방식입니다.
    /// </summary>
    public enum InventorySelectionConsumeMode
    {
        Consume,
        Keep,
    }

    /// <summary>
    /// 아이템 개수가 0이 되었을 때 인벤토리 후보 목록에 계속 보여줄지 결정합니다.
    /// </summary>
    public enum InventorySelectionZeroCountItemVisibility
    {
        HideItem,
        ShowItem,
    }

    /// <summary>
    /// 아이템 개수가 0이 되었을 때 개수 텍스트를 표시할지 결정합니다.
    /// </summary>
    public enum InventorySelectionZeroCountTextVisibility
    {
        HideText,
        ShowZero,
    }

    /// <summary>
    /// 인벤토리 선택 문맥의 아이템 소비와 0개 표시 방식을 묶은 정책입니다.
    /// 이 정책은 일반 인벤토리 동작이 아니라 OpenWithContext 로 열린 선택 모드에서만 사용합니다.
    /// </summary>
    [Serializable]
    public sealed class InventorySelectionConsumePolicy
    {
        public InventorySelectionConsumeMode consumeMode = InventorySelectionConsumeMode.Consume;
        public int consumeCount = 1;
        public InventorySelectionZeroCountItemVisibility zeroCountItemVisibility =
            InventorySelectionZeroCountItemVisibility.HideItem;
        public InventorySelectionZeroCountTextVisibility zeroCountTextVisibility =
            InventorySelectionZeroCountTextVisibility.HideText;

        public bool ShouldConsume() => consumeMode == InventorySelectionConsumeMode.Consume;

        public int GetConsumeCount() => Math.Max(1, consumeCount);

        public bool ShouldDisplayZeroCountItem() =>
            zeroCountItemVisibility == InventorySelectionZeroCountItemVisibility.ShowItem;

        public bool ShouldShowZeroCountText() =>
            zeroCountTextVisibility == InventorySelectionZeroCountTextVisibility.ShowZero;
    }

    /// <summary>
    /// 선택 문맥이 0개 아이템 표시 정책을 인벤토리 UI에 알려주기 위한 선택 확장 인터페이스입니다.
    /// 구현하지 않은 문맥은 기존처럼 0개 아이템을 숨깁니다.
    /// </summary>
    public interface IInventorySelectionZeroCountDisplayPolicy
    {
        bool ShouldDisplayZeroCountItem(SaveDataIcon itemData, StruckTableItem itemTableData);
        bool ShouldShowZeroCountText(SaveDataIcon itemData, StruckTableItem itemTableData);
    }
}
