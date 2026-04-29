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
        private const string FileNamePrefix = "world_map_";
        private const string ExtJson = ".json";

        /// <summary>
        /// 월드맵 JSON을 모아둘 프로젝트 상대 경로입니다.
        /// </summary>
        public static string Root => ConfigAddressablePath.Combine(ConfigAddressablePath.Root, "WorldMap");

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
    }
}
