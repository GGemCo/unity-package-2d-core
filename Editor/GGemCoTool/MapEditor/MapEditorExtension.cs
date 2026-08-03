using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Core 맵 배치툴에 외부 패키지 또는 게임 전용 배치 기능을 연결하는 확장 계약입니다.
    /// Core Editor는 구현체의 구체 타입을 참조하지 않고 Unity <c>TypeCache</c>로 검색합니다.
    /// </summary>
    public interface IMapEditorExtension
    {
        /// <summary>
        /// 맵 배치툴에서 확장 UI와 수명주기를 실행할 순서를 반환합니다.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// 맵 배치툴이 활성화되었을 때 에디터 전용 캐시와 이벤트를 준비합니다.
        /// </summary>
        void OnEnable();

        /// <summary>
        /// 맵 배치툴이 비활성화되었을 때 등록한 에디터 이벤트와 캐시를 정리합니다.
        /// </summary>
        void OnDisable();

        /// <summary>
        /// 현재 맵 컨텍스트를 기준으로 확장 UI를 그립니다.
        /// </summary>
        /// <param name="context">현재 선택된 맵과 씬 맵 루트를 포함하는 컨텍스트입니다.</param>
        void OnGUI(MapEditorExtensionContext context);

        /// <summary>
        /// Core 맵 배치 데이터 로드가 끝난 뒤 외부 배치 데이터를 불러옵니다.
        /// </summary>
        /// <param name="context">로드된 맵 컨텍스트입니다.</param>
        void Load(MapEditorExtensionContext context);

        /// <summary>
        /// Core 맵 배치 데이터를 저장하는 흐름에서 외부 배치 데이터를 함께 저장합니다.
        /// </summary>
        /// <param name="context">저장할 맵 컨텍스트입니다.</param>
        /// <returns>외부 배치 데이터를 정상적으로 저장했으면 <see langword="true"/>를 반환합니다.</returns>
        bool Export(MapEditorExtensionContext context);
    }

    /// <summary>
    /// 외부 맵 배치 확장이 프로젝트 확장 탭에 표시될 그룹과 이름을 선택적으로 제공합니다.
    /// 기존 <see cref="IMapEditorExtension"/> 구현과의 호환성을 유지하기 위해 별도 인터페이스로 분리합니다.
    /// </summary>
    public interface IMapEditorExtensionPresentation
    {
        /// <summary>
        /// 프로젝트 확장 탭에서 확장 패널을 묶을 패키지 또는 게임 기능 그룹명입니다.
        /// </summary>
        string CategoryName { get; }

        /// <summary>
        /// 프로젝트 확장 탭의 패널 제목으로 표시할 사용자 친화적인 이름입니다.
        /// </summary>
        string DisplayName { get; }
    }

    /// <summary>
    /// 맵 배치툴 확장 구현에 현재 선택 맵과 씬 루트를 안전하게 전달하는 읽기 전용 컨텍스트입니다.
    /// </summary>
    public sealed class MapEditorExtensionContext
    {
        /// <summary>
        /// 맵 배치툴 확장 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="mapUid">현재 선택된 맵 UID입니다.</param>
        /// <param name="mapData">현재 선택된 맵 테이블 데이터입니다.</param>
        /// <param name="mapRoot">현재 씬에 로드된 맵 루트입니다.</param>
        public MapEditorExtensionContext(int mapUid, StruckTableMap mapData, MapTileCommon mapRoot)
        {
            MapUid = mapUid;
            MapData = mapData;
            MapRoot = mapRoot;
        }

        /// <summary>
        /// 현재 선택된 맵 UID입니다.
        /// </summary>
        public int MapUid { get; }

        /// <summary>
        /// 현재 선택된 맵 테이블 데이터입니다.
        /// </summary>
        public StruckTableMap MapData { get; }

        /// <summary>
        /// 현재 씬에 로드된 맵 루트입니다.
        /// 맵이 아직 로드되지 않았으면 null입니다.
        /// </summary>
        public MapTileCommon MapRoot { get; }
    }
}
