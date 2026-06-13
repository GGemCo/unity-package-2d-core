using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대표 sound UID가 재생할 수 있는 모든 실제 AudioClip Addressables 키를 수집합니다.
    /// Variant 사운드는 런타임 선택 결과가 달라질 수 있으므로 활성 후보와 폴백 리소스를 모두 포함합니다.
    /// </summary>
    public sealed class SoundUsageAddressKeyResolver
    {
        private readonly TableLoaderManager _tableLoaderManager;

        /// <summary>
        /// 지정한 테이블 로더를 기준으로 사용처 키 해석기를 생성합니다.
        /// </summary>
        /// <param name="tableLoaderManager">사운드 대표 및 실제 리소스 테이블을 보유한 로더입니다.</param>
        public SoundUsageAddressKeyResolver(TableLoaderManager tableLoaderManager)
        {
            _tableLoaderManager = tableLoaderManager;
        }

        /// <summary>
        /// 대표 sound UID 목록을 실제 AudioClip Addressables 키 목록으로 변환합니다.
        /// </summary>
        /// <param name="soundUids">해석할 대표 sound UID 목록입니다.</param>
        /// <param name="logWarnings">잘못된 UID나 리소스 연결을 경고로 출력할지 여부입니다.</param>
        /// <returns>대소문자를 구분하지 않고 중복이 제거된 Addressables 키 목록입니다.</returns>
        public IReadOnlyList<string> ResolveAddressKeys(IEnumerable<int> soundUids, bool logWarnings = true)
        {
            List<string> result = new List<string>();
            HashSet<string> registeredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<int> registeredSoundUids = new HashSet<int>();

            if (_tableLoaderManager == null || soundUids == null)
                return result;

            foreach (int soundUid in soundUids)
            {
                if (soundUid <= 0 || !registeredSoundUids.Add(soundUid))
                    continue;

                AppendSoundKeys(soundUid, result, registeredKeys, logWarnings);
            }

            return result;
        }

        /// <summary>
        /// 대표 sound UID 한 건에 연결될 수 있는 실제 리소스 키를 결과 목록에 추가합니다.
        /// </summary>
        private void AppendSoundKeys(
            int soundUid,
            List<string> target,
            HashSet<string> registeredKeys,
            bool logWarnings)
        {
            StruckTableSound sound = _tableLoaderManager.GetSoundData(soundUid, false);
            if (sound == null)
            {
                if (logWarnings)
                    GcLogger.LogWarning($"[SoundUsage] sound 테이블에서 UID를 찾지 못했습니다. soundUid={soundUid}");
                return;
            }

            bool added = false;
            bool hasIntentionalSilentCandidate = false;
            if (sound.ResolveMode == SoundConstants.ResolveMode.Variant)
            {
                IReadOnlyList<StruckTableSoundVariant> variants =
                    _tableLoaderManager.TableSoundVariant?.GetVariants(soundUid) ?? Array.Empty<StruckTableSoundVariant>();
                bool hasEnabledVariant = false;
                for (int i = 0; i < variants.Count; i++)
                {
                    StruckTableSoundVariant variant = variants[i];
                    if (variant == null || !variant.Enabled)
                        continue;

                    hasEnabledVariant = true;
                    if (variant.CandidateResourceUid <= 0)
                    {
                        hasIntentionalSilentCandidate = true;
                    }
                    else
                    {
                        added |= AppendResourceKey(
                            sound,
                            variant.CandidateResourceUid,
                            target,
                            registeredKeys,
                            logWarnings);
                    }
                }

                if (!hasEnabledVariant)
                {
                    if (sound.FallbackResourceUid > 0)
                    {
                        added |= AppendResourceKey(
                            sound,
                            sound.FallbackResourceUid,
                            target,
                            registeredKeys,
                            logWarnings);
                    }

                    // 선택 가능한 Variant가 없고 폴백도 없으면 SoundResolver가 첫 직접 연결 리소스를 사용합니다.
                    if (!added)
                        added |= AppendFirstResourceKey(sound, target, registeredKeys);
                }
            }
            else
            {
                added |= AppendFirstResourceKey(sound, target, registeredKeys);
            }

            if (!added && !hasIntentionalSilentCandidate && logWarnings)
            {
                GcLogger.LogWarning(
                    $"[SoundUsage] 대표 사운드에 연결된 실제 AudioClip을 찾지 못했습니다. soundUid={soundUid}, type={sound.Type}");
            }
        }

        /// <summary>
        /// 대표 사운드 타입에 맞는 첫 번째 실제 리소스 키를 추가합니다.
        /// </summary>
        private bool AppendFirstResourceKey(
            StruckTableSound sound,
            List<string> target,
            HashSet<string> registeredKeys)
        {
            StruckTableSoundResource resource = sound.Type switch
            {
                SoundConstants.Type.Bgm => _tableLoaderManager.TableSoundBgm?.GetFirstBySoundUid(sound.Uid),
                SoundConstants.Type.Ambient => _tableLoaderManager.TableSoundAmbient?.GetFirstBySoundUid(sound.Uid),
                SoundConstants.Type.Sfx => _tableLoaderManager.TableSoundSfx?.GetFirstBySoundUid(sound.Uid),
                _ => null,
            };

            return AppendResourceKey(resource, target, registeredKeys);
        }

        /// <summary>
        /// 실제 리소스 UID를 대표 사운드 타입에 맞는 테이블에서 조회한 뒤 키를 추가합니다.
        /// </summary>
        private bool AppendResourceKey(
            StruckTableSound sound,
            int resourceUid,
            List<string> target,
            HashSet<string> registeredKeys,
            bool logWarnings)
        {
            StruckTableSoundResource resource = sound.Type switch
            {
                SoundConstants.Type.Bgm => _tableLoaderManager.TableSoundBgm?.GetDataByUid(resourceUid),
                SoundConstants.Type.Ambient => _tableLoaderManager.TableSoundAmbient?.GetDataByUid(resourceUid),
                SoundConstants.Type.Sfx => _tableLoaderManager.TableSoundSfx?.GetDataByUid(resourceUid),
                _ => null,
            };

            if (resource == null && logWarnings)
            {
                GcLogger.LogWarning(
                    $"[SoundUsage] 실제 사운드 리소스를 찾지 못했습니다. soundUid={sound.Uid}, resourceUid={resourceUid}, type={sound.Type}");
            }

            return AppendResourceKey(resource, target, registeredKeys);
        }

        /// <summary>
        /// 실제 사운드 리소스의 Addressables 키를 중복 없이 추가합니다.
        /// </summary>
        private static bool AppendResourceKey(
            StruckTableSoundResource resource,
            List<string> target,
            HashSet<string> registeredKeys)
        {
            string addressKey = resource?.BuildAddressKey();
            if (string.IsNullOrWhiteSpace(addressKey))
                return false;

            if (registeredKeys.Add(addressKey))
                target.Add(addressKey);

            return true;
        }
    }
}
