using System.Collections;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 이벤트를 준비, 실행, 갱신 및 종료하는 컨트롤러의 공통 계약을 정의합니다.
    /// 각 구현체는 특정 연출 타입의 생명주기를 관리합니다.
    /// </summary>
    public interface ICutsceneController
    {
        /// <summary>
        /// 컷신 이벤트 실행 전에 필요한 리소스 준비와 초기 설정을 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 데이터입니다.</param>
        /// <returns>준비 작업이 완료될 때까지 진행되는 코루틴입니다.</returns>
        IEnumerator Ready(CutsceneEvent evt);

        /// <summary>
        /// 지정한 컷신 이벤트를 실제로 시작하거나 즉시 반영합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 데이터입니다.</param>
        void Trigger(CutsceneEvent evt);

        /// <summary>
        /// 재생 중인 연출 상태를 프레임 단위로 갱신합니다.
        /// </summary>
        void Update();

        /// <summary>
        /// 현재 진행 중인 연출을 중단합니다.
        /// 필요 시 중간 상태를 정리하되, 최종 종료와는 구분될 수 있습니다.
        /// </summary>
        void Stop();

        /// <summary>
        /// 컨트롤러가 사용한 상태와 리소스를 정리하고 연출을 종료합니다.
        /// </summary>
        void End();
    }
}