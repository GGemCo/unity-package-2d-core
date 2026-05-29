using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 에디터 전용 빌드 모드 Override Provider를 런타임 공용 게이트에 연결하는 레지스트리입니다.
    /// </summary>
    public static class BuildModeOverrideRegistry
    {
        private static readonly object LockObject = new object();
        private static IBuildModeOverrideProvider _provider;

        /// <summary>
        /// 현재 등록된 빌드 모드 공급자가 있는지 반환합니다.
        /// </summary>
        public static bool HasProvider
        {
            get
            {
                lock (LockObject)
                {
                    return _provider != null;
                }
            }
        }

        /// <summary>
        /// 빌드 모드 공급자를 등록합니다.
        /// </summary>
        /// <param name="provider">등록할 공급자입니다. null이면 기존 공급자를 제거합니다.</param>
        public static void SetProvider(IBuildModeOverrideProvider provider)
        {
            lock (LockObject)
            {
                _provider = provider;
            }
        }

        /// <summary>
        /// 현재 등록된 빌드 모드 공급자를 제거합니다.
        /// </summary>
        public static void ClearProvider()
        {
            lock (LockObject)
            {
                _provider = null;
            }
        }

        /// <summary>
        /// 등록된 공급자에서 현재 빌드 모드를 조회합니다.
        /// </summary>
        /// <param name="mode">현재 선택된 빌드 모드입니다.</param>
        /// <returns>공급자에서 빌드 모드를 가져왔으면 true입니다.</returns>
        public static bool TryGetMode(out GGemCoBuildMode mode)
        {
            IBuildModeOverrideProvider provider;
            lock (LockObject)
            {
                provider = _provider;
            }

            mode = GGemCoBuildMode.Development;
            if (provider == null)
                return false;

            try
            {
                return provider.TryGetMode(out mode);
            }
            catch (Exception ex)
            {
                mode = GGemCoBuildMode.Development;
                GcLogger.LogError($"빌드 모드 조회 중 오류가 발생했습니다. error={ex.Message}");
                return false;
            }
        }
    }
}
