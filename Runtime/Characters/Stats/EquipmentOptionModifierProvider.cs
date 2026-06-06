using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 장비/옵션 기반 Modifier Provider입니다.
    /// - 장비 갱신(<see cref="UpdateFromEquippedItems"/>) 시 장비 옵션을 다시 계산하여 버킷(Flat/Percent)에 반영합니다.
    /// - 장비로 부여되는 착용 지속 Affect의 apply/remove 동기화도 함께 처리합니다.
    /// </summary>
    public sealed class EquipmentOptionModifierProvider : IStatModifierProvider
    {
        /// <summary>
        /// Affect/State 적용 대상 오브젝트입니다.
        /// </summary>
        private readonly GameObject _owner;

        /// <summary>
        /// 장비 옵션으로부터 누적되는 스탯 변경 버킷(Flat/Percent)입니다.
        /// </summary>
        private readonly StatModifierBucket _bucket = new();

        /// <summary>
        /// 장비에 의해 현재 적용 중인 착용 지속 Affect UID 집합입니다(동기화용).
        /// </summary>
        private readonly HashSet<int> _equipAppliedAffects = new();

        /// <summary>
        /// 스탯 키별 Flat(고정) 누적값입니다.
        /// </summary>
        public IReadOnlyDictionary<string, int> Flat => _bucket.Flat;

        /// <summary>
        /// 스탯 키별 Percent(비율) 누적값입니다.
        /// </summary>
        public IReadOnlyDictionary<string, float> Percent => _bucket.Percent;

        /// <summary>
        /// 버킷(Flat/Percent) 또는 장비 Affect 동기화 결과가 변경되었을 때 발생합니다.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 장비 옵션 Provider를 생성합니다.
        /// </summary>
        /// <param name="owner">Affect/State 적용 대상 오브젝트입니다.</param>
        public EquipmentOptionModifierProvider(GameObject owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 장비 옵션으로 누적된 모든 modifier를 제거하고 변경 이벤트를 발생시킵니다.
        /// </summary>
        public void Clear()
        {
            _bucket.Clear();
            Changed?.Invoke();
        }

        /// <summary>
        /// 장착 아이템(정의/인스턴스)을 기준으로 최종 옵션을 계산하여 Stat/Affect를 반영합니다.
        /// </summary>
        /// <param name="characterBase">캐릭터 기본 정의 정보입니다(현재 구현에서는 직접 사용되지 않을 수 있습니다).</param>
        /// <param name="equippedItems">장착 슬롯별 아이템 참조 목록입니다.</param>
        /// <remarks>
        /// - 옵션 계산은 아이템 인스턴스(roll 결과 포함)를 기반으로 수행합니다.
        /// - 계산된 Stat 옵션은 버킷에 누적되며, Affect 옵션은 착용 지속 효과로 동기화됩니다.
        /// - 최종적으로 <see cref="Changed"/> 이벤트를 발생시켜 상위에서 재계산을 트리거합니다.
        /// </remarks>
        public void UpdateFromEquippedItems(CharacterBase characterBase, Dictionary<int, EquippedItemRef> equippedItems)
        {
            _bucket.Clear();

            var statModifiers = new List<ConfigCommon.StruckStatus>(32);
            var desiredEquipAffects = new HashSet<int>();

            // 옵션 리졸버(테이블 기반)
            var tables = TableLoaderManager.Instance;
            var resolver = new ItemOptionResolver(tables);

            var instanceStore = SceneGame.Instance?.saveDataManager?.ItemInstances;

            foreach (var kv in equippedItems)
            {
                var equipRef = kv.Value;
                if (equipRef == null || equipRef.ItemUid <= 0) continue;

                // 인스턴스 기반이면 Base + Rolled 옵션을 사용(인스턴스가 없으면 처리하지 않음)
                if (equipRef.InstanceId <= 0 || instanceStore == null ||
                    !instanceStore.TryGet(equipRef.InstanceId, out var inst) || inst == null) continue;

                var options = resolver.ResolveFinalOptions(inst);
                ApplyOptionsFromEntries(options, statModifiers, desiredEquipAffects);
            }

            ApplyStatModifiers(statModifiers);
            SyncEquipAffects(desiredEquipAffects);

            Changed?.Invoke();
        }

        /// <summary>
        /// 옵션 엔트리 목록을 해석하여 스탯 변경과 착용 지속 Affect 목록을 수집/적용합니다.
        /// </summary>
        /// <param name="options">해석할 옵션 엔트리 목록입니다.</param>
        /// <param name="outStatModifiers">수집된 스탯 변경 목록을 추가할 대상 리스트입니다.</param>
        /// <param name="outEquipAffects">수집된 착용 지속 Affect UID를 추가할 대상 집합입니다.</param>
        private void ApplyOptionsFromEntries(List<ItemOptionEntry> options, List<ConfigCommon.StruckStatus> outStatModifiers, HashSet<int> outEquipAffects)
        {
            if (options == null || options.Count <= 0) return;

            for (int i = 0; i < options.Count; i++)
            {
                var op = options[i];
                if (!op.IsValid) continue;

                switch (op.Kind)
                {
                    case ItemOptionKind.Stat:
                        // CharacterStat은 BASE_*/STAT_* 스탯 키를 그대로 받아 계산 모듈별 버킷에 반영합니다.
                        outStatModifiers.Add(new ConfigCommon.StruckStatus(op.TargetId, op.Op, op.Value));
                        break;

                    case ItemOptionKind.Affect:
                    {
                        if (TryParseIntId(op.TargetId, out var affectUid) && affectUid > 0)
                        {
                            // 착용 지속 효과는 장비 변경 시점에 apply/remove를 동기화한다.
                            outEquipAffects.Add(affectUid);
                        }
                        break;
                    }

                    case ItemOptionKind.State:
                        // State는 Affect 패키지 쪽 정책(STATE -> Affect 매핑)에 따라 처리될 수 있다.
                        // Core에서는 브리지만 제공하고, 실제 매핑은 Affect 런타임에서 처리하도록 한다.
                        AffectRuntimeBridge.ApplyState(_owner, op.TargetId, op.Duration);
                        break;

                    case ItemOptionKind.DamageType:
                        // DamageType 기반 옵션(예: 속성 추가/전환)은 전투 파이프라인에 맞춰 확장 필요.
                        // Core 기본안에서는 저항/증가를 Stat으로 처리하는 것을 권장한다.
                        break;
                }
            }
        }

        /// <summary>
        /// 착용 지속 Affect를 목표 상태(desired)와 현재 상태(_equipAppliedAffects) 간 차이를 기반으로 동기화합니다.
        /// </summary>
        /// <param name="desired">이번 장비 구성에서 유지되어야 하는 Affect UID 집합입니다.</param>
        private void SyncEquipAffects(HashSet<int> desired)
        {
            // remove
            var toRemove = _equipAppliedAffects.Where(x => !desired.Contains(x)).ToArray();
            for (int i = 0; i < toRemove.Length; i++)
            {
                AffectRuntimeBridge.RemoveAffect(_owner, toRemove[i]);
                _equipAppliedAffects.Remove(toRemove[i]);
            }

            // apply
            foreach (var uid in desired)
            {
                if (_equipAppliedAffects.Contains(uid)) continue;
                AffectRuntimeBridge.ApplyAffect(_owner, uid, 0);
                _equipAppliedAffects.Add(uid);
            }
        }

        /// <summary>
        /// 문자열 ID를 int로 파싱합니다.
        /// </summary>
        /// <param name="v">파싱할 문자열 값입니다.</param>
        /// <param name="id">파싱된 정수 ID입니다.</param>
        /// <returns>파싱에 성공하면 true, 실패하면 false입니다.</returns>
        private static bool TryParseIntId(string v, out int id)
        {
            id = 0;
            if (string.IsNullOrEmpty(v)) return false;
            return int.TryParse(v, out id);
        }

        /// <summary>
        /// 스탯 변경 목록을 버킷에 누적 적용합니다.
        /// </summary>
        /// <param name="modifiers">적용할 스탯 변경 목록입니다.</param>
        /// <remarks>
        /// 이 메서드는 <see cref="Changed"/> 이벤트를 발생시키지 않습니다.
        /// (호출 흐름에 따라 일괄 처리 후 한 번만 이벤트를 발생시키기 위함)
        /// </remarks>
        public void ApplyStatModifiers(List<ConfigCommon.StruckStatus> modifiers)
        {
            if (modifiers == null || modifiers.Count <= 0) return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                ModifyStat(m.ID, m, true);
            }
        }

        /// <summary>
        /// 스탯 변경 목록을 버킷에서 제거(역적용)합니다.
        /// </summary>
        /// <param name="modifiers">제거할 스탯 변경 목록입니다.</param>
        /// <remarks>
        /// 이 메서드는 <see cref="Changed"/> 이벤트를 발생시키지 않습니다.
        /// </remarks>
        public void RemoveStatModifiers(List<ConfigCommon.StruckStatus> modifiers)
        {
            if (modifiers == null || modifiers.Count <= 0) return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                ModifyStat(m.ID, m, false);
            }
        }

        /// <summary>
        /// 접미사(SuffixType)에 따라 Flat 또는 Percent 버킷에 값을 추가/제거합니다.
        /// </summary>
        /// <param name="statType">스탯 키(예: BASE_ATK, STAT_ATK 등)입니다.</param>
        /// <param name="struckStatus">옵션으로부터 전달된 스탯 변경 정보입니다.</param>
        /// <param name="isAdding">true이면 적용, false이면 제거(역적용)합니다.</param>
        private void ModifyStat(string statType, ConfigCommon.StruckStatus struckStatus, bool isAdding)
        {
            if (string.IsNullOrEmpty(statType)) return;

            string baseStat = statType;

            float value = struckStatus.Value;
            ConfigCommon.SuffixType suffixType = struckStatus.SuffixType;

            switch (suffixType)
            {
                case ConfigCommon.SuffixType.Plus:
                    _bucket.AddFlat(baseStat, isAdding ? (int)value : -(int)value);
                    break;

                case ConfigCommon.SuffixType.Minus:
                    _bucket.AddFlat(baseStat, isAdding ? -(int)value : (int)value);
                    break;

                case ConfigCommon.SuffixType.Increase:
                    _bucket.AddPercent(baseStat, isAdding ? value : -value);
                    break;

                case ConfigCommon.SuffixType.Decrease:
                    _bucket.AddPercent(baseStat, isAdding ? -value : value);
                    break;

                case ConfigCommon.SuffixType.None:
                default:
                    // legacy/간편 표기: None이면 Plus로 간주(기존 OptionType* 호환)
                    _bucket.AddFlat(baseStat, isAdding ? (int)value : -(int)value);
                    break;
            }
        }
    }
}