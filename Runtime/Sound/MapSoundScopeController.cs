using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 전환에 맞춰 map_sound 사운드 범위를 준비하고 BGM 및 환경음을 활성화합니다.
    /// 다음 맵 범위를 먼저 획득한 뒤 이전 맵 범위를 해제하여 공용 AudioClip이 전환 중 내려가지 않도록 보호합니다.
    /// </summary>
    public sealed class MapSoundScopeController : IDisposable
    {
        private readonly TableLoaderManager _tableLoaderManager;
        private readonly AddressableLoaderSound _addressableLoaderSound;
        private readonly SoundUsageAddressKeyResolver _addressKeyResolver;

        private SoundScopeLease _activeLease;
        private SoundScopeLease _pendingLease;
        private IReadOnlyList<StruckTableMapSound> _pendingRows = Array.Empty<StruckTableMapSound>();
        private int _pendingMapUid;
        private int _prepareVersion;
        private bool _hasPendingTransition;
        private bool _isDisposed;

        /// <summary>
        /// 맵 사운드 범위 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="tableLoaderManager">맵 및 사운드 테이블 로더입니다.</param>
        /// <param name="addressableLoaderSound">범위 참조를 관리할 사운드 로더입니다.</param>
        public MapSoundScopeController(
            TableLoaderManager tableLoaderManager,
            AddressableLoaderSound addressableLoaderSound)
        {
            _tableLoaderManager = tableLoaderManager;
            _addressableLoaderSound = addressableLoaderSound;
            _addressKeyResolver = new SoundUsageAddressKeyResolver(tableLoaderManager);
        }

        /// <summary>
        /// 다음 맵에서 사용할 사운드 행을 조회하고 모든 AudioClip 범위를 미리 획득합니다.
        /// map_sound에 BGM 행이 없을 때는 map.BgmUid를 호환용 범위에 포함합니다.
        /// </summary>
        /// <param name="mapUid">준비할 맵 UID입니다.</param>
        /// <param name="legacyBgmUid">기존 map.BgmUid 값입니다.</param>
        public async Task PrepareAsync(int mapUid, int legacyBgmUid)
        {
            if (_isDisposed || mapUid <= 0)
                return;

            int requestVersion = ++_prepareVersion;
            ReleasePendingLease();

            IReadOnlyList<StruckTableMapSound> rows =
                _tableLoaderManager?.TableMapSound?.GetRowsByMapUid(mapUid) ?? Array.Empty<StruckTableMapSound>();
            List<int> soundUids = new List<int>(rows.Count + 1);
            bool hasConfiguredBgm = false;

            for (int i = 0; i < rows.Count; i++)
            {
                StruckTableMapSound row = rows[i];
                if (row == null || row.SoundUid <= 0)
                    continue;

                soundUids.Add(row.SoundUid);
                hasConfiguredBgm |= ResolveRole(row) == MapSoundRole.Bgm;
            }

            if (!hasConfiguredBgm && legacyBgmUid > 0)
                soundUids.Add(legacyBgmUid);

            IReadOnlyList<string> addressKeys = _addressKeyResolver.ResolveAddressKeys(soundUids);
            SoundScopeLease acquiredLease = null;
            if (addressKeys.Count > 0 && _addressableLoaderSound != null)
            {
                try
                {
                    acquiredLease = await _addressableLoaderSound.AcquireScopeAsync(
                        SoundUsageScopeKey.Map(mapUid),
                        addressKeys);
                }
                catch (Exception ex)
                {
                    GcLogger.LogWarning(
                        $"[MapSound] 맵 사운드 범위를 준비하지 못했습니다. mapUid={mapUid}, error={ex.Message}");
                }
            }

            if (_isDisposed || requestVersion != _prepareVersion)
            {
                acquiredLease?.Dispose();
                return;
            }

            _pendingLease = acquiredLease;
            _pendingRows = rows;
            _pendingMapUid = mapUid;
            _hasPendingTransition = true;
        }

        /// <summary>
        /// 준비된 맵 사운드를 활성화하고 이전 맵 범위를 해제합니다.
        /// 맵 환경음은 먼저 모두 정지한 뒤 새 행을 재생하며, BGM은 map_sound가 없을 때만 기존 map.BgmUid를 사용합니다.
        /// </summary>
        /// <param name="soundManager">BGM과 환경음을 재생할 사운드 매니저입니다.</param>
        /// <param name="mapUid">활성화할 맵 UID입니다.</param>
        /// <param name="legacyBgmUid">기존 map.BgmUid 값입니다.</param>
        public void Activate(SoundManager soundManager, int mapUid, int legacyBgmUid)
        {
            if (_isDisposed || !_hasPendingTransition || _pendingMapUid != mapUid)
                return;

            soundManager?.StopAmbient();

            bool hasConfiguredBgm = false;
            bool hasPlayedBgm = false;
            HashSet<string> ambientLayerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _pendingRows.Count; i++)
            {
                StruckTableMapSound row = _pendingRows[i];
                if (row == null || row.SoundUid <= 0)
                    continue;

                MapSoundRole role = ResolveRole(row);
                if (role == MapSoundRole.Bgm)
                    hasConfiguredBgm = true;

                if (!row.AutoPlay)
                    continue;

                if (!IsSoundTypeCompatible(row, role))
                    continue;

                switch (role)
                {
                    case MapSoundRole.Bgm:
                        if (hasPlayedBgm)
                        {
                            GcLogger.LogWarning(
                                $"[MapSound] 자동 재생 BGM은 맵당 한 개만 사용할 수 있습니다. mapUid={mapUid}, skippedUid={row.Uid}");
                            continue;
                        }

                        soundManager?.PlayBgmByUid(row.SoundUid, row.FadeDurationOverride);
                        hasPlayedBgm = true;
                        break;

                    case MapSoundRole.Ambient:
                        string layerKey = row.LayerKey?.Trim();
                        if (!string.IsNullOrWhiteSpace(layerKey) && !ambientLayerKeys.Add(layerKey))
                        {
                            GcLogger.LogWarning(
                                $"[MapSound] 같은 LayerKey의 환경음이 중복되어 뒤 행을 건너뜁니다. mapUid={mapUid}, layerKey={layerKey}, skippedUid={row.Uid}");
                            continue;
                        }

                        soundManager?.PlayByUid(row.SoundUid);
                        break;
                }
            }

            if (!hasConfiguredBgm)
            {
                if (legacyBgmUid > 0)
                    soundManager?.PlayBgmByUid(legacyBgmUid);
                else
                    soundManager?.StopBgm();
            }
            else if (!hasPlayedBgm)
            {
                // BGM 행을 명시했지만 자동 재생하지 않는 맵은 기존 BGM을 정지하고 외부 연출이 직접 재생하도록 합니다.
                soundManager?.StopBgm();
            }

            SoundScopeLease previousLease = _activeLease;
            _activeLease = _pendingLease;
            _pendingLease = null;
            _pendingRows = Array.Empty<StruckTableMapSound>();
            _pendingMapUid = 0;
            _hasPendingTransition = false;
            previousLease?.Dispose();
        }

        /// <summary>
        /// 맵 로드 실패 또는 새 전환 요청으로 아직 활성화하지 않은 범위를 해제합니다.
        /// </summary>
        public void CancelPending()
        {
            _prepareVersion++;
            ReleasePendingLease();
        }

        /// <summary>
        /// 현재 및 준비 중인 모든 맵 사운드 범위를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _prepareVersion++;
            ReleasePendingLease();
            _activeLease?.Dispose();
            _activeLease = null;
        }

        /// <summary>
        /// 행에 역할이 지정되지 않은 경우 대표 사운드 Type으로 BGM 또는 환경음 역할을 판정합니다.
        /// </summary>
        private MapSoundRole ResolveRole(StruckTableMapSound row)
        {
            if (row == null || row.Role != MapSoundRole.None)
                return row?.Role ?? MapSoundRole.None;

            StruckTableSound sound = _tableLoaderManager?.GetSoundData(row.SoundUid, false);
            return sound?.Type switch
            {
                SoundConstants.Type.Bgm => MapSoundRole.Bgm,
                SoundConstants.Type.Ambient => MapSoundRole.Ambient,
                _ => MapSoundRole.PreloadOnly,
            };
        }

        /// <summary>
        /// 자동 재생 역할과 대표 사운드 Type이 일치하는지 검사합니다.
        /// PreloadOnly는 모든 사운드 타입을 허용합니다.
        /// </summary>
        /// <param name="row">검사할 맵 사운드 행입니다.</param>
        /// <param name="role">행에 적용할 최종 역할입니다.</param>
        /// <returns>역할에 맞는 사운드 타입이면 true를 반환합니다.</returns>
        private bool IsSoundTypeCompatible(StruckTableMapSound row, MapSoundRole role)
        {
            if (row == null || role == MapSoundRole.PreloadOnly || role == MapSoundRole.None)
                return true;

            StruckTableSound sound = _tableLoaderManager?.GetSoundData(row.SoundUid, false);
            SoundConstants.Type expectedType = role == MapSoundRole.Bgm
                ? SoundConstants.Type.Bgm
                : SoundConstants.Type.Ambient;
            if (sound != null && sound.Type == expectedType)
                return true;

            GcLogger.LogWarning(
                $"[MapSound] Role과 대표 사운드 Type이 일치하지 않아 자동 재생하지 않습니다. rowUid={row.Uid}, soundUid={row.SoundUid}, role={role}, type={sound?.Type}");
            return false;
        }

        /// <summary>
        /// 준비 중인 임대 객체와 전환 데이터를 초기화합니다.
        /// </summary>
        private void ReleasePendingLease()
        {
            _pendingLease?.Dispose();
            _pendingLease = null;
            _pendingRows = Array.Empty<StruckTableMapSound>();
            _pendingMapUid = 0;
            _hasPendingTransition = false;
        }
    }
}
