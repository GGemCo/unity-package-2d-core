using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트 컨트롤러: 인덱스/생명주기/그룹 규칙만 관리
    /// CharacterBase.cs에서 생성/초기화
    /// </summary>
    public class AffectController
    {
        private CharacterBase _character;
        private TableAffect _tableAffect;
        private EffectManager _effectManager;

        // uid -> Affect
        private readonly Dictionary<int, AffectBase> _actives = new();

        // group -> uid (동일 Group은 1개만 허용)
        private readonly Dictionary<string, int> _groupIndex = new(StringComparer.Ordinal);

        private static bool IsNoneGroup(string group)
        {
            return string.IsNullOrWhiteSpace(group) ||
                   group.Equals("None", StringComparison.OrdinalIgnoreCase);
        }

        public void Initialize(CharacterBase characterBase)
        {
            _character = characterBase;
            if (TableLoaderManager.Instance == null) return;
            _tableAffect   = TableLoaderManager.Instance.TableAffect;
            _effectManager = SceneGame.Instance.EffectManager;
        }

        /// <summary>
        /// 어펙트 적용(필요 시 그룹 선점 해제/재적용 갱신)
        /// </summary>
        public void ApplyAffect(int affectUid, float durationOverride = 0f)
        {
            var info = _tableAffect?.GetDataByUid(affectUid); // StruckTableAffect 반환 가정
            if (info == null)
            {
                GcLogger.LogError("affect 테이블에 없는 어펙트입니다. affect Uid: " + affectUid);
                return;
            }

            float duration = durationOverride > 0f ? durationOverride : Mathf.Max(0f, info.Duration);
            string group   = info.Group;

            // 1) 그룹 단일성 보장
            if (!IsNoneGroup(group) && _groupIndex.TryGetValue(group, out var existingUidInGroup))
            {
                RemoveAffect(existingUidInGroup);
            }

            // 2) 동일 UID 존재 시 -> Refresh
            if (_actives.TryGetValue(info.Uid, out var existing))
            {
                existing.Refresh(duration);
                return;
            }

            // 3) 신규 인스턴스 구성
            var buffs = new List<ConfigCommon.StruckStatus>
            {
                new(info.StatusID, info.StatusSuffix, info.Value)
            };

            var affect = CreateAffectInstance(info, duration, buffs);

            // 4) 적용 및 인덱싱
            affect.Apply(info);
            _actives[info.Uid] = affect;
            if (!IsNoneGroup(group))
                _groupIndex[group] = info.Uid;
        }

        /// <summary>
        /// 단일 어펙트 제거(수동)
        /// </summary>
        public void RemoveAffect(int affectUid)
        {
            if (!_actives.TryGetValue(affectUid, out var affect))
                return;

            // 인덱스 해제
            if (!IsNoneGroup(affect.Group)
                && _groupIndex.TryGetValue(affect.Group, out var mapped)
                && mapped == affectUid)
            {
                _groupIndex.Remove(affect.Group);
            }

            // 정리
            affect.Stop();
            _actives.Remove(affectUid);
        }

        /// <summary>전체 제거(캐릭터 사망·씬 전환 등)</summary>
        public void RemoveAllAffects()
        {
            foreach (var kv in _actives)
                kv.Value.Stop();

            _actives.Clear();
            _groupIndex.Clear();
        }

        /// <summary>만료 콜백(코루틴 완료 시 호출)</summary>
        private void OnAffectCompleted(int affectUid)
        {
            // Affect 내부에서 스탯/이펙트는 아직 해제되지 않았으므로 Stop을 통해 정리
            RemoveAffect(affectUid);
        }

        // 선택적 유틸
        public bool HasAffect(int affectUid) => _actives.ContainsKey(affectUid);
        public bool HasGroup(string group) =>
            !IsNoneGroup(group) && _groupIndex.ContainsKey(group);

        // -------------------------
        // Factory
        // -------------------------
        private AffectBase CreateAffectInstance(
            StruckTableAffect info,
            float duration,
            List<ConfigCommon.StruckStatus> buffs)
        {
            bool isKnockBack = !string.IsNullOrEmpty(info.StatusID)
                               && info.StatusID.Equals(ConfigCommon.StatusKnockBack, StringComparison.OrdinalIgnoreCase);

            if (isKnockBack)
            {
                return new AffectKnockBack(
                    character:     _character,
                    effectManager: _effectManager,
                    uid:           info.Uid,
                    group:         info.Group,
                    buffs:         buffs,
                    duration:      duration,
                    onCompleted:   OnAffectCompleted
                );
            }

            // 기본 Affect
            return new AffectBase(
                _character,
                _effectManager,
                info.Uid,
                info.Group,
                buffs,
                duration,
                OnAffectCompleted
            );
        }
    }
}
