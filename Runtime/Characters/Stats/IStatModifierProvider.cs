using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스탯 Modifier 디버그에서 표시할 출처 구분입니다.
    /// </summary>
    public enum StatModifierDebugSourceType
    {
        /// <summary>출처를 특정할 수 없습니다.</summary>
        Unknown = 0,
        /// <summary>장비/아이템으로 인한 증가입니다.</summary>
        Item = 1,
        /// <summary>패시브 스킬로 인한 증가입니다.</summary>
        Skill = 2,
        /// <summary>Affect/버프/디버프로 인한 증가입니다.</summary>
        Affect = 3,
        /// <summary>스탯 포인트나 저장 데이터 기반 영구 증가입니다.</summary>
        Persistent = 4,
        /// <summary>런타임 임시 효과로 인한 증가입니다.</summary>
        Runtime = 5,
    }

    /// <summary>
    /// 스탯 Modifier Provider가 디버그용 출처 정보를 제공하기 위한 선택 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 기존 <see cref="IStatModifierProvider"/> 계약을 변경하지 않고,
    /// 디버그 수집기에서 구현 여부를 확인해 Item/Skill/Affect 증가량을 분리합니다.
    /// </remarks>
    public interface IStatModifierDebugSource
    {
        /// <summary>디버그 HUD에 표시할 출처 타입입니다.</summary>
        StatModifierDebugSourceType DebugSourceType { get; }

        /// <summary>디버그 HUD에 표시할 출처 이름입니다.</summary>
        string DebugSourceName { get; }
    }

    /// <summary>
    /// 스탯 Modifier 제공자(출처) 인터페이스입니다.
    /// - 장비/옵션, 영구(스탯 포인트), 패시브 스킬 등 "출처" 단위로 modifier를 분리하기 위한 계약입니다.
    /// - <see cref="Changed"/> 이벤트가 발생하면, <see cref="CharacterStat"/>는 최종 스탯을 재계산합니다.
    /// </summary>
    public interface IStatModifierProvider
    {
        /// <summary>
        /// Flat(가산) Modifier 사전입니다.
        /// </summary>
        /// <remarks>
        /// Key: 스탯 키(예: BASE_ATK, STAT_ATK 등), Value: 누적 가산 값입니다.
        /// </remarks>
        IReadOnlyDictionary<string, int> Flat { get; }

        /// <summary>
        /// Percent(%) Modifier 사전입니다.
        /// </summary>
        /// <remarks>
        /// Key: 스탯 키(예: BASE_ATK, STAT_ATK 등), Value: 누적 비율 값(예: 10 = +10%)입니다.
        /// </remarks>
        IReadOnlyDictionary<string, float> Percent { get; }

        /// <summary>
        /// Modifier 내용이 변경되었을 때 발생하는 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 구현체는 내부 버킷 갱신 후 이 이벤트를 발생시키며,
        /// 구독자(<see cref="CharacterStat"/>)는 이를 트리거로 최종 스탯을 재계산합니다.
        /// </remarks>
        event Action Changed;
    }
}
