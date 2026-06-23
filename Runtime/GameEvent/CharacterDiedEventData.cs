using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 사망 상태 전환이 확정되었을 때 전달되는 이벤트 데이터입니다.
    /// </summary>
    public readonly struct CharacterDiedEventData
    {
        /// <summary>
        /// 사망한 캐릭터입니다.
        /// </summary>
        public readonly CharacterBase Character;

        /// <summary>
        /// 사망 원인입니다.
        /// </summary>
        public readonly CharacterConstants.DieReasonType DieReasonType;

        /// <summary>
        /// 사망을 유발한 공격자 오브젝트입니다.
        /// </summary>
        public readonly GameObject Attacker;

        /// <summary>
        /// 사망 확정 시점의 월드 좌표입니다.
        /// </summary>
        public readonly Vector3 WorldPosition;

        /// <summary>
        /// 이벤트가 생성된 실시간 시각입니다.
        /// </summary>
        public readonly double TimeRealtimeSinceStartup;

        /// <summary>
        /// 캐릭터 사망 이벤트 데이터를 생성합니다.
        /// </summary>
        /// <param name="character">사망한 캐릭터입니다.</param>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        public CharacterDiedEventData(
            CharacterBase character,
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker)
        {
            Character = character;
            DieReasonType = dieReasonType;
            Attacker = attacker;
            WorldPosition = character != null ? character.transform.position : Vector3.zero;
            TimeRealtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        }
    }
}
