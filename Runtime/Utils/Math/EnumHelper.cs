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
        /// <summary>
        /// 문자열을 enum 값으로 변환합니다.
        /// - 대소문자 무시 (case-insensitive)
        /// - 앞뒤 공백 제거
        /// - "None" / "NONE" 처리
        /// 변환 실패 시 기본값(default(TEnum))을 반환합니다.
        /// </summary>
        public static TEnum ConvertEnum<TEnum>(string value) where TEnum : struct, Enum
        {
            // 1) Null / 공백 처리
            if (string.IsNullOrWhiteSpace(value))
            {
                // Enum 내부에 None 이 있으면 반환
                if (Enum.TryParse("None", true, out TEnum resultNone))
                    return resultNone;

                GcLogger.LogError($"[EnumConverter] Empty value for enum {typeof(TEnum).Name}");
                return default;
            }

            // 2) 전처리: 공백 제거 + 대문자 정규화
            value = value.Trim();

            // 3) Enum.TryParse (대소문자 무시)
            if (Enum.TryParse(value, true, out TEnum result))
                return result;

            // 4) Enum 이름 전체를 소문자로 비교 (예: "atk_fire" → "Atk_Fire")
            foreach (var name in Enum.GetNames(typeof(TEnum)))
            {
                if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                    return (TEnum)Enum.Parse(typeof(TEnum), name);
            }

            // 5) 실패 시 로그
            GcLogger.LogError($"[EnumConverter] Unknown value '{value}' for enum {typeof(TEnum).Name}");
            return default;
        }

    }
    public static class EnumCache<T> where T : Enum
    {
        public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
    }
}