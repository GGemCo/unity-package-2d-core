using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindow 슬롯의 저장 기반 활성 상태를 조회하고 갱신합니다.
    /// </summary>
    internal sealed class UIWindowSlotActivationService
    {
        private readonly Func<UIWindowConstants.WindowUid, UIWindow> _getWindowByUid;
        private readonly Func<List<UIWindow>> _getManagedWindows;

        /// <summary>
        /// 슬롯 활성 상태 서비스를 생성합니다.
        /// </summary>
        /// <param name="getWindowByUid">UID로 UIWindow를 조회하는 함수입니다.</param>
        /// <param name="getManagedWindows">현재 관리 중인 UIWindow 목록을 반환하는 함수입니다.</param>
        public UIWindowSlotActivationService(
            Func<UIWindowConstants.WindowUid, UIWindow> getWindowByUid,
            Func<List<UIWindow>> getManagedWindows)
        {
            _getWindowByUid = getWindowByUid;
            _getManagedWindows = getManagedWindows;
        }

        /// <summary>
        /// Core SaveDataManager가 관리하는 UIWindow 슬롯 활성화 저장 데이터를 반환합니다.
        /// </summary>
        /// <returns>초기화되어 있으면 저장 데이터를 반환하고, 아니면 null을 반환합니다.</returns>
        private WindowSlotActivationSaveData GetWindowSlotActivationSaveData()
        {
            return SceneGame.Instance?.saveDataManager?.WindowSlotActivation;
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯이 저장 데이터에 의해 활성화되어 있는지 확인합니다.
        /// </summary>
        /// <param name="windowUid">확인할 UIWindow UID입니다.</param>
        /// <param name="slotIndex">확인할 슬롯 인덱스입니다.</param>
        /// <returns>저장된 활성 슬롯이면 true입니다.</returns>
        public bool IsWindowSlotActivated(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            WindowSlotActivationSaveData saveData = GetWindowSlotActivationSaveData();
            return saveData != null && saveData.IsActivated(windowUid, slotIndex);
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯의 저장 활성 상태를 변경하고 화면 표시를 갱신합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow UID입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <param name="activated">저장 활성 여부입니다.</param>
        /// <returns>저장 상태가 실제로 변경되었으면 true입니다.</returns>
        public bool SetWindowSlotActivated(
            UIWindowConstants.WindowUid windowUid,
            int slotIndex,
            bool activated)
        {
            WindowSlotActivationSaveData saveData = GetWindowSlotActivationSaveData();
            if (saveData == null)
            {
                return false;
            }

            bool changed = saveData.SetActivated(windowUid, slotIndex, activated);
            if (changed)
            {
                RefreshWindowSlotActivationState(windowUid, slotIndex);
            }

            return changed;
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯을 저장 활성 상태로 변경합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow UID입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <returns>저장 상태가 새로 변경되었으면 true입니다.</returns>
        public bool ActivateWindowSlot(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            return SetWindowSlotActivated(windowUid, slotIndex, true);
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯의 저장 활성 상태를 해제합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow UID입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <returns>저장 상태가 새로 변경되었으면 true입니다.</returns>
        public bool DeactivateWindowSlot(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            return SetWindowSlotActivated(windowUid, slotIndex, false);
        }

        /// <summary>
        /// 저장 활성 상태가 반영되도록 특정 UIWindow 슬롯의 비활성 표시를 갱신합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow UID입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        private void RefreshWindowSlotActivationState(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            UIWindow window = _getWindowByUid?.Invoke(windowUid);
            window?.RefreshInactiveSlotState(slotIndex);
        }

        /// <summary>
        /// 저장 활성 정보 복원 이후 모든 관리 UIWindow의 비활성 표시를 다시 반영합니다.
        /// </summary>
        public void RefreshWindowSlotActivationStates()
        {
            List<UIWindow> managedWindows = _getManagedWindows?.Invoke();
            if (managedWindows == null)
            {
                return;
            }

            for (int i = 0; i < managedWindows.Count; i++)
            {
                managedWindows[i]?.RefreshInactiveSlotStates();
            }
        }
    }
}
