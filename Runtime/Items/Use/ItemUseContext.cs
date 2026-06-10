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

        /// <summary>패시브 스킬 지급 연동(없으면 GrantSkillPassive Action은 실패 처리)</summary>
        public IItemUseSkillPassiveReceiver SkillPassiveReceiver { get; }

        /// <summary>MP 회복 규칙 연동(없으면 Core 기본 MP 회복 정책 사용)</summary>
        public IItemUseMpReceiver MpReceiver { get; }

        public ItemUseContext(SceneGame sceneGame, Player player, PlayerData playerData,
            InventoryData inventory, int slotIndex, int itemUid, int consumeCount,
            IItemUseSkillReceiver skillReceiver,
            GameObject targetObject = null,
            IItemUseSkillPassiveReceiver skillPassiveReceiver = null,
            IItemUseMpReceiver mpReceiver = null)
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
            SkillPassiveReceiver = skillPassiveReceiver;
            MpReceiver = mpReceiver;
        }
    }

    /// <summary>
    /// 아이템 사용으로 MP를 회복할 때 게임별 MP 상한 규칙을 적용하기 위한 수신자 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// <para>Core는 기본적으로 <see cref="CharacterStat.MaxMp"/> 기준으로 MP를 회복합니다.</para>
    /// <para>게임별로 하트 개수, 전투 규칙, 임시 자원 등 별도 상한을 사용해야 하는 경우 플레이어 오브젝트의 컴포넌트가 이 인터페이스를 구현합니다.</para>
    /// </remarks>
    public interface IItemUseMpReceiver
    {
        /// <summary>
        /// 지정한 양만큼 MP를 회복할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="amount">회복하려는 MP 양입니다.</param>
        /// <returns>회복 가능하면 true입니다.</returns>
        bool CanAddMp(int amount);

        /// <summary>
        /// 지정한 양만큼 MP 회복을 시도합니다.
        /// </summary>
        /// <param name="amount">회복하려는 MP 양입니다.</param>
        /// <returns>실제로 MP가 변경되면 true입니다.</returns>
        bool TryAddMp(int amount);
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

    /// <summary>
    /// "아이템 사용 → 패시브 스킬 지급" 연동을 위한 최소 인터페이스입니다.
    /// Skill 패키지에서 저장 데이터 또는 컨트롤러가 이 인터페이스를 구현하면 Core의 GrantSkillPassive 액션과 연결됩니다.
    /// </summary>
    public interface IItemUseSkillPassiveReceiver
    {
        /// <summary>
        /// 패시브 스킬 지급을 시도합니다.
        /// </summary>
        /// <param name="skillPassiveUid">지급할 패시브 스킬 UID입니다.</param>
        /// <param name="level">지급할 레벨입니다.</param>
        /// <param name="messageKey">실패 시 표시할 시스템 메시지 키입니다.</param>
        /// <returns>지급에 성공하면 true입니다.</returns>
        bool TryGrantPassiveSkill(int skillPassiveUid, int level, out string messageKey);

        /// <summary>
        /// 이미 보유한 패시브 스킬인지 확인합니다.
        /// </summary>
        /// <param name="skillPassiveUid">확인할 패시브 스킬 UID입니다.</param>
        /// <returns>이미 보유 중이면 true입니다.</returns>
        bool HasPassiveSkill(int skillPassiveUid);
    }

    /// <summary>
    /// GrantSkillPassive 중복 정책 확장을 위한 선택 인터페이스입니다.
    /// 구현체가 있으면 dup=LevelUp 정책을 처리할 수 있습니다.
    /// </summary>
    public interface IItemUseSkillPassiveReceiverEx : IItemUseSkillPassiveReceiver
    {
        /// <summary>
        /// 이미 보유한 패시브 스킬의 레벨을 올립니다.
        /// </summary>
        /// <param name="skillPassiveUid">레벨을 올릴 패시브 스킬 UID입니다.</param>
        /// <param name="addLevel">추가할 레벨입니다.</param>
        /// <param name="messageKey">실패 시 표시할 시스템 메시지 키입니다.</param>
        /// <returns>레벨업에 성공하면 true입니다.</returns>
        bool TryLevelUpPassiveSkill(int skillPassiveUid, int addLevel, out string messageKey);
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
