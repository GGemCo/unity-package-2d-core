using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 재생 중인 사운드를 외부 수명주기에서 정지하기 위한 핸들입니다.
    /// </summary>
    public sealed class SoundPlaybackHandle
    {
        private readonly Action _stopAction;
        private bool _isStopped;

        /// <summary>
        /// 정지 가능한 재생 핸들을 생성합니다.
        /// </summary>
        /// <param name="stopAction">정지 요청 시 실행할 콜백입니다.</param>
        public SoundPlaybackHandle(Action stopAction)
        {
            _stopAction = stopAction;
        }

        /// <summary>
        /// 정지 가능한 실제 재생이 연결되어 있는지 여부입니다.
        /// </summary>
        public bool IsValid => _stopAction != null;

        /// <summary>
        /// 이미 정지 요청이 처리되었는지 여부입니다.
        /// </summary>
        public bool IsStopped => _isStopped;

        /// <summary>
        /// 재생 중인 사운드를 정지합니다.
        /// </summary>
        public void Stop()
        {
            if (_isStopped)
                return;

            _isStopped = true;
            _stopAction?.Invoke();
        }
    }
}
