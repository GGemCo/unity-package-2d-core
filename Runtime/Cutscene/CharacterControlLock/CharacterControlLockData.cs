using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에서 캐릭터 조작 잠금을 제어하기 위한 이벤트 데이터입니다.
    /// </summary>
    [Serializable]
    public class CharacterControlLockData
    {
        /// <summary>
        /// 조작 잠금을 적용할 캐릭터 대상입니다.
        /// 기본값은 현재 플레이어 캐릭터입니다.
        /// </summary>
        [Header("Target")]
        [Tooltip("조작 잠금을 적용할 캐릭터 대상입니다. Fixed는 직접 타입/uid를, RuntimeOverride는 런타임 키를 사용합니다.")]
        public CutsceneCharacterReference target = new CutsceneCharacterReference
        {
            characterType = CharacterConstants.Type.Player
        };

        /// <summary>
        /// 클립 지속 시간이 끝났을 때 조작 잠금을 해제할지 여부입니다.
        /// 꺼져 있으면 컷신 종료 또는 외부 해제 시점까지 잠금이 유지됩니다.
        /// </summary>
        [Header("Release")]
        [Tooltip("클립 지속 시간이 끝났을 때 조작 잠금을 해제할지 여부입니다. duration이 0 이하이면 컷신 종료까지 유지됩니다.")]
        public bool releaseOnClipEnd = true;

        /// <summary>
        /// 컷신 종료 시 이 이벤트가 획득한 조작 잠금을 해제할지 여부입니다.
        /// 특별한 외부 해제 흐름이 없다면 켜두는 것을 권장합니다.
        /// </summary>
        [Tooltip("컷신 종료 시 이 이벤트가 획득한 조작 잠금을 해제할지 여부입니다.")]
        public bool releaseOnCutsceneEnd = true;
    }
}
