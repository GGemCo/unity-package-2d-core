using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대표 sound UID를 실제 AudioClip 리소스 행으로 해석하는 런타임 서비스입니다.
    /// Direct/Variant 및 실제 리소스 테이블(sound_bgm/sound_ambient/sound_sfx) 해석을 한곳에서 처리합니다.
    /// </summary>
    public sealed class SoundResolver
    {
        private readonly TableLoaderManager _tableLoaderManager;
        private readonly SoundVariantSelector _variantSelector = new SoundVariantSelector();

        public SoundResolver(TableLoaderManager tableLoaderManager)
        {
            _tableLoaderManager = tableLoaderManager;
        }

        /// <summary>
        /// 외부에서 사용하는 대표 sound UID를 실제 재생 가능한 리소스 정보로 해석합니다.
        /// </summary>
        /// <param name="soundUid">대표 sound UID입니다.</param>
        /// <param name="resolved">최종 재생 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다. 무음 후보도 성공 결과로 처리됩니다.</returns>
        public bool TryResolve(int soundUid, out ResolvedSound resolved)
        {
            resolved = default;
            if (_tableLoaderManager == null)
                return false;

            StruckTableSound sound = _tableLoaderManager.GetSoundData(soundUid, false);
            if (sound == null)
                return false;

            if (sound.ResolveMode == SoundConstants.ResolveMode.Variant)
                return TryResolveVariant(sound, out resolved);

            return TryResolveDirect(sound, out resolved);
        }

        /// <summary>
        /// 대표 sound UID에 직접 연결된 실제 리소스 1개를 해석합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="resolved">최종 재생 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다.</returns>
        private bool TryResolveDirect(StruckTableSound sound, out ResolvedSound resolved)
        {
            resolved = default;
            if (TryGetFirstResource(sound, out StruckTableSoundResource resource))
                return BuildResolved(sound, resource, null, out resolved);

            GcLogger.LogWarning($"[SoundResolver] 직접 연결된 실제 사운드 리소스가 없습니다. soundUid={sound.Uid}, type={sound.Type}");
            return false;
        }

        /// <summary>
        /// sound_variant 후보 목록을 기준으로 실제 리소스를 선택합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="resolved">최종 재생 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다.</returns>
        private bool TryResolveVariant(StruckTableSound sound, out ResolvedSound resolved)
        {
            resolved = default;
            IReadOnlyList<StruckTableSoundVariant> variants = _tableLoaderManager.TableSoundVariant.GetVariants(sound.Uid);
            if (!_variantSelector.TrySelect(sound, variants, out StruckTableSoundVariant selected))
            {
                if (sound.FallbackResourceUid > 0 && TryGetResourceByUid(sound, sound.FallbackResourceUid, out StruckTableSoundResource fallback))
                    return BuildResolved(sound, fallback, null, out resolved);

                // ResolveMode가 비어 있는 신규 sound 행도 연결된 실제 리소스가 1개이면 기존 방식처럼 직접 재생합니다.
                if (TryGetFirstResource(sound, out StruckTableSoundResource directResource))
                    return BuildResolved(sound, directResource, null, out resolved);

                GcLogger.LogWarning($"[SoundResolver] 선택 가능한 variant 후보가 없습니다. soundUid={sound.Uid}");
                return false;
            }

            if (selected.CandidateResourceUid <= 0)
            {
                resolved = ResolvedSound.Silent(sound.Uid, sound);
                return true;
            }

            if (!TryGetResourceByUid(sound, selected.CandidateResourceUid, out StruckTableSoundResource resource))
            {
                GcLogger.LogWarning($"[SoundResolver] variant 후보 리소스를 찾지 못했습니다. soundUid={sound.Uid}, resourceUid={selected.CandidateResourceUid}, type={sound.Type}");
                return false;
            }

            return BuildResolved(sound, resource, selected, out resolved);
        }

        /// <summary>
        /// 대표 sound 타입에 맞는 실제 리소스 테이블에서 첫 번째 연결 행을 찾습니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="resource">찾은 실제 리소스 행입니다.</param>
        /// <returns>찾으면 true를 반환합니다.</returns>
        private bool TryGetFirstResource(StruckTableSound sound, out StruckTableSoundResource resource)
        {
            resource = sound.Type switch
            {
                SoundConstants.Type.Bgm => (StruckTableSoundResource)_tableLoaderManager.TableSoundBgm.GetFirstBySoundUid(sound.Uid),
                SoundConstants.Type.Ambient => (StruckTableSoundResource)_tableLoaderManager.TableSoundAmbient.GetFirstBySoundUid(sound.Uid),
                SoundConstants.Type.Sfx => (StruckTableSoundResource)_tableLoaderManager.TableSoundSfx.GetFirstBySoundUid(sound.Uid),
                _ => null,
            };

            return resource != null;
        }

        /// <summary>
        /// 대표 sound 타입에 맞는 실제 리소스 테이블에서 UID로 행을 찾습니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="resourceUid">실제 리소스 UID입니다.</param>
        /// <param name="resource">찾은 실제 리소스 행입니다.</param>
        /// <returns>찾으면 true를 반환합니다.</returns>
        private bool TryGetResourceByUid(StruckTableSound sound, int resourceUid, out StruckTableSoundResource resource)
        {
            resource = sound.Type switch
            {
                SoundConstants.Type.Bgm => (StruckTableSoundResource)_tableLoaderManager.TableSoundBgm.GetDataByUid(resourceUid),
                SoundConstants.Type.Ambient => (StruckTableSoundResource)_tableLoaderManager.TableSoundAmbient.GetDataByUid(resourceUid),
                SoundConstants.Type.Sfx => (StruckTableSoundResource)_tableLoaderManager.TableSoundSfx.GetDataByUid(resourceUid),
                _ => null,
            };

            return resource != null;
        }

        /// <summary>
        /// 실제 리소스 행과 variant 보정값을 결합해 최종 재생 정보를 생성합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="resource">실제 리소스 행입니다.</param>
        /// <param name="variant">선택된 variant 행입니다. Direct 재생이면 null입니다.</param>
        /// <param name="resolved">최종 재생 정보입니다.</param>
        /// <returns>생성에 성공하면 true를 반환합니다.</returns>
        private static bool BuildResolved(StruckTableSound sound, StruckTableSoundResource resource, StruckTableSoundVariant variant, out ResolvedSound resolved)
        {
            resolved = default;
            if (sound == null || resource == null || string.IsNullOrWhiteSpace(resource.FileName))
                return false;

            float volume = Mathf.Clamp01(sound.VolumeScale <= 0f ? 1f : sound.VolumeScale)
                           * Mathf.Clamp01(resource.Volume <= 0f ? 1f : resource.Volume)
                           * Mathf.Clamp01(variant != null && variant.VolumeScale > 0f ? variant.VolumeScale : 1f);
            float pitchMin = variant != null && variant.PitchMinOverride > 0f ? variant.PitchMinOverride : resource.GetSafePitchMin();
            float pitchMax = variant != null && variant.PitchMaxOverride > 0f ? variant.PitchMaxOverride : resource.GetSafePitchMax();
            if (pitchMax < pitchMin)
                (pitchMin, pitchMax) = (pitchMax, pitchMin);

            float pitch = Mathf.Approximately(pitchMin, pitchMax) ? pitchMin : Random.Range(pitchMin, pitchMax);
            resolved = new ResolvedSound(sound.Uid, resource.Uid, resource.Type, resource.FileName, volume, pitch,
                resource.Loop, resource.FadeDuration, resource.HasFadeDurationOverride(), sound, resource);
            return true;
        }
    }
}
