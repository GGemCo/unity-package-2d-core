using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// 패키지 간 저장 데이터 교환용 봉투(Envelope).
    /// 각 패키지는 섹션 키로 자신의 DTO를 JToken으로 넣고/꺼낸다.
    /// </summary>
    public sealed class SaveEnvelope
    {
        public readonly Dictionary<string, JToken> Sections = new();

        public void SetSection<T>(string key, T dto) => Sections[key] = JToken.FromObject(dto);
        public bool TryGetSection<T>(string key, out T dto)
        {
            if (Sections.TryGetValue(key, out var token))
            {
                dto = token.ToObject<T>();
                return true;
            }
            dto = default;
            return false;
        }
    }
}