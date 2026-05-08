using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 조작 잠금 이벤트가 잠금을 적용할 대상을 정의합니다.
    /// </summary>
    public enum CharacterControlLockTargetScope
    {
        /// <summary>
        /// <see cref="CharacterControlLockData.target"/>에 지정된 단일 캐릭터만 잠급니다.
        /// </summary>
        Target = 0,

        /// <summary>
        /// 현재 플레이어 캐릭터만 잠급니다.
        /// </summary>
        Player = 1,

        /// <summary>
        /// 현재 맵에 배치된 모든 몬스터를 잠급니다.
        /// </summary>
        CurrentMapMonsters = 2,

        /// <summary>
        /// 현재 플레이어와 현재 맵에 배치된 모든 몬스터를 잠급니다.
        /// </summary>
        PlayerAndCurrentMapMonsters = 3,

        /// <summary>
        /// 현재 씬에서 찾을 수 있는 모든 활성 캐릭터를 잠급니다.
        /// </summary>
        SceneCharacters = 4
    }

    /// <summary>
    /// 캐릭터 조작 잠금 이벤트가 함께 제어할 게임플레이 기능을 정의합니다.
    /// </summary>
    [Flags]
    public enum CharacterControlLockMask
    {
        /// <summary>
        /// 아무 기능도 잠그지 않습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 캐릭터의 조작 가능 상태를 잠급니다.
        /// </summary>
        CharacterControl = 1 << 0,

        /// <summary>
        /// 플레이어 자동 이동을 일시정지합니다.
        /// </summary>
        AutoMove = 1 << 1,

        /// <summary>
        /// 몬스터 Brain 또는 BT 의사결정 틱을 일시정지합니다.
        /// </summary>
        MonsterBrain = 1 << 2,

        /// <summary>
        /// 일반적인 게임 일시정지에 필요한 모든 기능을 잠급니다.
        /// </summary>
        All = CharacterControl | AutoMove | MonsterBrain
    }

    /// <summary>
    /// 컷씬 타임라인에서 캐릭터 조작, 자동 이동, 몬스터 Brain을 토큰 기반으로 잠그기 위한 이벤트 데이터입니다.
    /// </summary>
    [Serializable]
    public class CharacterControlLockData
    {
        /// <summary>
        /// 잠금을 적용할 대상 범위입니다.
        /// </summary>
        [Header("Target")]
        [Tooltip("잠금을 적용할 대상 범위입니다. Target은 아래 Target Reference를 사용하고, 나머지는 런타임의 현재 캐릭터 목록을 사용합니다.")]
        public CharacterControlLockTargetScope targetScope = CharacterControlLockTargetScope.Target;

        /// <summary>
        /// <see cref="CharacterControlLockTargetScope.Target"/> 범위일 때 사용할 단일 캐릭터 참조입니다.
        /// </summary>
        [Tooltip("Target 범위일 때 잠금을 적용할 캐릭터입니다. Fixed는 타입/uid를 직접 사용하고, RuntimeOverride는 컷씬 실행 시 주입된 대상을 사용합니다.")]
        public CutsceneCharacterReference target = new CutsceneCharacterReference
        {
            characterType = CharacterConstants.Type.Player
        };

        /// <summary>
        /// 잠금 이벤트가 함께 제어할 게임플레이 기능입니다.
        /// </summary>
        [Header("Lock")]
        [Tooltip("함께 잠글 기능입니다. 일반적인 연출 일시정지는 All을 사용합니다.")]
        public CharacterControlLockMask lockMask = CharacterControlLockMask.All;

        /// <summary>
        /// 잠금이 시작될 때 대상 캐릭터를 즉시 대기 상태로 돌릴지 여부입니다.
        /// </summary>
        [Tooltip("잠금 시작 시 대상 캐릭터의 이동 벡터를 0으로 만들고 대기 애니메이션을 재생합니다.")]
        public bool stopImmediately = true;

        /// <summary>
        /// 이벤트 클립의 지속 시간이 끝났을 때 잠금을 해제할지 여부입니다.
        /// </summary>
        [Header("Release")]
        [Tooltip("이벤트 duration이 끝났을 때 잠금을 해제합니다. duration이 0 이하면 컷씬 종료 또는 별도 해제 시점까지 유지됩니다.")]
        public bool releaseOnClipEnd = true;

        /// <summary>
        /// 컷씬 종료 시 이벤트가 보유한 잠금을 해제할지 여부입니다.
        /// </summary>
        [Tooltip("컷씬 종료 시 이 이벤트가 획득한 잠금을 해제합니다. 별도 해제 흐름이 없다면 켜두는 것을 권장합니다.")]
        public bool releaseOnCutsceneEnd = true;
    }
}
