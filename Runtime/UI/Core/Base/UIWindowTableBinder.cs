using System.Collections.Generic;
using System.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// TableWindow 데이터를 UIWindow 참조에 연결하는 책임을 담당합니다.
    /// </summary>
    internal sealed class UIWindowTableBinder
    {
        private readonly UIWindowRegistry _registry;
        private readonly Dictionary<int, StruckTableWindow> _tableWindows = new Dictionary<int, StruckTableWindow>();

        /// <summary>
        /// TableWindow 데이터와 UIWindow 참조를 연결할 수 있도록 바인더를 생성합니다.
        /// </summary>
        /// <param name="registry">UID 기준 UIWindow 참조 캐시입니다.</param>
        public UIWindowTableBinder(UIWindowRegistry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// 현재 게임에서 사용 중인 UIWindow 테이블 정보 목록을 반환합니다.
        /// </summary>
        public IReadOnlyDictionary<int, StruckTableWindow> TableWindows => _tableWindows;

        /// <summary>
        /// 현재 게임에서 사용 중인 UIWindow UID 목록을 반환합니다.
        /// </summary>
        public IEnumerable<int> WindowUids => _tableWindows.Keys;

        /// <summary>
        /// 보관 중인 TableWindow 바인딩 정보를 모두 제거합니다.
        /// </summary>
        public void Clear()
        {
            _tableWindows.Clear();
        }

        /// <summary>
        /// TableWindow 데이터를 읽어 UIWindow 참조에 연결하고 사용 가능한 윈도우 정보를 캐시합니다.
        /// </summary>
        /// <param name="windowKeys">Inspector에서 연결한 UID별 UIWindow 목록입니다.</param>
        public void Initialize(List<WindowKey> windowKeys)
        {
            Clear();

            if (TableLoaderManager.Instance == null)
            {
                return;
            }

            _registry.Rebuild(windowKeys);
            if (_registry.Count <= 0)
            {
                return;
            }

            TableWindow tableWindow = TableLoaderManager.Instance.TableWindow;
            if (tableWindow == null)
            {
                return;
            }

            Dictionary<int, StruckTableWindow> tables = tableWindow.GetDatas();
            if (tables == null)
            {
                return;
            }

            foreach (KeyValuePair<int, StruckTableWindow> table in tables.OrderBy(kv => kv.Value.Ordering))
            {
                int uid = table.Key;
                if (uid == 0)
                {
                    continue;
                }

                StruckTableWindow info = tableWindow.GetDataByUid(uid);
                if (info == null || info.Uid <= 0)
                {
                    continue;
                }

                UIWindow window = _registry.GetWindowReferenceByUid(uid);
                if (window == null)
                {
                    continue;
                }

                if (!info.UseInGame)
                {
                    window.gameObject.SetActive(false);
                    continue;
                }

                window.SetTableWindow(info);
                _tableWindows[info.Uid] = info;
            }
        }

        /// <summary>
        /// 지정한 UID에 해당하는 TableWindow 정보를 조회합니다.
        /// </summary>
        /// <param name="uid">조회할 UIWindow UID입니다.</param>
        /// <param name="info">조회된 TableWindow 정보입니다.</param>
        /// <returns>사용 가능한 TableWindow 정보가 있으면 true입니다.</returns>
        public bool TryGetWindowInfo(int uid, out StruckTableWindow info)
        {
            return _tableWindows.TryGetValue(uid, out info);
        }
    }
}
