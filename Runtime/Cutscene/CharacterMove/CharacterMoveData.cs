using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CharacterMove 이벤트의 이동 경로 계산 방식을 정의합니다.
    /// </summary>
    public enum CutsceneCharacterMoveMode
    {
        /// <summary>
        /// 시작/종료 좌표를 직접 지정해 이동합니다.
        /// </summary>
        AbsolutePosition = 0,

        /// <summary>
        /// 현재 위치를 기준으로 방향/거리만큼 상대 이동합니다.
        /// </summary>
        RelativeFromCurrent = 1,

        /// <summary>
        /// 플레이어 위치를 기준으로 방향/거리만큼 상대 이동합니다.
        /// </summary>
        RelativeFromPlayer = 2,
    }

    /// <summary>
    /// CharacterMove 이벤트 실행 시 캐릭터의 바라보기 처리 방식을 정의합니다.
    /// </summary>
    public enum CutsceneCharacterMoveFacingMode
    {
        /// <summary>
        /// 현재 바라보는 방향을 유지합니다.
        /// </summary>
        KeepCurrent = 0,

        /// <summary>
        /// 이동 벡터를 기반으로 자동으로 바라보기 방향을 결정합니다.
        /// </summary>
        FaceMoveDirection = 1,

        /// <summary>
        /// 지정한 고정 방향으로 바라보기를 강제합니다.
        /// </summary>
        FaceExplicit = 2,
    }

    [Serializable]
    public class CharacterMoveData
    {
        [Header("타겟")]
        [Tooltip("카메라가 타겟을 따라갈 것인지")]
        public bool isFollowTarget = false;
        [Tooltip("캐릭터 타입")]
        public CharacterConstants.Type characterType;
        [Tooltip("npc, monster 테이블의 고유번호")]
        public int characterUid;
        [Tooltip("크기")]
        public float characterScale;
        [Tooltip("이동 속도")]
        public int characterMoveSpeed;

        [Header("이동 공통")]
        [Tooltip("이동 경로 계산 방식")]
        public CutsceneCharacterMoveMode moveMode = CutsceneCharacterMoveMode.AbsolutePosition;

        [Header("절대 이동")]
        [Tooltip("절대 이동 시작 좌표(0,0이면 현재 위치 사용)")]
        public Vec2 startPosition;
        [Tooltip("절대 이동 종료 좌표")]
        public Vec2 endPosition;

        [Header("상대 이동")]
        [Tooltip("상대 이동 기준점(현재 위치/플레이어 위치)에서 사용할 이동 방향")]
        public CharacterConstants.FacingDirection8 relativeDirection = CharacterConstants.FacingDirection8.Right;
        [Tooltip("상대 이동 기준점(현재 위치/플레이어 위치)에서 사용할 이동 거리")]
        public float relativeDistance = 0f;
        [Tooltip("상대 이동 계산 후 최종 위치에 더할 보정 오프셋")]
        public Vec2 relativeOffset;

        [Header("바라보기")]
        [Tooltip("이동 시작 시 바라보기 적용 방식")]
        public CutsceneCharacterMoveFacingMode facingMode = CutsceneCharacterMoveFacingMode.FaceMoveDirection;
        [Tooltip("facingMode가 FaceExplicit일 때 사용할 방향")]
        public CharacterConstants.FacingDirection8 explicitFacing = CharacterConstants.FacingDirection8.Right;
    }
}
