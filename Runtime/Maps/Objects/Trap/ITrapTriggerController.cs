using UnityEngine;

namespace GGemCo2DCore
{ 
    /// <summary>
    /// (선택) 트랩이 외부 제어를 지원하려면 구현할 수 있는 인터페이스.
    /// - IsActive: 현재 동작 중 여부
    /// - RequestStart/RequestEnd: 외부에서 시작/종료 요청
    /// </summary>
    public interface ITrapTriggerController
    {
        bool IsActive { get; }
        void RequestStart(Collider2D triggerSource);
        void RequestEnd();
    }
}