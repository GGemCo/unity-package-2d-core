using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저 인스턴스를 생성하는 전용 팩토리입니다.
    /// - 기존 ProjectileManager와 분리된 신규 시스템입니다.
    /// - 정적 데이터는 laser 테이블을 사용합니다.
    /// </summary>
    public sealed class LaserManager
    {
        private TableLoaderManager _table;

        /// <summary>
        /// 레이저 매니저를 초기화합니다.
        /// </summary>
        /// <param name="sceneGame">현재 SceneGame 인스턴스입니다.</param>
        public void Initialize(SceneGame sceneGame)
        {
            _table = TableLoaderManager.Instance;
        }

        /// <summary>
        /// 런타임 메타데이터를 사용해 레이저를 생성합니다.
        /// </summary>
        /// <param name="metadata">생성할 레이저의 런타임 메타데이터입니다.</param>
        /// <returns>생성된 레이저 인스턴스이며, 실패 시 null을 반환합니다.</returns>
        public LaserBeam CreateLaser(MetadataLaser metadata)
        {
            if (metadata == null)
            {
                GcLogger.LogError("[LaserManager] metadata is null.");
                return null;
            }

            if (_table == null)
            {
                GcLogger.LogError("[LaserManager] TableLoaderManager is not initialized.");
                return null;
            }

            StruckTableLaser info = _table.GetLaserData(metadata.LaserUid);
            if (info == null)
            {
                GcLogger.LogError($"[LaserManager] Unknown laser uid={metadata.LaserUid}");
                return null;
            }

            GameObject go = new GameObject($"Laser_{info.Uid}");
            LaserBeam comp = go.AddComponent<LaserBeam>();
            if (comp == null)
            {
                Object.Destroy(go);
                GcLogger.LogError("[LaserManager] Component add failed.");
                return null;
            }

            comp.Initialize(info, metadata);
            return comp;
        }
    }
}
