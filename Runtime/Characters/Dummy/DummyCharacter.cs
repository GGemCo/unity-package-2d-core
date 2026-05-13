using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬/연출에서 사용하는 더미 캐릭터 구현입니다.
    /// 원본 Monster/Npc 정보는 참조하되, 런타임 타입은 None으로 고정하여
    /// 전투/AI 전용 부가 로직의 자동 부착을 최소화합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DummyCharacter : CharacterBase
    {
        [SerializeField] private CharacterConstants.Type sourceType = CharacterConstants.Type.None;
        [SerializeField] private int sourceUid;

        /// <summary>
        /// 더미 생성 시 참조한 원본 캐릭터 타입을 반환합니다.
        /// </summary>
        public CharacterConstants.Type SourceType => sourceType;

        /// <summary>
        /// 더미 생성 시 참조한 원본 캐릭터 UID를 반환합니다.
        /// </summary>
        public int SourceUid => sourceUid;

        /// <summary>
        /// 더미 캐릭터의 원본 참조 정보를 기록합니다.
        /// </summary>
        /// <param name="type">원본 캐릭터 타입입니다.</param>
        /// <param name="uid">원본 캐릭터 UID입니다.</param>
        public void ConfigureSource(CharacterConstants.Type type, int uid)
        {
            sourceType = type;
            sourceUid = uid;
        }

        /// <summary>
        /// 더미 캐릭터의 런타임 타입을 None으로 고정한 뒤 공통 초기화를 수행합니다.
        /// </summary>
        protected override void Awake()
        {
            type = CharacterConstants.Type.None;
            base.Awake();
        }
    }
}
