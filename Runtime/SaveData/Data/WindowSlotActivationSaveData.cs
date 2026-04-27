using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 - UIWindow 슬롯 활성화 정보입니다.
    /// 각 UIWindow의 기본 비활성 설정은 Inspector 값을 기준으로 유지하고, 구매 등으로 활성화된 슬롯만 저장합니다.
    /// </summary>
    public class WindowSlotActivationSaveData : DefaultData, ISaveData
    {
        /// <summary>
        /// UIWindow uid별 활성화된 슬롯 인덱스 목록입니다.
        /// Json 저장을 위해 public 필드로 유지합니다.
        /// </summary>
        public Dictionary<int, List<int>> ActiveSlotsByWindow = new Dictionary<int, List<int>>();

        /// <summary>
        /// 저장 컨테이너에서 UIWindow 슬롯 활성화 정보를 복원합니다.
        /// </summary>
        /// <param name="loader">테이블 로더입니다. 현재 슬롯 활성화 데이터는 테이블을 사용하지 않습니다.</param>
        /// <param name="saveDataContainer">로드된 저장 데이터 컨테이너입니다.</param>
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            ActiveSlotsByWindow.Clear();
            if (saveDataContainer?.WindowSlotActivationSaveData?.ActiveSlotsByWindow == null)
            {
                return;
            }

            foreach (var pair in saveDataContainer.WindowSlotActivationSaveData.ActiveSlotsByWindow)
            {
                if (pair.Key <= 0 || pair.Value == null)
                {
                    continue;
                }

                var slots = new List<int>();
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    int slotIndex = pair.Value[i];
                    if (slotIndex < 0 || slots.Contains(slotIndex))
                    {
                        continue;
                    }

                    slots.Add(slotIndex);
                }

                if (slots.Count > 0)
                {
                    ActiveSlotsByWindow[pair.Key] = slots;
                }
            }
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯이 저장 데이터에서 활성화되어 있는지 확인합니다.
        /// </summary>
        /// <param name="windowUid">확인할 UIWindow uid입니다.</param>
        /// <param name="slotIndex">확인할 슬롯 인덱스입니다.</param>
        /// <returns>저장된 활성 슬롯이면 true입니다.</returns>
        public bool IsActivated(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            int uidValue = (int)windowUid;
            return uidValue > 0 &&
                   slotIndex >= 0 &&
                   ActiveSlotsByWindow.TryGetValue(uidValue, out var slots) &&
                   slots != null &&
                   slots.Contains(slotIndex);
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯의 저장 활성 상태를 변경합니다.
        /// 상태가 변경되면 저장 요청을 예약합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow uid입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <param name="activated">활성 저장 여부입니다.</param>
        /// <returns>상태가 실제로 변경되었으면 true입니다.</returns>
        public bool SetActivated(UIWindowConstants.WindowUid windowUid, int slotIndex, bool activated)
        {
            int uidValue = (int)windowUid;
            if (uidValue <= 0 || slotIndex < 0)
            {
                return false;
            }

            bool changed = activated
                ? AddActivatedSlot(uidValue, slotIndex)
                : RemoveActivatedSlot(uidValue, slotIndex);

            if (changed)
            {
                SaveDatas();
            }

            return changed;
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯을 활성 저장 상태로 변경합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow uid입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <returns>상태가 새로 변경되었으면 true입니다.</returns>
        public bool Activate(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            return SetActivated(windowUid, slotIndex, true);
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯의 활성 저장 상태를 해제합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow uid입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <returns>상태가 새로 변경되었으면 true입니다.</returns>
        public bool Deactivate(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            return SetActivated(windowUid, slotIndex, false);
        }

        /// <summary>
        /// 저장 목록에 활성 슬롯 인덱스를 추가합니다.
        /// </summary>
        /// <param name="uidValue">대상 UIWindow uid 값입니다.</param>
        /// <param name="slotIndex">추가할 슬롯 인덱스입니다.</param>
        /// <returns>새 슬롯이 추가되었으면 true입니다.</returns>
        private bool AddActivatedSlot(int uidValue, int slotIndex)
        {
            if (!ActiveSlotsByWindow.TryGetValue(uidValue, out var slots) || slots == null)
            {
                slots = new List<int>();
                ActiveSlotsByWindow[uidValue] = slots;
            }

            if (slots.Contains(slotIndex))
            {
                return false;
            }

            slots.Add(slotIndex);
            slots.Sort();
            return true;
        }

        /// <summary>
        /// 저장 목록에서 활성 슬롯 인덱스를 제거합니다.
        /// </summary>
        /// <param name="uidValue">대상 UIWindow uid 값입니다.</param>
        /// <param name="slotIndex">제거할 슬롯 인덱스입니다.</param>
        /// <returns>기존 슬롯이 제거되었으면 true입니다.</returns>
        private bool RemoveActivatedSlot(int uidValue, int slotIndex)
        {
            if (!ActiveSlotsByWindow.TryGetValue(uidValue, out var slots) || slots == null)
            {
                return false;
            }

            bool removed = slots.Remove(slotIndex);
            if (removed && slots.Count <= 0)
            {
                ActiveSlotsByWindow.Remove(uidValue);
            }

            return removed;
        }

        protected override int GetMaxSlotCount()
        {
            return 0;
        }
    }
}
