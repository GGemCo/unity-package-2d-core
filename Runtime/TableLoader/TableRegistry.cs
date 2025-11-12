using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class TableRegistry
    {
        private readonly Dictionary<string, ITableParser> _map = new();

        public void Register(ITableParser parser)
        {
            if (parser == null || string.IsNullOrEmpty(parser.Key))
            {
                GcLogger.LogWarning("[TableRegistry] Invalid parser registration.");
                return;
            }
            _map[parser.Key] = parser;
        }
        
        public bool TryLoad(string key, string content)
        {
            if (!_map.TryGetValue(key, out var parser)) return false;
            parser.LoadData(content); // void 메서드 호출
            return true;
        }
    }
}