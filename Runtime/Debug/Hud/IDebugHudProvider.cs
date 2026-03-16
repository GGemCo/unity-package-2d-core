using System.Text;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디버그 HUD 수집기 인터페이스입니다.
    /// 각 구현체는 자체 갱신 주기에 맞춰 데이터를 샘플링하고, 화면 출력용 문자열을 구성합니다.
    /// </summary>
    public interface IDebugHudProvider
    {
        /// <summary>현재 설정에서 이 Provider가 활성화되어야 하는지 여부입니다.</summary>
        bool IsEnabled(GGemCoSettings settings);

        /// <summary>현재 설정 기준 갱신 주기(초)입니다.</summary>
        float GetUpdateInterval(GGemCoSettings settings);

        /// <summary>Provider 내부 상태를 초기화합니다.</summary>
        void Reset();

        /// <summary>샘플링 누적 시간만큼 Provider 상태를 갱신합니다.</summary>
        void Tick(float elapsedSeconds);

        /// <summary>현재 Provider 내용을 문자열로 구성합니다.</summary>
        bool TryBuildContent(StringBuilder builder);
    }
}
