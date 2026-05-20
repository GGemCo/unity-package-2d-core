using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CharacterTweenMove 이벤트에서 사용하는 캐릭터 이동 데이터입니다.
    /// Run 애니메이션/이동 속도 제어 없이 Duration + Easing 기반으로 위치를 보간합니다.
    /// </summary>
    [Serializable]
    public class CharacterTweenMoveData
    {
        [Header("타겟")]
        [Tooltip("카메라가 타겟을 따라갈 것인지")]
        public bool isFollowTarget = false;
        [Tooltip("캐릭터 타입")]
        public CharacterConstants.Type characterType;
        [Tooltip("npc, monster 테이블의 고유번호")]
        public int characterUid;

        [Header("이동 공통")]
        [Tooltip("이동 경로 계산 방식")]
        public CutsceneCharacterMoveMode moveMode = CutsceneCharacterMoveMode.AbsolutePosition;
        [Tooltip("이동 보간 곡선")]
        public Easing.EaseType easing = Easing.EaseType.Linear;

        [Header("절대 이동")]
        [Tooltip("절대 이동 시작 좌표(0,0이면 현재 위치 사용)")]
        public Vec2 startPosition;
        [Tooltip("절대 이동 종료 좌표")]
        public Vec2 endPosition;

        [Header("상대 이동")]
        [Tooltip("현재 위치 기준 이동 방향")]
        public CharacterConstants.FacingDirection8 relativeDirection = CharacterConstants.FacingDirection8.Right;
        [Tooltip("현재 위치 기준 이동 거리")]
        public float relativeDistance = 0f;
        [Tooltip("상대 이동 계산 후 추가 보정 오프셋")]
        public Vec2 relativeOffset;

        [Header("바라보기")]
        [Tooltip("이동 시작 시 바라보기 적용 방식")]
        public CutsceneCharacterMoveFacingMode facingMode = CutsceneCharacterMoveFacingMode.FaceMoveDirection;
        [Tooltip("facingMode가 FaceExplicit일 때 사용할 방향")]
        public CharacterConstants.FacingDirection8 explicitFacing = CharacterConstants.FacingDirection8.Right;
    }
}
