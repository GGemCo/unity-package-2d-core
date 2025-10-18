using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class EnumHelper
    {
        /// <summary>
        /// 지정한 enum 값에서 설정된 플래그 항목을 반환합니다.
        /// </summary>
        /// <typeparam name="T">Enum 형식 (예: TileRole, LayerMask 등)</typeparam>
        /// <param name="mask">비트 플래그 enum 값</param>
        /// <returns>설정된 플래그 항목의 IEnumerable</returns>
        public static IEnumerable<T> Flags<T>(T mask) where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                // 0 (None) 값은 제외
                if (Convert.ToInt64(value) == 0)
                    continue;

                long maskValue = Convert.ToInt64(mask);
                long flagValue = Convert.ToInt64(value);

                if ((maskValue & flagValue) == flagValue)
                    yield return value;
            }
        }
        
        /// <summary>
        /// 플래그 Enum에 상태를 추가합니다.
        /// </summary>
        public static T AddFlag<T>(this T current, T flag) where T : Enum
        {
            long cur = Convert.ToInt64(current);
            long add = Convert.ToInt64(flag);
            return (T)Enum.ToObject(typeof(T), cur | add);
        }

        /// <summary>
        /// 플래그 Enum에서 상태를 제거합니다.
        /// </summary>
        public static T RemoveFlag<T>(this T current, T flag) where T : Enum
        {
            long cur = Convert.ToInt64(current);
            long remove = Convert.ToInt64(flag);
            return (T)Enum.ToObject(typeof(T), cur & ~remove);
        }

        /// <summary>
        /// 지정한 상태가 포함되어 있는지 확인합니다.
        /// </summary>
        public static bool HasFlagFast<T>(this T current, T flag) where T : Enum
        {
            long cur = Convert.ToInt64(current);
            long check = Convert.ToInt64(flag);
            return (cur & check) == check;
        }

        /// <summary>
        /// 지정한 상태를 토글합니다. (있으면 제거, 없으면 추가)
        /// </summary>
        public static T ToggleFlag<T>(this T current, T flag) where T : Enum
        {
            return current.HasFlagFast(flag)
                ? current.RemoveFlag(flag)
                : current.AddFlag(flag);
        }
        /// <summary>
        /// 플래그 Enum 값을 None(0)으로 초기화합니다.
        /// </summary>
        public static T ClearFlags<T>(this T current) where T : Enum
        {
            return (T)Enum.ToObject(typeof(T), 0);
        }
    }
}