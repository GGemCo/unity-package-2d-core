using System.Collections;

namespace GGemCo2DCore
{
    /// <summary>
    /// 패키지별로 로더 스텝을 추가하려면 이 인터페이스를 구현하고 GameLoaderManager.Register로 등록하세요.
    /// </summary>
    public interface IGameLoadStep
    {
        /// <summary>고유 식별자(e.g., "control.input")</summary>
        string Id { get; }

        /// <summary>실행 순서. 낮을수록 먼저 실행됩니다.</summary>
        int Order { get; }

        /// <summary>진행률 UI의 부제목으로 사용할 Localization 키 (StringTable Key)</summary>
        string LocalizedKey { get; }

        /// <summary>코루틴 본체. 내부에서 Addressables/IO 등을 처리</summary>
        IEnumerator Run();

        /// <summary>0~1 진행률</summary>
        float GetProgress();
    }
}