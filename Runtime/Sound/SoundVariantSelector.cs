using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// sound_variant 후보 목록에서 실제 재생할 리소스 후보를 선택합니다.
    /// 선택 순서, 가중치, 최근 반복 방지 상태를 런타임 동안 유지합니다.
    /// </summary>
    public sealed class SoundVariantSelector
    {
        private readonly Dictionary<int, int> _sequenceIndexBySoundUid = new Dictionary<int, int>();
        private readonly Dictionary<int, Queue<int>> _recentResourceUidsBySoundUid = new Dictionary<int, Queue<int>>();

        /// <summary>
        /// 대표 sound 행과 후보 목록을 기준으로 실제 후보를 선택합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="variants">후보 목록입니다.</param>
        /// <param name="selected">선택된 후보입니다.</param>
        /// <returns>선택에 성공하면 true를 반환합니다.</returns>
        public bool TrySelect(StruckTableSound sound, IReadOnlyList<StruckTableSoundVariant> variants, out StruckTableSoundVariant selected)
        {
            selected = null;
            if (sound == null || variants == null || variants.Count == 0)
                return false;

            List<StruckTableSoundVariant> enabledVariants = CollectEnabledVariants(variants);
            if (enabledVariants.Count == 0)
                return false;

            List<StruckTableSoundVariant> selectableVariants = sound.SelectionMode == SoundConstants.SelectionMode.Sequence
                ? enabledVariants
                : FilterRecent(sound, enabledVariants);

            selected = sound.SelectionMode switch
            {
                SoundConstants.SelectionMode.RandomEqual => SelectRandomEqual(sound, selectableVariants),
                SoundConstants.SelectionMode.Sequence => SelectSequence(sound, selectableVariants),
                SoundConstants.SelectionMode.ShuffleBag => SelectRandomEqual(sound, selectableVariants),
                _ => SelectWeighted(sound, selectableVariants),
            };

            RememberRecent(sound, selected);
            return selected != null;
        }

        /// <summary>
        /// 사용 가능한 후보만 수집합니다.
        /// </summary>
        /// <param name="variants">원본 후보 목록입니다.</param>
        /// <returns>활성 후보 목록입니다.</returns>
        private static List<StruckTableSoundVariant> CollectEnabledVariants(IReadOnlyList<StruckTableSoundVariant> variants)
        {
            List<StruckTableSoundVariant> result = new List<StruckTableSoundVariant>();
            for (int i = 0; i < variants.Count; i++)
            {
                StruckTableSoundVariant variant = variants[i];
                if (variant == null || !variant.Enabled)
                    continue;

                result.Add(variant);
            }

            return result;
        }

        /// <summary>
        /// 후보를 동일 확률로 선택합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="variants">후보 목록입니다.</param>
        /// <returns>선택된 후보입니다.</returns>
        private static StruckTableSoundVariant SelectRandomEqual(StruckTableSound sound, List<StruckTableSoundVariant> variants)
        {
            if (variants == null || variants.Count == 0)
                return null;

            return variants[Random.Range(0, variants.Count)];
        }

        /// <summary>
        /// 후보 Weight 값을 기준으로 하나를 선택합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="variants">후보 목록입니다.</param>
        /// <returns>선택된 후보입니다.</returns>
        private static StruckTableSoundVariant SelectWeighted(StruckTableSound sound, List<StruckTableSoundVariant> variants)
        {
            int totalWeight = 0;
            for (int i = 0; i < variants.Count; i++)
                totalWeight += Mathf.Max(0, variants[i].Weight);

            if (totalWeight <= 0)
                return SelectRandomEqual(sound, variants);

            int roll = Random.Range(0, totalWeight);
            int cursor = 0;
            for (int i = 0; i < variants.Count; i++)
            {
                cursor += Mathf.Max(0, variants[i].Weight);
                if (roll < cursor)
                    return variants[i];
            }

            return variants[variants.Count - 1];
        }

        /// <summary>
        /// 후보를 등록 순서대로 선택합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="variants">후보 목록입니다.</param>
        /// <returns>선택된 후보입니다.</returns>
        private StruckTableSoundVariant SelectSequence(StruckTableSound sound, List<StruckTableSoundVariant> variants)
        {
            if (!_sequenceIndexBySoundUid.TryGetValue(sound.Uid, out int index))
                index = 0;

            index = Mathf.Abs(index) % variants.Count;
            StruckTableSoundVariant selected = variants[index];
            _sequenceIndexBySoundUid[sound.Uid] = (index + 1) % variants.Count;
            return selected;
        }

        /// <summary>
        /// 최근 선택한 후보를 가능한 한 제외합니다.
        /// 후보가 모두 제외되면 원본 후보 목록을 그대로 사용합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="variants">후보 목록입니다.</param>
        /// <returns>최근 후보가 제외된 목록입니다.</returns>
        private List<StruckTableSoundVariant> FilterRecent(StruckTableSound sound, List<StruckTableSoundVariant> variants)
        {
            if (sound.NoRepeatRecentCount <= 0 || !_recentResourceUidsBySoundUid.TryGetValue(sound.Uid, out Queue<int> recentQueue))
                return variants;

            HashSet<int> recent = new HashSet<int>(recentQueue);
            List<StruckTableSoundVariant> filtered = new List<StruckTableSoundVariant>();
            for (int i = 0; i < variants.Count; i++)
            {
                if (!recent.Contains(variants[i].CandidateResourceUid))
                    filtered.Add(variants[i]);
            }

            return filtered.Count > 0 ? filtered : variants;
        }

        /// <summary>
        /// 반복 방지 정책을 위해 최근 선택한 후보 UID를 기록합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="selected">선택된 후보입니다.</param>
        private void RememberRecent(StruckTableSound sound, StruckTableSoundVariant selected)
        {
            if (sound == null || selected == null || sound.NoRepeatRecentCount <= 0)
                return;

            if (!_recentResourceUidsBySoundUid.TryGetValue(sound.Uid, out Queue<int> queue))
            {
                queue = new Queue<int>();
                _recentResourceUidsBySoundUid[sound.Uid] = queue;
            }

            queue.Enqueue(selected.CandidateResourceUid);
            while (queue.Count > sound.NoRepeatRecentCount)
                queue.Dequeue();
        }
    }
}
