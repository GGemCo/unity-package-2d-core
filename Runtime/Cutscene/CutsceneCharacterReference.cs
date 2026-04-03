using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 이벤트에서 사용할 캐릭터 참조 정보입니다.
    /// 고정 대상 또는 런타임 오버라이드 키를 통해 실제 캐릭터를 해석합니다.
    /// </summary>
    [Serializable]
    public class CutsceneCharacterReference
    {
        [Tooltip("캐릭터 대상 소스 모드입니다. Fixed는 characterType/characterUid를, RuntimeOverride는 runtimeTargetKey를 사용합니다.")]
        public CutsceneCharacterTargetSourceMode sourceMode = CutsceneCharacterTargetSourceMode.Fixed;

        [Tooltip("sourceMode가 Fixed일 때 사용할 캐릭터 타입입니다.")]
        public CharacterConstants.Type characterType;

        [Tooltip("sourceMode가 Fixed일 때 사용할 캐릭터 uid입니다.")]
        public int characterUid;

        [Tooltip("sourceMode가 RuntimeOverride일 때 CutsceneManager에서 조회할 런타임 캐릭터 키입니다.")]
        public CutsceneKeyCharacterTarget runtimeTargetKey = CutsceneKeyCharacterTarget.None;
    }
}
