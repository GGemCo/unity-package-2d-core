using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 사운드 리소스를 함께 유지할 사용 범위를 식별하는 값입니다.
    /// </summary>
    public readonly struct SoundUsageScopeKey : IEquatable<SoundUsageScopeKey>
    {
        private const string GlobalPrefix = "Global";
        private const string MapPrefix = "Map";
        private const string UiWindowPrefix = "UIWindow";

        private readonly string _value;

        /// <summary>
        /// 정규화된 범위 식별자입니다.
        /// </summary>
        public string Value => _value ?? string.Empty;

        /// <summary>
        /// 사용할 수 있는 범위 식별자인지 여부입니다.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        /// <summary>
        /// 지정한 문자열로 사운드 사용 범위 키를 생성합니다.
        /// </summary>
        /// <param name="value">범위를 구분할 고유 문자열입니다.</param>
        public SoundUsageScopeKey(string value)
        {
            _value = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 게임 전체에서 유지할 전역 사운드 범위 키를 생성합니다.
        /// </summary>
        /// <param name="id">전역 범위를 구분할 식별자입니다.</param>
        /// <returns>전역 사운드 범위 키입니다.</returns>
        public static SoundUsageScopeKey Global(string id)
        {
            return new SoundUsageScopeKey(BuildValue(GlobalPrefix, id));
        }

        /// <summary>
        /// 지정한 맵에서 유지할 사운드 범위 키를 생성합니다.
        /// </summary>
        /// <param name="mapUid">맵 UID입니다.</param>
        /// <returns>맵 사운드 범위 키입니다.</returns>
        public static SoundUsageScopeKey Map(int mapUid)
        {
            return mapUid > 0
                ? new SoundUsageScopeKey($"{MapPrefix}.{mapUid}")
                : default;
        }

        /// <summary>
        /// 지정한 UI 윈도우가 활성화된 동안 유지할 사운드 범위 키를 생성합니다.
        /// </summary>
        /// <param name="windowId">UI 윈도우 식별자입니다.</param>
        /// <returns>UI 윈도우 사운드 범위 키입니다.</returns>
        public static SoundUsageScopeKey UiWindow(string windowId)
        {
            return new SoundUsageScopeKey(BuildValue(UiWindowPrefix, windowId));
        }

        /// <summary>
        /// 두 범위 키가 같은 식별자를 나타내는지 확인합니다.
        /// </summary>
        /// <param name="other">비교할 범위 키입니다.</param>
        /// <returns>대소문자를 구분하지 않고 같으면 true를 반환합니다.</returns>
        public bool Equals(SoundUsageScopeKey other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SoundUsageScopeKey other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// 두 사운드 범위 키가 같은지 비교합니다.
        /// </summary>
        public static bool operator ==(SoundUsageScopeKey left, SoundUsageScopeKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 두 사운드 범위 키가 다른지 비교합니다.
        /// </summary>
        public static bool operator !=(SoundUsageScopeKey left, SoundUsageScopeKey right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// 접두사와 하위 식별자를 결합하여 범위 값을 생성합니다.
        /// </summary>
        /// <param name="prefix">범위 종류 접두사입니다.</param>
        /// <param name="id">하위 범위 식별자입니다.</param>
        /// <returns>결합된 범위 값입니다.</returns>
        private static string BuildValue(string prefix, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            return $"{prefix}.{id.Trim()}";
        }
    }
}
