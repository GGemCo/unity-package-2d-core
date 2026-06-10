using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 단위의 공중 상태를 공통으로 관리합니다.
    /// Ground Probe 기반 물리 판정과 Jump/CrowdControl/Lunge 같은 강제 공중 토큰을 합산해 최종 공중 상태를 제공합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAirborneStateController : MonoBehaviour
    {
        private readonly Dictionary<int, CharacterAirborneToken> _tokens = new();
        private CharacterBase _character;
        private Rigidbody2D _rigidbody2D;
        private int _nextTokenId;

        private struct CharacterAirborneToken
        {
            public CharacterAirborneSource Source;
            public string Reason;
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnDisable()
        {
            ClearAllForcedAirborne();
        }

        /// <summary>
        /// 강제 공중 상태를 등록하고, 이후 해제에 사용할 핸들을 반환합니다.
        /// </summary>
        /// <param name="source">공중 상태를 등록한 시스템 원인입니다.</param>
        /// <param name="reason">디버그 확인용 사유 문자열입니다.</param>
        /// <returns>등록된 공중 상태 핸들입니다.</returns>
        public CharacterAirborneHandle AcquireAirborne(CharacterAirborneSource source, string reason = null)
        {
            if (source == CharacterAirborneSource.None)
                return CharacterAirborneHandle.Invalid;

            if ((source & CharacterAirborneSource.PhysicsProbe) != 0)
                source &= ~CharacterAirborneSource.PhysicsProbe;

            if (source == CharacterAirborneSource.None)
                return CharacterAirborneHandle.Invalid;

            int id = ++_nextTokenId;
            _tokens[id] = new CharacterAirborneToken
            {
                Source = source,
                Reason = reason,
            };

            return new CharacterAirborneHandle(id, source);
        }

        /// <summary>
        /// 지정한 핸들로 등록된 강제 공중 상태를 해제합니다.
        /// </summary>
        /// <param name="handle">해제할 공중 상태 핸들입니다.</param>
        /// <returns>실제로 해제되었으면 <see langword="true"/>를 반환합니다.</returns>
        public bool ReleaseAirborne(CharacterAirborneHandle handle)
        {
            if (!handle.IsValid)
                return false;

            return _tokens.Remove(handle.Id);
        }

        /// <summary>
        /// 현재 등록된 모든 강제 공중 상태를 제거합니다.
        /// </summary>
        public void ClearAllForcedAirborne()
        {
            _tokens.Clear();
        }

        /// <summary>
        /// 특정 원인으로 등록된 강제 공중 상태가 있는지 확인합니다.
        /// </summary>
        /// <param name="source">확인할 공중 상태 원인입니다.</param>
        /// <returns>해당 원인이 하나 이상 활성화되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool HasForcedAirborne(CharacterAirborneSource source)
        {
            CharacterAirborneSource active = GetForcedAirborneSources();
            return (active & source) != 0;
        }

        /// <summary>
        /// 현재 캐릭터의 지상/공중 상태 스냅샷을 계산합니다.
        /// </summary>
        /// <param name="maxGroundDistance">지면 판정에 사용할 최대 거리입니다.</param>
        /// <returns>계산된 공중 상태 정보입니다.</returns>
        public CharacterAirborneInfo GetAirborneInfo(float maxGroundDistance = CharacterGroundProbeUtility.DefaultGroundedCheckDistance)
        {
            CacheReferences();

            bool hasGround = false;
            float distanceToGround = 0f;
            bool isGrounded = false;

            if (_character != null && _rigidbody2D != null)
            {
                hasGround = CharacterGroundProbeUtility.TryProbeGroundBelow(
                    _character,
                    _rigidbody2D,
                    maxGroundDistance,
                    out float groundY,
                    out float bottomY);

                if (hasGround)
                {
                    distanceToGround = bottomY - groundY;
                    isGrounded = distanceToGround >= -CharacterGroundProbeUtility.ProbeUpOffset && distanceToGround <= Mathf.Max(0f, maxGroundDistance);
                }
            }

            CharacterAirborneSource forcedSources = GetForcedAirborneSources();
            bool isForcedAirborne = forcedSources != CharacterAirborneSource.None;
            bool isPhysicallyAirborne = !isGrounded;
            CharacterAirborneSource source = forcedSources;
            if (isPhysicallyAirborne)
                source |= CharacterAirborneSource.PhysicsProbe;

            float verticalVelocity = _rigidbody2D != null ? _rigidbody2D.GetLinearVelocity().y : 0f;
            bool isAirborne = isPhysicallyAirborne || isForcedAirborne;

            return new CharacterAirborneInfo(
                isGrounded,
                isAirborne,
                isPhysicallyAirborne,
                isForcedAirborne,
                source,
                hasGround ? distanceToGround : 0f,
                verticalVelocity);
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<CharacterBase>();

            if (_rigidbody2D == null)
                _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private CharacterAirborneSource GetForcedAirborneSources()
        {
            CharacterAirborneSource sources = CharacterAirborneSource.None;
            foreach (CharacterAirborneToken token in _tokens.Values)
                sources |= token.Source;

            return sources;
        }
    }
}
