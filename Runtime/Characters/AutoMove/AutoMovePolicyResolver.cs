namespace GGemCo2DCore
{
    /// <summary>
    /// 전역 자동 이동 설정과 현재 맵의 자동 이동 정책을 조합하여 최종 사용 가능 여부를 계산합니다.
    /// </summary>
    public static class AutoMovePolicyResolver
    {
        /// <summary>
        /// 현재 게임 상태를 기준으로 자동 이동을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>자동 이동을 사용할 수 있으면 true를 반환합니다.</returns>
        public static bool IsAutoMoveEnabled()
        {
            var settings = AddressableLoaderSettings.Instance
                ? AddressableLoaderSettings.Instance.settings
                : null;

            if (settings == null)
            {
                return false;
            }

            MapAutoMovePolicy mapPolicy = ResolveCurrentMapPolicy();
            return IsAutoMoveEnabled(settings.enableAutoMove, mapPolicy);
        }

        /// <summary>
        /// 전역 설정 값과 맵 정책을 조합하여 자동 이동 사용 여부를 계산합니다.
        /// </summary>
        /// <param name="globalEnabled">전역 자동 이동 사용 여부입니다.</param>
        /// <param name="mapPolicy">현재 맵의 자동 이동 정책입니다.</param>
        /// <returns>최종적으로 자동 이동을 사용할 수 있으면 true를 반환합니다.</returns>
        public static bool IsAutoMoveEnabled(bool globalEnabled, MapAutoMovePolicy mapPolicy)
        {
            switch (mapPolicy)
            {
                case MapAutoMovePolicy.Enabled:
                    return true;

                case MapAutoMovePolicy.Disabled:
                    return false;

                case MapAutoMovePolicy.Inherit:
                default:
                    return globalEnabled;
            }
        }

        /// <summary>
        /// 현재 로드 중이거나 로드된 맵의 자동 이동 정책을 반환합니다.
        /// 맵 정보를 아직 알 수 없으면 전역 설정을 따르도록 <see cref="MapAutoMovePolicy.Inherit"/>를 반환합니다.
        /// </summary>
        /// <returns>현재 맵의 자동 이동 정책입니다.</returns>
        private static MapAutoMovePolicy ResolveCurrentMapPolicy()
        {
            StruckTableMap mapData = SceneGame.Instance?.mapManager?.GetCurrentMapTableData();
            return mapData?.AutoMovePolicy ?? MapAutoMovePolicy.Inherit;
        }
    }
}
