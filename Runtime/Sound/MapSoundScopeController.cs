using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private IReadOnlyList<int> _pendingBgmUids = Array.Empty<int>();
        private IReadOnlyList<int> _pendingAmbientSoundUids = Array.Empty<int>();
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
        /// 다음 맵에서 사용할 사운드 행과 map 테이블의 복수 BGM·환경음 목록을 조회하여
        /// 필요한 모든 AudioClip 범위를 미리 획득합니다.
        /// </summary>
        /// <param name="mapUid">준비할 맵 UID입니다.</param>
        /// <param name="mapData">복수 BGM 및 환경음 설정을 포함한 map 테이블 행입니다.</param>
        public async Task PrepareAsync(int mapUid, StruckTableMap mapData)
        {
            if (_isDisposed || mapUid <= 0)
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();
            int requestVersion = ++_prepareVersion;
            ReleasePendingLease();

            IReadOnlyList<StruckTableMapSound> rows =
                _tableLoaderManager?.TableMapSound?.GetRowsByMapUid(mapUid) ?? Array.Empty<StruckTableMapSound>();
            IReadOnlyList<int> mapBgmUids = mapData?.BgmUids ?? Array.Empty<int>();
            IReadOnlyList<int> mapAmbientSoundUids = mapData?.AmbientSoundUids ?? Array.Empty<int>();
            List<int> soundUids =
                new List<int>(rows.Count + mapBgmUids.Count + mapAmbientSoundUids.Count);
            bool hasConfiguredBgm = false;

            for (int i = 0; i < rows.Count; i++)
            {
                StruckTableMapSound row = rows[i];
                if (row == null || row.SoundUid <= 0)
                    continue;

                soundUids.Add(row.SoundUid);
                hasConfiguredBgm |= ResolveRole(row) == MapSoundRole.Bgm;
            }

            AppendGeneratedManifestSoundUids(mapUid, soundUids);

            if (!hasConfiguredBgm)
                AppendValidSoundUids(mapBgmUids, soundUids);

            AppendValidSoundUids(mapAmbientSoundUids, soundUids);

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

            stopwatch.Stop();
            LogPrepareMetrics(mapUid, addressKeys, acquiredLease, stopwatch.Elapsed.TotalSeconds);

            _pendingLease = acquiredLease;
            _pendingRows = rows;
            _pendingBgmUids = mapBgmUids;
            _pendingAmbientSoundUids = mapAmbientSoundUids;
            _pendingMapUid = mapUid;
            _hasPendingTransition = true;
        }

        /// <summary>
        /// 기존 단일 BGM 호출부와의 하위 호환성을 유지하며 다음 맵 사운드 범위를 준비합니다.
        /// 신규 호출부는 복수 사운드를 전달할 수 있는 <see cref="PrepareAsync(int, StruckTableMap)"/>을 사용합니다.
        /// </summary>
        /// <param name="mapUid">준비할 맵 UID입니다.</param>
        /// <param name="legacyBgmUid">기존 map.BgmUid 값입니다.</param>
        public Task PrepareAsync(int mapUid, int legacyBgmUid)
        {
            StruckTableMap compatibilityMapData = new StruckTableMap
            {
                BgmUid = legacyBgmUid,
                BgmUids = legacyBgmUid > 0 ? new[] { legacyBgmUid } : Array.Empty<int>(),
            };
            return PrepareAsync(mapUid, compatibilityMapData);
        }

        /// <summary>
        /// 준비된 맵 사운드를 활성화하고 이전 맵 범위를 해제합니다.
        /// 환경음은 map 테이블과 map_sound 설정을 함께 재생하고, BGM 후보가 여러 개면 하나를 무작위 선택합니다.
        /// </summary>
        /// <param name="soundManager">BGM과 환경음을 재생할 사운드 매니저입니다.</param>
        /// <param name="mapUid">활성화할 맵 UID입니다.</param>
        public void Activate(SoundManager soundManager, int mapUid)
        {
            if (_isDisposed || !_hasPendingTransition || _pendingMapUid != mapUid)
                return;

            bool hasConfiguredBgm = false;
            HashSet<string> ambientLayerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<int> playedAmbientSoundUids = new HashSet<int>();
            List<StruckTableMapSound> bgmCandidates = new List<StruckTableMapSound>();
            List<SoundFadeRequest> ambientRequests =
                new List<SoundFadeRequest>(_pendingAmbientSoundUids.Count + _pendingRows.Count);

            for (int i = 0; i < _pendingAmbientSoundUids.Count; i++)
            {
                int soundUid = _pendingAmbientSoundUids[i];
                if (soundUid <= 0 ||
                    !IsSoundTypeCompatible(soundUid, MapSoundRole.Ambient, "map.AmbientSoundUids") ||
                    !playedAmbientSoundUids.Add(soundUid))
                {
                    continue;
                }

                ambientRequests.Add(new SoundFadeRequest(soundUid));
            }

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
                        bgmCandidates.Add(row);
                        break;

                    case MapSoundRole.Ambient:
                        string layerKey = row.LayerKey?.Trim();
                        if (!string.IsNullOrWhiteSpace(layerKey) && !ambientLayerKeys.Add(layerKey))
                        {
                            GcLogger.LogWarning(
                                $"[MapSound] 같은 LayerKey의 환경음이 중복되어 뒤 행을 건너뜁니다. mapUid={mapUid}, layerKey={layerKey}, skippedUid={row.Uid}");
                            continue;
                        }

                        if (playedAmbientSoundUids.Add(row.SoundUid))
                        {
                            ambientRequests.Add(new SoundFadeRequest(
                                row.SoundUid,
                                row.UseFadeDurationOverride,
                                row.FadeDurationOverride));
                        }
                        break;
                }
            }

            soundManager?.TransitionAmbient(ambientRequests);

            if (!hasConfiguredBgm)
            {
                int selectedBgmUid = SelectRandomCompatibleSoundUid(
                    _pendingBgmUids,
                    MapSoundRole.Bgm,
                    "map.BgmUids");
                if (selectedBgmUid > 0)
                    soundManager?.PlayBgmByUid(selectedBgmUid);
                else
                    soundManager?.StopBgm();
            }
            else if (bgmCandidates.Count > 0)
            {
                StruckTableMapSound selected =
                    bgmCandidates[UnityEngine.Random.Range(0, bgmCandidates.Count)];
                soundManager?.PlayBgmByUid(
                    selected.SoundUid,
                    selected.FadeDurationOverride,
                    selected.UseFadeDurationOverride);
            }
            else
            {
                // BGM 행을 명시했지만 자동 재생 후보가 없는 맵은 기존 BGM을 정지하고 외부 연출이 직접 재생하도록 합니다.
                soundManager?.StopBgm();
            }

            SoundScopeLease previousLease = _activeLease;
            _activeLease = _pendingLease;
            _pendingLease = null;
            _pendingRows = Array.Empty<StruckTableMapSound>();
            _pendingBgmUids = Array.Empty<int>();
            _pendingAmbientSoundUids = Array.Empty<int>();
            _pendingMapUid = 0;
            _hasPendingTransition = false;
            previousLease?.Dispose();
        }

        /// <summary>
        /// 기존 단일 BGM 활성화 호출부와의 하위 호환성을 유지합니다.
        /// </summary>
        /// <param name="soundManager">BGM과 환경음을 재생할 사운드 매니저입니다.</param>
        /// <param name="mapUid">활성화할 맵 UID입니다.</param>
        /// <param name="legacyBgmUid">기존 map.BgmUid 값입니다.</param>
        public void Activate(SoundManager soundManager, int mapUid, int legacyBgmUid)
        {
            Activate(soundManager, mapUid);
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
        /// 에디터 자동 분석으로 생성된 맵 사운드 사용 매니페스트를 현재 범위에 합칩니다.
        /// 매니페스트가 아직 생성되지 않았거나 로드되지 않은 프로젝트에서는 아무 작업도 하지 않습니다.
        /// </summary>
        /// <param name="mapUid">사운드 사용처를 조회할 맵 UID입니다.</param>
        /// <param name="target">대표 sound UID를 추가할 목록입니다.</param>
        private void AppendGeneratedManifestSoundUids(int mapUid, List<int> target)
        {
            if (mapUid <= 0 || target == null)
                return;

            IReadOnlyList<int> generatedSoundUids = _tableLoaderManager?.TableSoundUsageManifest?.GetSoundUids(
                SoundUsageManifestScopeType.Map,
                mapUid) ?? Array.Empty<int>();

            for (int i = 0; i < generatedSoundUids.Count; i++)
            {
                int soundUid = generatedSoundUids[i];
                if (soundUid > 0)
                    target.Add(soundUid);
            }
        }


        /// <summary>
        /// 설정이 활성화된 경우 맵 사운드 범위의 로드 시간, 성공/실패 키 수 및 메모리 추정값을 출력합니다.
        /// </summary>
        /// <param name="mapUid">프로파일링 대상 맵 UID입니다.</param>
        /// <param name="requestedKeys">요청한 Addressables 키 목록입니다.</param>
        /// <param name="lease">획득된 범위 임대 객체입니다.</param>
        /// <param name="elapsedSeconds">범위 준비에 걸린 전체 시간입니다.</param>
        private void LogPrepareMetrics(
            int mapUid,
            IReadOnlyList<string> requestedKeys,
            SoundScopeLease lease,
            double elapsedSeconds)
        {
            GGemCoSoundSettings settings = AddressableLoaderSettings.Instance?.soundSettings;
            if (settings == null || !settings.IsMapScopeProfilingEnabled())
                return;

            int requestedCount = requestedKeys?.Count ?? 0;
            int loadedCount = lease?.LoadedKeys?.Count ?? 0;
            int failedCount = lease?.FailedKeys?.Count ?? 0;
            long memoryBytes = _addressableLoaderSound?.GetRuntimeMemoryBytes(lease?.LoadedKeys) ?? 0L;
            string message =
                $"[MapSoundProfile] mapUid={mapUid}, elapsed={elapsedSeconds * 1000d:0.###}ms, " +
                $"requested={requestedCount}, loaded={loadedCount}, failed={failedCount}, " +
                $"runtimeMemory={FormatBytes(memoryBytes)}";

            float slowThreshold = settings.GetSlowMapScopeLoadThresholdSeconds();
            if (slowThreshold > 0f && elapsedSeconds >= slowThreshold)
                GcLogger.LogWarning(message);
            else
                GcLogger.Log(message);
        }

        /// <summary>
        /// 바이트 값을 사운드 프로파일 로그에서 읽기 쉬운 단위로 변환합니다.
        /// </summary>
        /// <param name="bytes">변환할 바이트 값입니다.</param>
        /// <returns>B, KB 또는 MB 단위 문자열입니다.</returns>
        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0L)
                return "0 B";
            if (bytes < 1024L)
                return $"{bytes} B";
            if (bytes < 1024L * 1024L)
                return $"{bytes / 1024d:0.##} KB";

            return $"{bytes / (1024d * 1024d):0.##} MB";
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
        /// map 테이블에 직접 등록된 sound UID가 지정한 역할의 사운드 타입과 일치하는지 검사합니다.
        /// </summary>
        /// <param name="soundUid">검사할 대표 sound UID입니다.</param>
        /// <param name="role">기대하는 맵 사운드 역할입니다.</param>
        /// <param name="source">오류 로그에 표시할 원본 설정 위치입니다.</param>
        /// <returns>사운드 타입이 역할과 일치하면 true입니다.</returns>
        private bool IsSoundTypeCompatible(int soundUid, MapSoundRole role, string source)
        {
            StruckTableSound sound = _tableLoaderManager?.GetSoundData(soundUid, false);
            SoundConstants.Type expectedType = role == MapSoundRole.Bgm
                ? SoundConstants.Type.Bgm
                : SoundConstants.Type.Ambient;
            if (sound != null && sound.Type == expectedType)
                return true;

            GcLogger.LogWarning(
                $"[MapSound] map 테이블의 사운드 타입이 역할과 일치하지 않아 자동 재생하지 않습니다. source={source}, soundUid={soundUid}, role={role}, type={sound?.Type}");
            return false;
        }

        /// <summary>
        /// 유효한 sound UID를 대상 목록에 순서대로 추가합니다.
        /// </summary>
        /// <param name="source">추가할 sound UID 목록입니다.</param>
        /// <param name="target">UID를 추가할 대상 목록입니다.</param>
        private static void AppendValidSoundUids(IReadOnlyList<int> source, List<int> target)
        {
            if (source == null || target == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                int soundUid = source[i];
                if (soundUid > 0)
                    target.Add(soundUid);
            }
        }

        /// <summary>
        /// 지정한 역할과 타입이 일치하는 sound UID 후보 중 하나를 무작위로 선택합니다.
        /// 별도 후보 컬렉션을 만들지 않는 reservoir sampling 방식으로 맵 전환 시 불필요한 할당을 피합니다.
        /// </summary>
        /// <param name="candidates">선택할 sound UID 후보 목록입니다.</param>
        /// <param name="role">기대하는 맵 사운드 역할입니다.</param>
        /// <param name="source">오류 로그에 표시할 원본 설정 위치입니다.</param>
        /// <returns>선택된 sound UID이며, 후보가 없으면 0입니다.</returns>
        private int SelectRandomCompatibleSoundUid(
            IReadOnlyList<int> candidates,
            MapSoundRole role,
            string source)
        {
            if (candidates == null || candidates.Count == 0)
                return 0;

            int selectedSoundUid = 0;
            int validCandidateCount = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                int soundUid = candidates[i];
                if (soundUid <= 0 || !IsSoundTypeCompatible(soundUid, role, source))
                    continue;

                validCandidateCount++;
                if (UnityEngine.Random.Range(0, validCandidateCount) == 0)
                    selectedSoundUid = soundUid;
            }

            return selectedSoundUid;
        }

        /// <summary>
        /// 준비 중인 임대 객체와 전환 데이터를 초기화합니다.
        /// </summary>
        private void ReleasePendingLease()
        {
            _pendingLease?.Dispose();
            _pendingLease = null;
            _pendingRows = Array.Empty<StruckTableMapSound>();
            _pendingBgmUids = Array.Empty<int>();
            _pendingAmbientSoundUids = Array.Empty<int>();
            _pendingMapUid = 0;
            _hasPendingTransition = false;
        }
    }
}
