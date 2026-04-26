namespace GGemCo2DCore
{
    /// <summary>
    /// 인벤토리를 특정 선택 작업의 후보 목록으로 사용할 때 필요한 위임 계약입니다.
    /// Core 인벤토리는 이 인터페이스만 알고, 실제 장착/등록 정책은 각 상위 기능이 구현합니다.
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
        /// 선택된 아이콘에 대해 실제 문맥 작업을 실행합니다.
        /// </summary>
        ResultCommon Execute(UIIconItem icon);

        /// <summary>
        /// 인벤토리가 일반 모드로 돌아가거나 닫힐 때 문맥이 정리할 수 있는 훅입니다.
        /// </summary>
        void OnClosed();
    }
}
