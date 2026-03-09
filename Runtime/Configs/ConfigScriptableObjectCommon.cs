using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// ScriptableObject CreateAssetMenu 공통 규칙 정의
    /// </summary>
    public static class ConfigScriptableObjectCommon
    {
        /// <summary>
        /// 패키지 단위 정렬 우선순위
        /// 각 패키지는 1000 단위 블록을 사용한다.
        /// </summary>
        public enum PackageOrder
        {
            None = -10000,
            Core = 0,
            Control = 1000,
            Simulation = 2000,
            Affect = 3000,
            Skill = 4000,
            AiBt = 5000,
        }

        /// <summary>
        /// ScriptableObject 생성 메뉴 메타데이터
        /// </summary>
        public readonly struct MenuInfo
        {
            /// <summary>
            /// 생성될 에셋 기본 파일명
            /// </summary>
            public string FileName { get; }

            /// <summary>
            /// CreateAssetMenu 메뉴 경로
            /// </summary>
            public string MenuName { get; }

            /// <summary>
            /// CreateAssetMenu 정렬 순서
            /// </summary>
            public int Ordering { get; }

            /// <summary>
            /// 대상 ScriptableObject 타입
            /// </summary>
            public Type AssetType { get; }

            public MenuInfo(string fileName, string menuName, int ordering, Type assetType)
            {
                FileName = fileName;
                MenuName = menuName;
                Ordering = ordering;
                AssetType = assetType;
            }
        }

        /// <summary>
        /// 패키지 정렬 순서와 패키지 내부 순서를 조합하여 최종 메뉴 순서를 만든다.
        /// </summary>
        public static int GetMenuOrder(PackageOrder packageOrder, int localOrder)
        {
            return (int)packageOrder + localOrder;
        }
    }
}