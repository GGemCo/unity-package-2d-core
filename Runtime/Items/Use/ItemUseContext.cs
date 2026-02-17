using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 사용 실행 컨텍스트
    /// - Core는 "데이터/서비스" 중심으로 설계하고,
    ///   스킬 지급/어펙트 적용 등 외부 패키지 연동은 인터페이스/브리지로 확장합니다.
    /// </summary>
    public sealed class ItemUseContext
    {
        public SceneGame SceneGame { get; }
        public Player Player { get; }
        public PlayerData PlayerData { get; }

        /// <summary>
        /// 효과 적용 대상(기본값: Player.gameObject)
        /// - ApplyAffect 등 대상 기반 Action에서 사용합니다.
        /// </summary>
        public GameObject TargetObject { get; }

        public InventoryData Inventory { get; }
        public int SlotIndex { get; }
        public int ItemUid { get; }
        public int ConsumeCount { get; }

        /// <summary>스킬 지급 연동(없으면 GrantSkill Action은 실패 처리)</summary>
        public IItemUseSkillReceiver SkillReceiver { get; }

        public ItemUseContext(SceneGame sceneGame, Player player, PlayerData playerData,
            InventoryData inventory, int slotIndex, int itemUid, int consumeCount,
            IItemUseSkillReceiver skillReceiver,
            GameObject targetObject = null)
        {
            SceneGame = sceneGame;
            Player = player;
            PlayerData = playerData;

            TargetObject = targetObject != null ? targetObject : (player != null ? player.gameObject : null);

            Inventory = inventory;
            SlotIndex = slotIndex;
            ItemUid = itemUid;
            ConsumeCount = consumeCount;
            SkillReceiver = skillReceiver;
        }
    }

    /// <summary>
    /// "아이템 사용 → 스킬 지급" 연동을 위한 최소 인터페이스
    /// - Skill 패키지에서 Player/SkillManager 등이 이 인터페이스를 구현하면
    ///   Core의 ItemUseActionGrantSkill이 자연스럽게 연결됩니다.
    /// </summary>
    public interface IItemUseSkillReceiver
    {
        /// <summary>
        /// 스킬 지급을 시도합니다.
        /// - success=true면 지급 완료
        /// - false면 messageKey(시스템 메시지 키) 반환
        /// </summary>
        bool TryGrantSkill(int skillUid, int level, out string messageKey);

        /// <summary>이미 보유한 스킬인지 여부</summary>
        bool HasSkill(int skillUid);
    }

    /// <summary>
    /// GrantSkill 중복 정책 확장을 위한 선택 인터페이스(옵션)
    /// - Skill 패키지에서 구현하면 LevelUp 같은 정책을 자연스럽게 지원할 수 있습니다.
    /// - 미구현 시에는 Core가 가능한 범위(Ignore/Fail/대체보상)만 처리합니다.
    /// </summary>
    public interface IItemUseSkillReceiverEx : IItemUseSkillReceiver
    {
        /// <summary>
        /// 이미 보유한 스킬의 레벨을 올립니다.
        /// </summary>
        bool TryLevelUpSkill(int skillUid, int addLevel, out string messageKey);
    }

    public enum SkillDuplicatePolicy
    {
        /// <summary>중복이면 실패</summary>
        Fail = 0,
        /// <summary>중복이면 아무 것도 하지 않고 성공</summary>
        Ignore = 1,
        /// <summary>중복이면 레벨업</summary>
        LevelUp = 2,
        /// <summary>중복이면 대체 보상 지급 후 성공</summary>
        AlternativeReward = 3,
    }
}
