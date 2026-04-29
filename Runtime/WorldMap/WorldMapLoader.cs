using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables 또는 문자열 JSON에서 월드맵 정의를 로드합니다.
    /// </summary>
    public static class WorldMapLoader
    {
        /// <summary>
        /// 그래프 ID에 해당하는 Addressables 키로 월드맵 정의를 비동기 로드합니다.
        /// </summary>
        /// <param name="graphId">로드할 월드맵 그래프 ID입니다.</param>
        /// <returns>로드된 월드맵 정의입니다. 실패 시 null입니다.</returns>
        public static Task<WorldMapDefinition> LoadByGraphIdAsync(string graphId)
        {
            string key = ConfigAddressableWorldMap.GetKey(graphId);
            return LoadByKeyAsync(key);
        }

        /// <summary>
        /// 지정한 Addressables 키로 월드맵 JSON TextAsset을 로드한 뒤 정의 객체로 변환합니다.
        /// </summary>
        /// <param name="key">Addressables TextAsset 키입니다.</param>
        /// <returns>로드된 월드맵 정의입니다. 실패 시 null입니다.</returns>
        public static async Task<WorldMapDefinition> LoadByKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                GcLogger.LogError("월드맵 Addressables 키가 비어 있습니다.");
                return null;
            }

            try
            {
                TextAsset textAsset = await AddressableLoaderController.LoadByKeyAsync<TextAsset>(key);
                if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
                {
                    GcLogger.LogError("월드맵 JSON TextAsset을 로드하지 못했습니다. key: " + key);
                    return null;
                }

                string error;
                WorldMapDefinition definition = FromJson(textAsset.text, out error);
                if (definition == null)
                {
                    GcLogger.LogError(error);
                }

                return definition;
            }
            catch (Exception e)
            {
                GcLogger.LogError("월드맵 JSON 로드 중 오류가 발생했습니다. key: " + key + ", error: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// JSON 문자열을 월드맵 정의 객체로 변환합니다.
        /// </summary>
        /// <param name="json">월드맵 JSON 문자열입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>변환된 월드맵 정의입니다. 실패 시 null입니다.</returns>
        public static WorldMapDefinition FromJson(string json, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "월드맵 JSON 문자열이 비어 있습니다.";
                return null;
            }

            try
            {
                WorldMapGraphJson graphJson = JsonConvert.DeserializeObject<WorldMapGraphJson>(json);
                if (graphJson == null)
                {
                    error = "월드맵 JSON 파싱 결과가 비어 있습니다.";
                    return null;
                }

                return WorldMapDefinition.FromJson(graphJson);
            }
            catch (Exception e)
            {
                error = "월드맵 JSON 파싱에 실패했습니다. " + e.Message;
                return null;
            }
        }
    }
}
