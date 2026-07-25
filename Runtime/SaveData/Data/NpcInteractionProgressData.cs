using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// NPC별 첫 인터랙션 대화 완료 상태를 저장합니다.
    /// NPC의 Interaction UID가 변경되면 새 첫 대화를 다시 진행할 수 있도록 두 UID를 함께 비교합니다.
    /// </summary>
    public sealed class NpcInteractionProgressData : DefaultData, ISaveData
    {
        /// <summary>
        /// NPC UID를 Key로, 첫 대화를 완료한 Interaction UID를 Value로 보관합니다.
        /// </summary>
        public Dictionary<int, int> CompletedFirstDialogueInteractionUidByNpcUid =
            new Dictionary<int, int>();

        /// <summary>
        /// 저장 컨테이너에서 유효한 NPC 첫 대화 완료 기록을 복원합니다.
        /// 기존 저장 파일에 데이터가 없으면 빈 기록으로 초기화합니다.
        /// </summary>
        /// <param name="saveDataContainer">로드된 Core 저장 데이터 컨테이너입니다.</param>
        public void Initialize(SaveDataContainer saveDataContainer = null)
        {
            CompletedFirstDialogueInteractionUidByNpcUid.Clear();

            Dictionary<int, int> loadedRecords =
                saveDataContainer?.NpcInteractionProgressData
                    ?.CompletedFirstDialogueInteractionUidByNpcUid;
            if (loadedRecords == null)
            {
                return;
            }

            foreach (KeyValuePair<int, int> pair in loadedRecords)
            {
                if (pair.Key <= 0 || pair.Value <= 0)
                {
                    continue;
                }

                CompletedFirstDialogueInteractionUidByNpcUid[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// 지정한 NPC가 현재 Interaction의 첫 대화를 완료했는지 확인합니다.
        /// </summary>
        /// <param name="npcUid">확인할 NPC UID입니다.</param>
        /// <param name="interactionUid">현재 NPC에 연결된 Interaction UID입니다.</param>
        /// <returns>동일한 Interaction의 첫 대화를 완료했으면 <see langword="true"/>입니다.</returns>
        public bool IsFirstDialogueCompleted(int npcUid, int interactionUid)
        {
            return npcUid > 0 &&
                   interactionUid > 0 &&
                   CompletedFirstDialogueInteractionUidByNpcUid != null &&
                   CompletedFirstDialogueInteractionUidByNpcUid.TryGetValue(
                       npcUid,
                       out int completedInteractionUid) &&
                   completedInteractionUid == interactionUid;
        }

        /// <summary>
        /// 지정한 NPC의 현재 Interaction 첫 대화를 완료 상태로 기록하고 저장을 요청합니다.
        /// 동일한 기록이 이미 있으면 중복 저장하지 않습니다.
        /// </summary>
        /// <param name="npcUid">완료 처리할 NPC UID입니다.</param>
        /// <param name="interactionUid">완료한 Interaction UID입니다.</param>
        /// <returns>새 기록이 반영되었으면 <see langword="true"/>입니다.</returns>
        public bool MarkFirstDialogueCompleted(int npcUid, int interactionUid)
        {
            if (npcUid <= 0 || interactionUid <= 0)
            {
                return false;
            }

            CompletedFirstDialogueInteractionUidByNpcUid ??= new Dictionary<int, int>();
            if (IsFirstDialogueCompleted(npcUid, interactionUid))
            {
                return false;
            }

            CompletedFirstDialogueInteractionUidByNpcUid[npcUid] = interactionUid;
            SaveDatas();
            return true;
        }

        /// <summary>
        /// 슬롯 기반 데이터가 아니므로 최대 슬롯 수로 0을 반환합니다.
        /// </summary>
        /// <returns>항상 0입니다.</returns>
        protected override int GetMaxSlotCount()
        {
            return 0;
        }
    }
}
