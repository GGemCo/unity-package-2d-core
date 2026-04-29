using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 에디터에서 편집하는 월드맵 노드 원본 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapNodeData
    {
        /// <summary>그래프 내부에서 사용하는 노드 고유 ID입니다.</summary>
        public string nodeId;

        /// <summary>TableMap을 참조하는 맵 UID입니다.</summary>
        public int mapUid;

        /// <summary>배경 기준 0~1 범위의 정규화 좌표입니다.</summary>
        public Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);

        /// <summary>테이블 이름 대신 표시할 선택 제목입니다.</summary>
        public string titleOverride;

        /// <summary>런타임에서 아이콘을 로드할 Addressables 키 또는 경로입니다.</summary>
        public string iconAddress;

        /// <summary>노드의 플레이 성격입니다.</summary>
        public WorldMapNodeType nodeType = WorldMapNodeType.Normal;

        /// <summary>처음부터 월드맵에 표시할지 여부입니다.</summary>
        public bool visibleByDefault = true;

        /// <summary>노드 표시/진입 조건을 판정할 조건 키입니다.</summary>
        public string unlockConditionKey;
    }

    /// <summary>
    /// 에디터에서 편집하는 월드맵 연결선 원본 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapEdgeData
    {
        /// <summary>그래프 내부에서 사용하는 연결선 고유 ID입니다.</summary>
        public string edgeId;

        /// <summary>출발 노드 ID입니다.</summary>
        public string fromNodeId;

        /// <summary>도착 노드 ID입니다.</summary>
        public string toNodeId;

        /// <summary>양방향 이동을 허용할지 여부입니다.</summary>
        public bool bidirectional;

        /// <summary>연결선의 플레이 성격입니다.</summary>
        public WorldMapEdgeType edgeType = WorldMapEdgeType.Normal;

        /// <summary>연결선 해금 조건을 판정할 조건 키입니다.</summary>
        public string unlockConditionKey;
    }

    /// <summary>
    /// JSON으로 export되는 월드맵 루트 DTO입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapGraphJson
    {
        /// <summary>JSON 스키마 버전입니다.</summary>
        public int version;

        /// <summary>월드맵 그래프 ID입니다.</summary>
        public string graphId;

        /// <summary>시작 노드 ID입니다.</summary>
        public string startNodeId;

        /// <summary>배경 이미지와 기준 해상도 정보입니다.</summary>
        public WorldMapBackgroundJson background;

        /// <summary>월드맵 노드 목록입니다.</summary>
        public List<WorldMapNodeJson> nodes = new List<WorldMapNodeJson>();

        /// <summary>월드맵 연결선 목록입니다.</summary>
        public List<WorldMapEdgeJson> edges = new List<WorldMapEdgeJson>();
    }

    /// <summary>
    /// JSON으로 export되는 월드맵 배경 정보입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapBackgroundJson
    {
        /// <summary>배경 이미지 Addressables 키 또는 경로입니다.</summary>
        public string address;

        /// <summary>편집 기준 해상도입니다.</summary>
        public WorldMapVector2Json referenceResolution;
    }

    /// <summary>
    /// JSON 직렬화용 2D 벡터입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapVector2Json
    {
        /// <summary>X 좌표 또는 너비 값입니다.</summary>
        public float x;

        /// <summary>Y 좌표 또는 높이 값입니다.</summary>
        public float y;

        /// <summary>
        /// 기본 생성자입니다.
        /// </summary>
        public WorldMapVector2Json()
        {
        }

        /// <summary>
        /// Unity Vector2 값을 JSON DTO로 변환합니다.
        /// </summary>
        /// <param name="value">변환할 Vector2 값입니다.</param>
        public WorldMapVector2Json(Vector2 value)
        {
            x = value.x;
            y = value.y;
        }

        /// <summary>
        /// JSON DTO 값을 Unity Vector2로 변환합니다.
        /// </summary>
        /// <returns>변환된 Vector2 값입니다.</returns>
        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// JSON으로 export되는 월드맵 노드 DTO입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapNodeJson
    {
        /// <summary>그래프 내부 노드 ID입니다.</summary>
        public string nodeId;

        /// <summary>TableMap UID입니다.</summary>
        public int mapUid;

        /// <summary>0~1 범위의 정규화 좌표입니다.</summary>
        public WorldMapVector2Json position;

        /// <summary>표시 제목 override입니다.</summary>
        public string titleOverride;

        /// <summary>아이콘 Addressables 키 또는 경로입니다.</summary>
        public string iconAddress;

        /// <summary>노드 타입 문자열입니다.</summary>
        public string nodeType;

        /// <summary>기본 표시 여부입니다.</summary>
        public bool visibleByDefault;

        /// <summary>해금 조건 키입니다.</summary>
        public string unlockConditionKey;
    }

    /// <summary>
    /// JSON으로 export되는 월드맵 연결선 DTO입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapEdgeJson
    {
        /// <summary>그래프 내부 연결선 ID입니다.</summary>
        public string edgeId;

        /// <summary>출발 노드 ID입니다.</summary>
        public string fromNodeId;

        /// <summary>도착 노드 ID입니다.</summary>
        public string toNodeId;

        /// <summary>양방향 연결 여부입니다.</summary>
        public bool bidirectional;

        /// <summary>연결선 타입 문자열입니다.</summary>
        public string edgeType;

        /// <summary>해금 조건 키입니다.</summary>
        public string unlockConditionKey;
    }
}
