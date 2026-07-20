using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Core 외부 패키지가 사운드 사용 매니페스트 분석 과정에 참여하기 위한 에디터 확장 계약입니다.
    /// </summary>
    /// <remarks>
    /// Core Editor는 구현 패키지를 직접 참조하지 않고 Unity <c>TypeCache</c>로 구현체를 검색합니다.
    /// 상위 패키지는 이 인터페이스를 구현하여 자신이 소유한 데이터와 에셋의 사운드 사용처를 추가할 수 있습니다.
    /// </remarks>
    public interface ISoundUsageManifestContributor
    {
        /// <summary>
        /// 동일한 생성 과정에 여러 확장기가 참여할 때 사용할 실행 순서입니다.
        /// 낮은 값부터 실행합니다.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// 진단 메시지에 표시할 확장기 이름입니다.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 패키지가 소유한 테이블과 에셋을 분석하여 사운드 사용처를 생성 컨텍스트에 추가합니다.
        /// </summary>
        /// <param name="context">Core 기본 분석 결과와 레코드 추가 API를 제공하는 생성 컨텍스트입니다.</param>
        void Collect(SoundUsageManifestBuildContext context);
    }

    /// <summary>
    /// Core 외부 패키지가 사운드 매니페스트 변경 감지용 원본 경로를 제공하기 위한 에디터 확장 계약입니다.
    /// </summary>
    /// <remarks>
    /// Core Editor는 구현 패키지를 직접 참조하지 않고 Unity <c>TypeCache</c>로 구현체를 검색합니다.
    /// 설정 에셋처럼 Core가 알 수 없는 원본을 등록하면 변경 후 매니페스트 재생성 누락을 검출할 수 있습니다.
    /// </remarks>
    public interface ISoundUsageManifestSourceContributor
    {
        /// <summary>
        /// 매니페스트 분석 결과에 영향을 주는 에셋 또는 텍스트 원본 경로를 등록합니다.
        /// </summary>
        /// <param name="context">중복을 제거하여 원본 경로를 수집하는 컨텍스트입니다.</param>
        void CollectSourcePaths(SoundUsageManifestSourceContext context);
    }

    /// <summary>
    /// 외부 패키지의 사운드 매니페스트 원본 경로를 안전하게 수집합니다.
    /// </summary>
    public sealed class SoundUsageManifestSourceContext
    {
        private readonly ISet<string> _paths;

        /// <summary>
        /// Core 내부 지문 계산기가 사용할 원본 경로 집합을 연결합니다.
        /// </summary>
        /// <param name="paths">대소문자를 구분하지 않는 원본 경로 집합입니다.</param>
        internal SoundUsageManifestSourceContext(ISet<string> paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Unity 프로젝트 기준 에셋 또는 파일 경로를 원본 목록에 추가합니다.
        /// </summary>
        /// <param name="path">매니페스트 분석 결과에 영향을 주는 원본 경로입니다.</param>
        public void AddPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
                _paths.Add(path.Replace('\\', '/'));
        }
    }

    /// <summary>
    /// 맵에 배치된 몬스터 한 종류를 나타내는 자동 분석 입력 값입니다.
    /// </summary>
    public readonly struct SoundUsageManifestMapMonsterPlacement
    {
        /// <summary>몬스터가 배치된 맵 UID입니다.</summary>
        public int MapUid { get; }

        /// <summary>맵 배치 JSON에서 발견한 몬스터 UID입니다.</summary>
        public int MonsterUid { get; }

        /// <summary>
        /// 맵과 몬스터 UID를 보관하는 배치 값을 생성합니다.
        /// </summary>
        /// <param name="mapUid">몬스터가 배치된 맵 UID입니다.</param>
        /// <param name="monsterUid">배치된 몬스터 UID입니다.</param>
        public SoundUsageManifestMapMonsterPlacement(int mapUid, int monsterUid)
        {
            MapUid = mapUid;
            MonsterUid = monsterUid;
        }
    }

    /// <summary>
    /// Core 기본 분석기와 외부 패키지 확장기가 공유하는 사운드 매니페스트 생성 컨텍스트입니다.
    /// </summary>
    public sealed class SoundUsageManifestBuildContext
    {
        private readonly List<SoundUsageManifestBuildRecord> _records;
        private readonly SoundUsageManifestBuildResult _result;
        private readonly TableMap _tableMap;
        private readonly TableMonster _tableMonster;
        private readonly TableSound _tableSound;
        private readonly List<SoundUsageManifestMapMonsterPlacement> _mapMonsterPlacements =
            new List<SoundUsageManifestMapMonsterPlacement>();
        private readonly HashSet<long> _mapMonsterPlacementKeys = new HashSet<long>();

        /// <summary>
        /// 현재까지 맵 배치 JSON에서 발견한 고유 몬스터 배치 목록입니다.
        /// </summary>
        public IReadOnlyList<SoundUsageManifestMapMonsterPlacement> MapMonsterPlacements =>
            _mapMonsterPlacements;

        /// <summary>
        /// 빌더 내부에서 사용할 생성 컨텍스트를 초기화합니다.
        /// </summary>
        /// <param name="records">최종 정규화 전 원본 레코드 목록입니다.</param>
        /// <param name="result">진단 결과를 기록할 객체입니다.</param>
        /// <param name="tableMap">Core 맵 테이블입니다.</param>
        /// <param name="tableMonster">Core 몬스터 테이블입니다.</param>
        /// <param name="tableSound">Core 대표 사운드 테이블입니다.</param>
        internal SoundUsageManifestBuildContext(
            List<SoundUsageManifestBuildRecord> records,
            SoundUsageManifestBuildResult result,
            TableMap tableMap,
            TableMonster tableMonster,
            TableSound tableSound)
        {
            _records = records ?? throw new ArgumentNullException(nameof(records));
            _result = result ?? throw new ArgumentNullException(nameof(result));
            _tableMap = tableMap;
            _tableMonster = tableMonster;
            _tableSound = tableSound;
        }

        /// <summary>
        /// 맵 배치 JSON에서 발견한 몬스터를 외부 패키지 분석용 입력 목록에 등록합니다.
        /// 동일한 맵과 몬스터 조합은 한 번만 보관합니다.
        /// </summary>
        /// <param name="mapUid">몬스터가 배치된 맵 UID입니다.</param>
        /// <param name="monsterUid">배치된 몬스터 UID입니다.</param>
        internal void RegisterMapMonsterPlacement(int mapUid, int monsterUid)
        {
            if (mapUid <= 0 || monsterUid <= 0)
                return;

            long key = ((long)mapUid << 32) | (uint)monsterUid;
            if (!_mapMonsterPlacementKeys.Add(key))
                return;

            _mapMonsterPlacements.Add(
                new SoundUsageManifestMapMonsterPlacement(mapUid, monsterUid));
        }

        /// <summary>
        /// Core monster 테이블에서 지정한 몬스터 정보를 조회합니다.
        /// </summary>
        /// <param name="monsterUid">조회할 몬스터 UID입니다.</param>
        /// <param name="monster">조회에 성공한 몬스터 행입니다.</param>
        /// <returns>유효한 몬스터 행을 찾았으면 true입니다.</returns>
        public bool TryGetMonster(int monsterUid, out StruckTableMonster monster)
        {
            monster = null;
            return monsterUid > 0 &&
                   _tableMonster != null &&
                   _tableMonster.TryGetDataByUid(monsterUid, out monster) &&
                   monster != null;
        }

        /// <summary>
        /// Core map 테이블에서 지정한 맵 정보를 조회합니다.
        /// </summary>
        /// <param name="mapUid">조회할 맵 UID입니다.</param>
        /// <param name="map">조회에 성공한 맵 행입니다.</param>
        /// <returns>유효한 맵 행을 찾았으면 true입니다.</returns>
        public bool TryGetMap(int mapUid, out StruckTableMap map)
        {
            map = null;
            return mapUid > 0 &&
                   _tableMap != null &&
                   _tableMap.TryGetDataByUid(mapUid, out map) &&
                   map != null;
        }

        /// <summary>
        /// Core sound 테이블에서 지정한 대표 사운드 정보를 조회합니다.
        /// </summary>
        /// <param name="soundUid">조회할 대표 사운드 UID입니다.</param>
        /// <param name="sound">조회에 성공한 대표 사운드 행입니다.</param>
        /// <returns>유효한 대표 사운드 행을 찾았으면 true입니다.</returns>
        public bool TryGetSound(int soundUid, out StruckTableSound sound)
        {
            sound = null;
            return soundUid > 0 &&
                   _tableSound != null &&
                   _tableSound.TryGetDataByUid(soundUid, out sound) &&
                   sound != null;
        }

        /// <summary>
        /// 외부 패키지 설정에서 발견한 게임 전역 사운드 사용처를 원본 레코드에 추가합니다.
        /// 게임 시작 시 로드한 참조는 사운드 로더가 파괴될 때까지 유지됩니다.
        /// </summary>
        /// <param name="soundUid">sound 테이블의 대표 UID입니다.</param>
        /// <param name="sourceType">사운드 사용처의 원본 종류입니다.</param>
        /// <param name="sourceUid">원본 데이터 UID입니다. 설정 에셋처럼 UID가 없으면 0을 사용합니다.</param>
        /// <param name="sourcePath">원본 에셋 또는 테이블 위치입니다.</param>
        /// <param name="memo">진단 및 추적에 사용할 설명입니다.</param>
        public void AddGlobalSoundUsage(
            int soundUid,
            SoundUsageManifestSourceType sourceType,
            int sourceUid,
            string sourcePath,
            string memo)
        {
            if (soundUid <= 0)
                return;

            _records.Add(new SoundUsageManifestBuildRecord
            {
                ScopeType = SoundUsageManifestScopeType.Global,
                ScopeUid = SoundUsageManifestScopeIds.Global,
                SoundUid = soundUid,
                SourceType = sourceType,
                SourceUid = sourceUid,
                SourcePath = sourcePath,
                Memo = memo,
            });
        }

        /// <summary>
        /// 외부 패키지에서 발견한 맵 범위 사운드 사용처를 원본 레코드에 추가합니다.
        /// 잘못된 UID와 중복 항목은 최종 정규화 단계에서 다시 검증합니다.
        /// </summary>
        /// <param name="mapUid">사운드를 유지할 맵 UID입니다.</param>
        /// <param name="soundUid">sound 테이블의 대표 UID입니다.</param>
        /// <param name="sourceType">사운드 사용처의 원본 종류입니다.</param>
        /// <param name="sourceUid">스킬 UID 등 원본 데이터 UID입니다.</param>
        /// <param name="sourcePath">원본 에셋 또는 테이블 위치입니다.</param>
        /// <param name="memo">진단 및 추적에 사용할 설명입니다.</param>
        public void AddMapSoundUsage(
            int mapUid,
            int soundUid,
            SoundUsageManifestSourceType sourceType,
            int sourceUid,
            string sourcePath,
            string memo)
        {
            if (mapUid <= 0 || soundUid <= 0)
                return;

            _records.Add(new SoundUsageManifestBuildRecord
            {
                ScopeType = SoundUsageManifestScopeType.Map,
                ScopeUid = mapUid,
                SoundUid = soundUid,
                SourceType = sourceType,
                SourceUid = sourceUid,
                SourcePath = sourcePath,
                Memo = memo,
            });
        }

        /// <summary>
        /// 외부 분석 과정에서 발견한 누락 또는 잘못된 설정을 생성 결과 경고로 기록합니다.
        /// </summary>
        /// <param name="message">사용자에게 표시할 경고 메시지입니다.</param>
        public void AddWarning(string message)
        {
            _result.AddWarning(message);
        }

        /// <summary>
        /// 외부 분석기의 진행 또는 결과 메시지를 생성 결과에 기록합니다.
        /// </summary>
        /// <param name="message">사용자에게 표시할 일반 메시지입니다.</param>
        public void AddMessage(string message)
        {
            _result.AddMessage(message);
        }
    }
}
