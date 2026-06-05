using System;
using UnityEngine.Playables;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 효과 Timeline Clip이 Timeline 창에서 유효한 PlayableAsset으로 동작하기 위한 빈 Behaviour입니다.
    /// 실제 런타임 실행은 Bake된 RuntimeSequence가 담당합니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectTimelinePlayableBehaviour : PlayableBehaviour
    {
    }
}
