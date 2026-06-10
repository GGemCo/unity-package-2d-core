using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공중 상태 등록을 해제할 때 사용하는 핸들입니다.
    /// 등록한 시스템은 이 값을 보관했다가 자신이 등록한 공중 상태만 해제해야 합니다.
    /// </summary>
    public readonly struct CharacterAirborneHandle : IEquatable<CharacterAirborneHandle>
    {
        /// <summary>
        /// 유효하지 않은 공중 상태 핸들입니다.
        /// </summary>
        public static readonly CharacterAirborneHandle Invalid = new(0, CharacterAirborneSource.None);

        /// <summary>
        /// 내부 등록 식별자입니다.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 등록 당시의 공중 상태 원인입니다.
        /// </summary>
        public CharacterAirborneSource Source { get; }

        /// <summary>
        /// 현재 핸들이 유효한지 여부입니다.
        /// </summary>
        public bool IsValid => Id > 0 && Source != CharacterAirborneSource.None;

        /// <summary>
        /// 공중 상태 핸들을 생성합니다.
        /// </summary>
        /// <param name="id">내부 등록 식별자입니다.</param>
        /// <param name="source">공중 상태 원인입니다.</param>
        public CharacterAirborneHandle(int id, CharacterAirborneSource source)
        {
            Id = id;
            Source = source;
        }

        /// <inheritdoc />
        public bool Equals(CharacterAirborneHandle other)
        {
            return Id == other.Id && Source == other.Source;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is CharacterAirborneHandle other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (int)Source;
            }
        }
    }
}
