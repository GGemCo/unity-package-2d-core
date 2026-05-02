namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 JSON Addressables 키와 에셋 경로 규칙을 제공합니다.
    /// </summary>
    public static class ConfigAddressableWorldMap
    {
        /// <summary>기본 월드맵 그래프 ID입니다.</summary>
        public const string DefaultGraphId = "main";

        private const string KeyPrefix = ConfigDefine.NameSDK + "_WorldMap_";
        private const string BackgroundKeyPrefix = ConfigDefine.NameSDK + "_WorldMap_Background_";
        private const string IconKeyPrefix = ConfigDefine.NameSDK + "_WorldMap_Icon_";
        private const string InactiveSpriteKeyPrefix = ConfigDefine.NameSDK + "_WorldMap_InactiveSprite_";
        private const string DecorationSpriteKeyPrefix = ConfigDefine.NameSDK + "_WorldMap_DecorationSprite_";
        private const string DecorationAnimatorKeyPrefix = ConfigDefine.NameSDK + "_WorldMap_DecorationAnimator_";
        private const string EdgeSpriteKeyPrefix = ConfigDefine.NameSDK + "_WorldMap_Edge_";
        private const string FileNamePrefix = "world_map_";
        private const string ExtJson = ".json";

        /// <summary>
        /// 월드맵 JSON을 모아둘 프로젝트 상대 경로입니다.
        /// </summary>
        public static string Root => ConfigAddressablePath.Combine(ConfigAddressablePath.Root, "WorldMap");

        /// <summary>
        /// 월드맵 배경/아이콘 원본 이미지를 모아둘 프로젝트 상대 경로입니다.
        /// </summary>
        public static string ImageRoot => ConfigAddressablePath.Combine(Root, "Images");

        /// <summary>
        /// 월드맵 그래프 ID에 대응하는 Addressables 키를 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <returns>Addressables 키입니다.</returns>
        public static string GetKey(string graphId)
        {
            return KeyPrefix + NormalizeGraphId(graphId);
        }

        /// <summary>
        /// 기본 월드맵 JSON Addressables 키를 반환합니다.
        /// </summary>
        /// <returns>기본 월드맵 Addressables 키입니다.</returns>
        public static string GetDefaultKey()
        {
            return GetKey(DefaultGraphId);
        }

        /// <summary>
        /// 월드맵 배경 Sprite에 대응하는 Addressables 키를 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <returns>배경 Sprite Addressables 키입니다.</returns>
        public static string GetBackgroundKey(string graphId)
        {
            return BackgroundKeyPrefix + NormalizeGraphId(graphId);
        }

        /// <summary>
        /// 월드맵 노드 아이콘 Sprite에 대응하는 Addressables 키를 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <param name="nodeId">월드맵 노드 ID입니다.</param>
        /// <returns>노드 아이콘 Sprite Addressables 키입니다.</returns>
        public static string GetNodeIconKey(string graphId, string nodeId)
        {
            return IconKeyPrefix + NormalizeGraphId(graphId) + "_" + NormalizeNodeId(nodeId);
        }

        /// <summary>
        /// 월드맵 노드 비활성 Sprite에 사용할 Addressables 키를 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <param name="nodeId">월드맵 노드 ID입니다.</param>
        /// <returns>노드 비활성 Sprite Addressables 키입니다.</returns>
        public static string GetNodeInactiveSpriteKey(string graphId, string nodeId)
        {
            return InactiveSpriteKeyPrefix + NormalizeGraphId(graphId) + "_" + NormalizeNodeId(nodeId);
        }

        /// <summary>
        /// 월드맵 노드 데코레이션 Sprite에 사용할 Addressables 키를 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <param name="nodeId">월드맵 노드 ID입니다.</param>
        /// <returns>노드 데코레이션 Sprite Addressables 키입니다.</returns>
        public static string GetNodeDecorationSpriteKey(string graphId, string nodeId)
        {
            return DecorationSpriteKeyPrefix + NormalizeGraphId(graphId) + "_" + NormalizeNodeId(nodeId);
        }

        /// <summary>
        /// 월드맵 노드 데코레이션 AnimatorController에 사용할 Addressables 키를 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <param name="nodeId">월드맵 노드 ID입니다.</param>
        /// <returns>노드 데코레이션 AnimatorController Addressables 키입니다.</returns>
        public static string GetNodeDecorationAnimatorKey(string graphId, string nodeId)
        {
            return DecorationAnimatorKeyPrefix + NormalizeGraphId(graphId) + "_" + NormalizeNodeId(nodeId);
        }

        /// <summary>
        /// 월드맵 연결선 Sprite에 사용할 Addressables 키를 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <param name="edgeId">월드맵 연결선 ID입니다.</param>
        /// <returns>연결선 Sprite Addressables 키입니다.</returns>
        public static string GetEdgeSpriteKey(string graphId, string edgeId)
        {
            return EdgeSpriteKeyPrefix + NormalizeGraphId(graphId) + "_" + NormalizeEdgeId(edgeId);
        }

        /// <summary>
        /// 월드맵 그래프 ID에 대응하는 JSON 에셋 경로를 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <returns>프로젝트 상대 JSON 경로입니다.</returns>
        public static string GetAssetPath(string graphId)
        {
            return ConfigAddressablePath.Combine(Root, GetFileName(graphId));
        }

        /// <summary>
        /// 월드맵 그래프 ID에 대응하는 JSON 파일명을 만듭니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <returns>JSON 파일명입니다.</returns>
        public static string GetFileName(string graphId)
        {
            return FileNamePrefix + NormalizeGraphId(graphId) + ExtJson;
        }

        /// <summary>
        /// Addressables 키와 파일명에 안전한 그래프 ID로 정규화합니다.
        /// </summary>
        /// <param name="graphId">원본 그래프 ID입니다.</param>
        /// <returns>정규화된 그래프 ID입니다.</returns>
        public static string NormalizeGraphId(string graphId)
        {
            string normalized = ConfigAddressablePath.Normalize(graphId);
            return string.IsNullOrWhiteSpace(normalized) ? DefaultGraphId : normalized;
        }

        /// <summary>
        /// Addressables 키에 안전한 노드 ID로 정규화합니다.
        /// </summary>
        /// <param name="nodeId">원본 노드 ID입니다.</param>
        /// <returns>정규화된 노드 ID입니다.</returns>
        public static string NormalizeNodeId(string nodeId)
        {
            string normalized = ConfigAddressablePath.Normalize(nodeId);
            return string.IsNullOrWhiteSpace(normalized) ? "node" : normalized;
        }

        /// <summary>
        /// Addressables 키에 안전한 연결선 ID로 정규화합니다.
        /// </summary>
        /// <param name="edgeId">원본 연결선 ID입니다.</param>
        /// <returns>정규화된 연결선 ID입니다.</returns>
        public static string NormalizeEdgeId(string edgeId)
        {
            string normalized = ConfigAddressablePath.Normalize(edgeId);
            return string.IsNullOrWhiteSpace(normalized) ? "edge" : normalized;
        }
    }
}
