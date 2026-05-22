using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 말풍선 입력 대기 상태에서 다음 연출로 진행하는 정책을 정의합니다.
    /// </summary>
    public enum DialogueBalloonAdvancePolicy
    {
        /// <summary>
        /// 기존 정책입니다.
        /// 유저 입력이 들어오면 즉시 다음 연출로 진행할 수 있습니다.
        /// </summary>
        LegacyImmediate = 0,

        /// <summary>
        /// 말풍선 클립 길이(이벤트 duration)까지는 다음 연출로 진행하지 않습니다.
        /// 입력이 먼저 들어와도 클립 시간이 충족될 때까지 대기합니다.
        /// </summary>
        WaitUntilClipDuration = 1,
    }

    /// <summary>
    /// 말풍선 썸네일의 좌우 반전(Flip) 적용 정책을 정의합니다.
    /// </summary>
    public enum DialogueBalloonThumbnailFlipPolicy
    {
        /// <summary>
        /// 원본 스프라이트 방향을 유지합니다.
        /// </summary>
        KeepOriginal = 0,

        /// <summary>
        /// 항상 좌우 반전을 적용합니다.
        /// </summary>
        ForceFlip = 1,

        /// <summary>
        /// 썸네일 배치 위치(좌/우)에 따라 말풍선을 향하도록 자동 반전합니다.
        /// </summary>
        AutoByThumbnailPosition = 2,

        /// <summary>
        /// 말풍선 화자의 현재 바라보기 방향을 기준으로 자동 반전합니다.
        /// </summary>
        AutoBySpeakerFacing = 3,
    }

    /// <summary>
    /// 썸네일 원본 스프라이트가 기본적으로 바라보는 수평 방향을 정의합니다.
    /// </summary>
    public enum DialogueBalloonThumbnailSourceFacing
    {
        /// <summary>
        /// 원본 스프라이트가 오른쪽을 바라보는 기준입니다.
        /// </summary>
        Right = 0,

        /// <summary>
        /// 원본 스프라이트가 왼쪽을 바라보는 기준입니다.
        /// </summary>
        Left = 1,
    }

    /// <summary>
    /// 컷신에서 캐릭터 위에 표시할 대사 말풍선 데이터를 정의합니다.
    /// </summary>
    [Serializable]
    public class DialogueBalloonData
    {
        /// <summary>
        /// 타자 효과 속도가 지정되지 않았을 때 사용할 기본 초당 글자 수입니다.
        /// </summary>
        public const float DefaultTypewriterCharactersPerSecond = 30f;

        [Header("타겟")]
        [Tooltip("카메라가 타겟을 따라갈 것인지")]
        public bool isFollowTarget = false;
        [Tooltip("캐릭터 타입")]
        public CharacterConstants.Type characterType;
        [Tooltip("npc, monster 테이블의 고유번호")]
        public int characterUid;
        
        [Header("메시지 텍스트")]
        [Tooltip("말풍선 내용")]
        public string message;
        [Tooltip("폰트 크기")]
        public float fontSize;

        [Header("타자 효과")]
        [Tooltip("말풍선 내용을 한 글자씩 표시할지 여부")]
        public bool useTypewriter;
        [Tooltip("타자 효과일 때 초당 표시할 글자 수")]
        public float typewriterCharactersPerSecond = DefaultTypewriterCharactersPerSecond;

        [Header("진행 대기")]
        [Tooltip("true이면 유저 입력을 받을 때까지 컷신 타임라인 진행을 대기합니다.")]
        public bool waitForUserInput;
        [Tooltip("Wait For User Input 활성화 시 다음 연출로 넘어가는 정책입니다.")]
        public DialogueBalloonAdvancePolicy advancePolicy = DialogueBalloonAdvancePolicy.LegacyImmediate;

        [Header("말풍선 유지 중 애니메이션")]
        [Tooltip("true이면 말풍선이 유지되는 동안 대상 캐릭터 애니메이션을 반복 재생합니다.")]
        public bool useTalkLoopAnimation;
        [Tooltip("말풍선 유지 중 반복 재생할 애니메이션 이름입니다.")]
        public string talkLoopAnimationName;
        [Tooltip("말풍선 유지 중 반복 재생할 애니메이션 대상입니다. 설정되지 않으면 말풍선 화자를 기본 대상으로 사용합니다.")]
        public CutsceneCharacterReference talkLoopAnimationTarget = new CutsceneCharacterReference();
        [Tooltip("말풍선 유지 중 반복 재생할 애니메이션 속도입니다.")]
        public float talkLoopAnimationTimeScale = 1f;
        [Tooltip("말풍선 종료 시 루프 애니메이션을 대기 애니메이션으로 복원할지 여부입니다.")]
        public bool restoreTalkLoopAnimationOnStop = true;

        [Header("썸네일")]
        [Tooltip("말풍선 썸네일 표시 위치입니다. None이면 썸네일을 표시하지 않습니다.")]
        public ConfigCommon.ThumbnailPositionType thumbnailPositionType = ConfigCommon.ThumbnailPositionType.None;
        [Tooltip("테이블 썸네일 대신 사용할 썸네일 이미지 이름입니다. 비어 있으면 캐릭터 타입/UID 기준 썸네일을 사용합니다.")]
        public string thumbnailImage;
        [Tooltip("오른쪽 기준 썸네일 위치 보정값입니다.")]
        public Vector3 offsetImageThumbnailCharacter;
        [Tooltip("왼쪽 기준 썸네일 위치 보정값입니다.")]
        public Vector3 offsetImageThumbnailCharacterLeft;
        [Tooltip("썸네일 Flip 적용 정책입니다.")]
        public DialogueBalloonThumbnailFlipPolicy thumbnailFlipPolicy = DialogueBalloonThumbnailFlipPolicy.KeepOriginal;
        [Tooltip("원본 썸네일 이미지의 기본 바라보기 방향입니다.")]
        public DialogueBalloonThumbnailSourceFacing thumbnailSourceFacing = DialogueBalloonThumbnailSourceFacing.Right;

        [Header("말풍선 레이아웃")]
        [Tooltip("true이면 말꼬리 X를 기준으로 패널+썸네일 전체를 좌우 대칭 배치합니다.")]
        public bool useSymmetricLayoutByTail = true;
        [Tooltip("말꼬리를 화자 얼굴 바라보기 방향의 앞쪽으로 이동할 픽셀 오프셋입니다.")]
        public float tailForwardOffsetPx = 3f;
        [Tooltip("말꼬리 중심 기준 최소 반너비(px)입니다. 0 이하면 최소 크기 강제를 사용하지 않습니다.")]
        public float minHalfExtentByTailPx = 0f;
        [Tooltip("썸네일이 없는 쪽 텍스트 패딩(px)입니다.")]
        public int textPaddingOnNonThumbnailSidePx = 7;
        [Tooltip("썸네일이 있는 쪽 텍스트 패딩(px)입니다.")]
        public int textPaddingOnThumbnailSidePx = 3;
        [Tooltip("패널과 썸네일 사이 간격(px)입니다.")]
        public float thumbnailGapPx = 0f;

        /// <summary>
        /// 타자 효과 속도가 지정되지 않았으면 기본값으로 보정해서 반환합니다.
        /// </summary>
        /// <returns>0보다 큰 유효한 초당 글자 수입니다.</returns>
        public float GetSafeTypewriterCharactersPerSecond()
        {
            return typewriterCharactersPerSecond > 0f
                ? typewriterCharactersPerSecond
                : DefaultTypewriterCharactersPerSecond;
        }

        /// <summary>
        /// 말풍선 루프 애니메이션 속도가 유효하지 않으면 기본값(1.0)으로 보정해서 반환합니다.
        /// </summary>
        /// <returns>0보다 큰 유효한 애니메이션 재생 속도입니다.</returns>
        public float GetSafeTalkLoopAnimationTimeScale()
        {
            return talkLoopAnimationTimeScale > 0f
                ? talkLoopAnimationTimeScale
                : 1f;
        }

        /// <summary>
        /// Wait For User Input 상황에서 클립 길이만큼 최소 대기를 강제하는지 여부를 반환합니다.
        /// </summary>
        /// <returns>클립 길이 게이트 정책이면 <see langword="true"/>를 반환합니다.</returns>
        public bool ShouldWaitUntilClipDuration()
        {
            return advancePolicy == DialogueBalloonAdvancePolicy.WaitUntilClipDuration;
        }

        /// <summary>
        /// 말꼬리 전방 오프셋 값을 0 이상으로 보정해 반환합니다.
        /// </summary>
        /// <returns>0 이상인 말꼬리 전방 오프셋(px)입니다.</returns>
        public float GetSafeTailForwardOffsetPx()
        {
            return Mathf.Max(0f, tailForwardOffsetPx);
        }

        /// <summary>
        /// 말꼬리 기준 최소 반너비를 0 이상으로 보정해 반환합니다.
        /// </summary>
        /// <returns>0 이상인 최소 반너비(px)입니다.</returns>
        public float GetSafeMinHalfExtentByTailPx()
        {
            return Mathf.Max(0f, minHalfExtentByTailPx);
        }

        /// <summary>
        /// 썸네일이 없는 쪽 텍스트 패딩을 0 이상으로 보정해 반환합니다.
        /// </summary>
        /// <returns>0 이상인 패딩(px)입니다.</returns>
        public int GetSafeTextPaddingOnNonThumbnailSidePx()
        {
            return Mathf.Max(0, textPaddingOnNonThumbnailSidePx);
        }

        /// <summary>
        /// 썸네일이 있는 쪽 텍스트 패딩을 0 이상으로 보정해 반환합니다.
        /// </summary>
        /// <returns>0 이상인 패딩(px)입니다.</returns>
        public int GetSafeTextPaddingOnThumbnailSidePx()
        {
            return Mathf.Max(0, textPaddingOnThumbnailSidePx);
        }

        /// <summary>
        /// 패널과 썸네일 사이 간격 값을 0 이상으로 보정해 반환합니다.
        /// </summary>
        /// <returns>0 이상인 간격(px)입니다.</returns>
        public float GetSafeThumbnailGapPx()
        {
            return Mathf.Max(0f, thumbnailGapPx);
        }
    }
}
