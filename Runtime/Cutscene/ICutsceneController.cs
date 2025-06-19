using System.Collections;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 클립별 컨트롤러 인터페이스
    /// </summary>
    public interface ICutsceneController
    {
        /// <summary>
        /// 클립 연출 준비
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        IEnumerator Ready(CutsceneEvent evt);
        /// <summary>
        /// 클립 연출 시작
        /// </summary>
        /// <param name="evt"></param>
        void Trigger(CutsceneEvent evt);
        /// <summary>
        /// 클립 연출중 일때 처리
        /// </summary>
        void Update();
        /// <summary>
        /// 클립 연출이 종료되었을 때 처리
        /// </summary>
        void Stop();
        /// <summary>
        /// 모든 연출 종료되었을 때 처리 
        /// </summary>
        void End();
    }
}