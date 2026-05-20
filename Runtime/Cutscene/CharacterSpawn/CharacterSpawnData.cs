using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 생성 위치 계산 방식을 정의합니다.
    /// </summary>
    public enum CutsceneCharacterSpawnPositionMode
    {
        /// <summary>
        /// 절대 좌표를 직접 사용합니다.
        /// </summary>
        WorldPosition = 0,

        /// <summary>
        /// 플레이어 기준 방향/거리로 상대 위치를 계산합니다.
        /// </summary>
        RelativeToPlayer = 1,
    }

    /// <summary>
    /// 컷신에서 캐릭터를 생성할 때 사용하는 설정 데이터입니다.
    /// </summary>
    [Serializable]
    public class CharacterSpawnData
    {
        [Header("Target")]
        [Tooltip("생성할 캐릭터 타입입니다. Player는 지원하지 않으며 Monster/Npc를 사용합니다.")]
        public CharacterConstants.Type characterType;

        [Tooltip("생성할 캐릭터 uid입니다.")]
        public int characterUid;

        [Tooltip("0보다 크면 생성 직후 캐릭터 크기를 강제로 설정합니다.")]
        public float characterScale;

        [Header("Spawn Position")]
        [Tooltip("생성 위치 계산 방식입니다.")]
        public CutsceneCharacterSpawnPositionMode positionMode = CutsceneCharacterSpawnPositionMode.WorldPosition;

        [Tooltip("positionMode가 WorldPosition일 때 사용할 절대 좌표입니다.")]
        public Vec2 worldPosition;

        [Tooltip("positionMode가 RelativeToPlayer일 때 플레이어 기준 방향입니다.")]
        public CharacterConstants.FacingDirection8 playerRelativeDirection = CharacterConstants.FacingDirection8.Right;

        [Tooltip("positionMode가 RelativeToPlayer일 때 플레이어 기준 거리입니다.")]
        public float playerRelativeDistance = 1f;

        [Tooltip("최종 계산 위치에 추가할 오프셋입니다.")]
        public Vec2 positionOffset;

        [Header("Presentation")]
        [Tooltip("true이면 생성 후 즉시 표시하고, false이면 비활성 상태로 유지합니다.")]
        public bool spawnVisible = true;

        /// <summary>
        /// 컷신 종료 시 생성된 캐릭터를 맵 배치 캐릭터로 정착시킬지 여부를 제어합니다.
        /// </summary>
        [Header("Lifecycle")]
        [Tooltip("true이면 컷신 종료 후에도 맵 배치 캐릭터로 정착시킵니다. false이면 컷신 종료 시 제거합니다.")]
        public bool settleToMapOnCutsceneEnd = true;
    }
}
