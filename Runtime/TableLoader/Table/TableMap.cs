using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 테이블 Structure
    /// </summary>
    public class StruckTableMap : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public int Chapter;
        public MapConstants.Type Type;
        public MapConstants.SubType Subtype;
        public string FolderName;
        public Vector2 PlayerSpawnPosition;
        public int PlayerDeadSpawnUid;

        /// <summary>
        /// 기존 단일 BGM 설정과의 하위 호환성을 위한 대표 sound UID입니다.
        /// <see cref="BgmUids"/>가 비어 있을 때만 복수 BGM 후보에 포함됩니다.
        /// </summary>
        public int BgmUid;

        /// <summary>
        /// 맵 진입 시 무작위로 하나를 선택하여 재생할 BGM sound UID 목록입니다.
        /// </summary>
        public int[] BgmUids = System.Array.Empty<int>();

        /// <summary>
        /// 맵 진입 시 동시에 재생할 환경음 sound UID 목록입니다.
        /// </summary>
        public int[] AmbientSoundUids = System.Array.Empty<int>();

        /// <summary>
        /// 맵 진입 시 카메라 Follow Offset을 테이블 값으로 덮어쓸지 여부입니다.
        /// </summary>
        public bool UseCameraFollowOffset;

        /// <summary>
        /// <see cref="UseCameraFollowOffset"/>이 참일 때 적용할 카메라 Follow Offset 값입니다.
        /// </summary>
        public Vector2 CameraFollowOffset;

        /// <summary>
        /// 맵 진입 시 카메라 Follow Dead Zone을 테이블 값으로 덮어쓸지 여부입니다.
        /// </summary>
        public bool UseCameraFollowDeadZone;

        /// <summary>
        /// <see cref="UseCameraFollowDeadZone"/>이 참일 때 적용할 카메라 Follow Dead Zone 반경입니다.
        /// X 또는 Y 값이 0이면 해당 축의 데드존을 사용하지 않습니다.
        /// </summary>
        public Vector2 CameraFollowDeadZone;

        /// <summary>
        /// 맵 진입 시 카메라 하단 Follow Offset 정책을 테이블 값으로 덮어쓸지 여부입니다.
        /// </summary>
        public bool UseCameraBottomFollowOffsetPolicy;

        /// <summary>
        /// <see cref="UseCameraBottomFollowOffsetPolicy"/>가 참일 때 적용할 하단 Follow Offset 정책입니다.
        /// </summary>
        public CameraBottomFollowOffsetPolicy BottomFollowOffsetPolicy;

        /// <summary>
        /// 현재 맵에서 자동 이동 사용 여부를 전역 설정 기준으로 덮어쓸 정책입니다.
        /// </summary>
        public MapAutoMovePolicy AutoMovePolicy;

        /// <summary>
        /// 현재 맵에서 Parallax 배경 연출을 사용할지 여부입니다.
        /// 활성화된 맵은 카메라와 플레이어의 맵 경계 제한을 런타임에서 해제합니다.
        /// </summary>
        public bool UseParallax;
    }

    /// <summary>
    /// 맵 테이블
    /// </summary>
    public class TableMap : DefaultTable<StruckTableMap>
    {
        public override string Key => ConfigAddressableTable.Map;

        /// <summary>
        /// 테이블 데이터 1행이 로드된 직후 호출된다.
        /// </summary>
        /// <param name="data">로드된 어펙트 데이터.</param>
        /// <remarks>
        /// 로컬라이징 시스템이 존재하면 UID 기반으로 이름을 치환한다.
        /// 기존 방식과의 호환을 위해 로컬라이징이 없을 경우 Memo를 이름으로 사용한다.
        /// </remarks>
        protected override void OnLoadedData(StruckTableMap data)
        {
            if (data == null) return;

            // 기존 방식과의 호환: 로컬라이징 키가 비어있으면 uid 문자열을 사용한다.
            if (LocalizationManager.Instance != null)
            {
                data.Name = LocalizationManager.Instance.GetMapNameByKey($"{data.Uid}");
            }
            else
            {
                data.Name = $"{data.Name}";
            }

            if (AddressableLoaderSettings.Instance && AddressableLoaderSettings.Instance.mapSettings &&
                AddressableLoaderSettings.Instance.mapSettings.EnableMapUid)
            {
                data.Name += $" ({data.Uid})";
            }
        }

        /// <summary>
        /// map 테이블 한 행을 런타임에서 사용하는 맵 데이터 구조체로 변환합니다.
        /// 신규 카메라 오버라이드 컬럼이 비어 있거나 누락된 경우 기존 기본 동작을 유지합니다.
        /// </summary>
        /// <param name="data">헤더명 기준으로 파싱된 원본 문자열 데이터입니다.</param>
        /// <returns>파싱된 맵 테이블 행 데이터입니다.</returns>
        protected override StruckTableMap BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);

            return new StruckTableMap
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                Chapter = reader.Int("Chapter"),
                Type = reader.Enum<MapConstants.Type>("Type"),
                Subtype = reader.Enum<MapConstants.SubType>("Subtype"),
                FolderName = reader.String("FolderName"),
                PlayerSpawnPosition = reader.Vector2("PlayerSpawnPosition"),
                PlayerDeadSpawnUid = reader.Int("PlayerDeadSpawnUid"),
                BgmUids = reader.IntArray("BgmUids"),
                AmbientSoundUids = reader.IntArray("AmbientSoundUids"),
                UseCameraFollowOffset = reader.BoolYN("UseCameraFollowOffset"),
                CameraFollowOffset = reader.Vector2("CameraFollowOffset"),
                UseCameraFollowDeadZone = reader.BoolYN("UseCameraFollowDeadZone"),
                CameraFollowDeadZone = reader.Vector2("CameraFollowDeadZone"),
                UseCameraBottomFollowOffsetPolicy = reader.BoolYN("UseCameraBottomFollowOffsetPolicy"),
                BottomFollowOffsetPolicy = reader.Enum<CameraBottomFollowOffsetPolicy>("BottomFollowOffsetPolicy"),
                AutoMovePolicy = reader.Enum<MapAutoMovePolicy>("AutoMovePolicy"),
                UseParallax = reader.BoolYN("UseParallax"),
            };
        }
    }
}
