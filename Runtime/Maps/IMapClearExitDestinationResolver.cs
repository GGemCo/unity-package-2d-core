using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 클리어 종료 연출 후 표시할 UIWindow 대상을 나타냅니다.
    /// Core가 상위 패키지의 전용 윈도우 타입을 알지 않도록 Window 테이블 UID만 보관합니다.
    /// </summary>
    public readonly struct MapClearExitDestination
    {
        /// <summary>
        /// 표시할 Window 테이블 UID입니다.
        /// </summary>
        public int WindowUid { get; }

        /// <summary>
        /// 맵 클리어 종료 화면 정보를 생성합니다.
        /// </summary>
        /// <param name="windowUid">표시할 Window 테이블 UID입니다.</param>
        public MapClearExitDestination(int windowUid)
        {
            WindowUid = windowUid;
        }

        /// <summary>
        /// UIWindowManager에 전달할 수 있는 유효한 대상인지 확인합니다.
        /// </summary>
        /// <returns>Window 테이블 UID가 0보다 크면 <see langword="true"/>입니다.</returns>
        public bool IsValid()
        {
            return WindowUid > 0;
        }
    }

    /// <summary>
    /// 외부 패키지가 특정 맵의 클리어 종료 화면을 결정할 때 구현하는 정책 포트입니다.
    /// </summary>
    public interface IMapClearExitDestinationResolver
    {
        /// <summary>
        /// 지정한 맵에서 사용할 클리어 종료 화면을 조회합니다.
        /// </summary>
        /// <param name="mapUid">클리어한 맵 UID입니다.</param>
        /// <param name="destination">조회된 종료 화면 정보입니다.</param>
        /// <returns>현재 정책이 대상을 결정했으면 <see langword="true"/>입니다.</returns>
        bool TryResolve(int mapUid, out MapClearExitDestination destination);
    }

    /// <summary>
    /// 외부 패키지가 제공하는 맵 클리어 종료 화면 정책을 관리합니다.
    /// </summary>
    public static class MapClearExitDestinationResolverRegistry
    {
        private static readonly List<IMapClearExitDestinationResolver> Resolvers =
            new List<IMapClearExitDestinationResolver>();

        /// <summary>
        /// 맵 클리어 종료 화면 정책을 등록합니다.
        /// </summary>
        /// <param name="resolver">등록할 정책입니다.</param>
        public static void Register(IMapClearExitDestinationResolver resolver)
        {
            if (resolver == null || Resolvers.Contains(resolver))
            {
                return;
            }

            Resolvers.Add(resolver);
        }

        /// <summary>
        /// 맵 클리어 종료 화면 정책 등록을 해제합니다.
        /// </summary>
        /// <param name="resolver">등록 해제할 정책입니다.</param>
        public static void Unregister(IMapClearExitDestinationResolver resolver)
        {
            if (resolver == null)
            {
                return;
            }

            Resolvers.Remove(resolver);
        }

        /// <summary>
        /// 등록된 정책에서 지정한 맵의 종료 화면을 조회합니다.
        /// 나중에 등록한 상위 계층 정책이 기존 정책을 재정의할 수 있도록 역순으로 탐색합니다.
        /// </summary>
        /// <param name="mapUid">클리어한 맵 UID입니다.</param>
        /// <param name="destination">조회된 종료 화면 정보입니다.</param>
        /// <returns>유효한 종료 화면을 찾았으면 <see langword="true"/>입니다.</returns>
        public static bool TryResolve(
            int mapUid,
            out MapClearExitDestination destination)
        {
            for (int i = Resolvers.Count - 1; i >= 0; i--)
            {
                try
                {
                    IMapClearExitDestinationResolver resolver = Resolvers[i];
                    if (resolver != null &&
                        resolver.TryResolve(mapUid, out destination) &&
                        destination.IsValid())
                    {
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    GcLogger.LogException(exception);
                }
            }

            destination = default;
            return false;
        }
    }
}
