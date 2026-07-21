using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 이펙트
    /// </summary>
    public class VfxEffectUI : VfxAnimationControllerSprite
    {
        [Tooltip("자동 재생 여부")]
        [SerializeField] private bool autoStart = true;
        [Tooltip("재생 속도")]
        [SerializeField] private  float timeScale;
        [Tooltip("크기")]
        [SerializeField] private  Vector3 scale = Vector3.one;
        
        protected override void OnEnable()
        {
            if (!autoStart) return;
            InitializeScale();
            Play(-1f, timeScale);
        }

        public void PlayEffect(bool forceReset = false)
        {
            InitializeScale();
            Play(-1f, timeScale, forceReset);
        }

        /// <summary>
        /// start 애니메이션 클립만 첫 프레임부터 한 번 재생합니다.
        /// </summary>
        /// <param name="forceReset">
        /// 같은 start 상태를 재생 중이어도 첫 프레임부터 다시 재생하려면 <see langword="true"/>입니다.
        /// </param>
        /// <returns>start 클립이 존재하여 재생을 시작했으면 <see langword="true"/>를 반환합니다.</returns>
        /// <remarks>
        /// start 이후 play/end 클립을 연결하지 않습니다.
        /// 클립 재생 완료 후에는 Animator가 start 클립의 마지막 프레임을 유지합니다.
        /// </remarks>
        public bool PlayStartClipOnce(bool forceReset = true)
        {
            InitializeScale();

            const string clipName = IVfxAnimationController.KeyClipNameStart;
            if (GetClipByName(clipName) == null)
            {
                return false;
            }

            // PlayAnimation이 기존 순차 재생 코루틴을 정리하므로
            // 파괴 연출 도중 복구되어도 start 클립만 독립적으로 다시 재생됩니다.
            PlayAnimation(clipName, timeScale: timeScale, forceReset: forceReset);
            return true;
        }
        
        /// <summary>
        /// end 애니메이션 클립만 첫 프레임부터 한 번 재생합니다.
        /// </summary>
        /// <param name="forceReset">
        /// 같은 end 상태를 재생 중이어도 첫 프레임부터 다시 재생하려면 <see langword="true"/>입니다.
        /// </param>
        /// <returns>start 클립이 존재하여 재생을 시작했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool PlayEndClipOnce(bool forceReset = true)
        {
            InitializeScale();

            const string clipName = IVfxAnimationController.KeyClipNameEnd;
            if (GetClipByName(clipName) == null)
            {
                return false;
            }

            // PlayAnimation이 기존 순차 재생 코루틴을 정리하므로
            // 파괴 연출 도중 복구되어도 start 클립만 독립적으로 다시 재생됩니다.
            PlayAnimation(clipName, timeScale: timeScale, forceReset: forceReset);
            return true;
        }

        /// <summary>
        /// UI 이펙트를 1회만 재생하고, 예상 재생 시간을 초 단위로 반환합니다.
        /// </summary>
        /// <param name="forceReset">같은 상태 재생 중이어도 첫 프레임부터 다시 재생할지 여부입니다.</param>
        /// <returns>
        /// 1회 재생 시작에 성공하면 예상 재생 시간(초)을 반환합니다.
        /// 재생 가능한 클립이 없으면 0을 반환합니다.
        /// </returns>
        public float PlayOneShotEffect(bool forceReset = true)
        {
            InitializeScale();
            bool started = Play(0f, timeScale, forceReset);
            if (!started)
            {
                return 0f;
            }

            return GetEstimatedOneShotDuration();
        }

        /// <summary>
        /// 현재 클립 구성(start/play/end)과 timeScale 설정을 기준으로
        /// 1회 재생 시 예상 소요 시간을 계산합니다.
        /// </summary>
        /// <returns>예상 소요 시간(초)입니다.</returns>
        public float GetEstimatedOneShotDuration()
        {
            const float epsilon = 0.0001f;
            float safeTimeScale = Mathf.Abs(timeScale) > epsilon ? Mathf.Abs(timeScale) : 1f;

            bool hasStart = GetClipByName(IVfxAnimationController.KeyClipNameStart) != null;
            bool hasPlay = GetClipByName(IVfxAnimationController.KeyClipNamePlay) != null;
            bool hasEnd = GetClipByName(IVfxAnimationController.KeyClipNameEnd) != null;

            float durationStart = hasStart ? GetAnimationDuration(IVfxAnimationController.KeyClipNameStart, false, false) : 0f;
            float durationPlay = hasPlay ? GetAnimationDuration(IVfxAnimationController.KeyClipNamePlay, false, false) : 0f;
            float durationEnd = hasEnd ? GetAnimationDuration(IVfxAnimationController.KeyClipNameEnd, false, false) : 0f;

            if (hasStart)
            {
                // duration=0 경로에서 start는 timeScale을 적용하고, add(play/end)는 기본 배속으로 재생된다.
                return (durationStart / safeTimeScale) + durationPlay + durationEnd;
            }

            if (hasPlay)
            {
                // start가 없으면 play가 시작 클립이 되며 timeScale이 적용된다.
                return (durationPlay / safeTimeScale) + durationEnd;
            }

            if (hasEnd)
            {
                // start/play가 없으면 end가 시작 클립이 되며 timeScale이 적용된다.
                return durationEnd / safeTimeScale;
            }

            return 0f;
        }

        private void InitializeScale()
        {
            if (scale == Vector3.zero || scale == Vector3.one) return;
            transform.localScale = scale;
        }
    }
}
