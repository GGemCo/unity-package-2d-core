using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 물리 오버라이드(현재는 gravityScale)를 중앙에서 관리합니다.
    /// 각 시스템은 이전 값을 직접 저장/복구하지 않고, 핸들을 획득/해제하는 방식으로만 접근합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterPhysicsOverrideController : MonoBehaviour
    {
        private struct GravityOverrideRequest
        {
            public int Id;
            public object OwnerKey;
            public Object LifecycleOwner;
            public bool HasLifecycleOwner;
            public CharacterPhysicsOverrideChannel Channel;
            public int Priority;
            public float GravityScale;
            public long Sequence;
            public string Reason;
        }

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;

        private readonly List<GravityOverrideRequest> _gravityRequests = new();

        private int _nextRequestId = 1;
        private long _nextSequence = 1;
        private bool _hasBaseGravityScale;
        private float _baseGravityScale;

        public float BaseGravityScale => _baseGravityScale;
        public float CurrentGravityScale => GetTargetGravityScale();
        public int ActiveRequestCount => _gravityRequests.Count;

        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            CaptureBaseGravityScale(force: false);
            ReconcileGravityScale();
        }

        private void FixedUpdate()
        {
            CleanupDestroyedOwners();
            ReconcileGravityScale();
        }

        private void OnDisable()
        {
            ForceRestoreBaseGravity();
        }

        public void CaptureBaseGravityScale(bool force = true)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
                if (rb == null)
                    return;
            }

            if (_hasBaseGravityScale && !force)
                return;

            _baseGravityScale = rb.gravityScale;
            _hasBaseGravityScale = true;
        }

        public CharacterPhysicsOverrideHandle AcquireGravityOverride(
            object ownerKey,
            Object lifecycleOwner,
            CharacterPhysicsOverrideChannel channel,
            int priority,
            float gravityScale,
            string reason = null)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (rb == null)
                return default;

            CleanupDestroyedOwners();

            var request = new GravityOverrideRequest
            {
                Id = _nextRequestId++,
                OwnerKey = ownerKey,
                LifecycleOwner = lifecycleOwner,
                HasLifecycleOwner = !ReferenceEquals(lifecycleOwner, null),
                Channel = channel,
                Priority = priority,
                GravityScale = gravityScale,
                Sequence = _nextSequence++,
                Reason = reason,
            };

            _gravityRequests.Add(request);
            ReconcileGravityScale();
            return new CharacterPhysicsOverrideHandle(request.Id);
        }

        public void ReleaseGravityOverride(CharacterPhysicsOverrideHandle handle)
        {
            if (!handle.IsValid)
                return;

            for (int i = _gravityRequests.Count - 1; i >= 0; i--)
            {
                if (_gravityRequests[i].Id != handle.Id)
                    continue;

                _gravityRequests.RemoveAt(i);
                ReconcileGravityScale();
                return;
            }
        }

        public void ReleaseGravityOverride(ref CharacterPhysicsOverrideHandle handle)
        {
            ReleaseGravityOverride(handle);
            handle = default;
        }

        public void ReleaseAllByOwner(object ownerKey)
        {
            if (ownerKey == null)
                return;

            bool removedAny = false;
            for (int i = _gravityRequests.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_gravityRequests[i].OwnerKey, ownerKey))
                    continue;

                _gravityRequests.RemoveAt(i);
                removedAny = true;
            }

            if (removedAny)
            {
                ReconcileGravityScale();
            }
        }

        public void ForceRestoreBaseGravity()
        {
            _gravityRequests.Clear();

            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (rb == null)
                return;

            CaptureBaseGravityScale(force: false);
            if (!Mathf.Approximately(rb.gravityScale, _baseGravityScale))
            {
                rb.gravityScale = _baseGravityScale;
            }
        }

        private void CleanupDestroyedOwners()
        {
            if (_gravityRequests.Count == 0)
                return;

            bool removedAny = false;
            for (int i = _gravityRequests.Count - 1; i >= 0; i--)
            {
                var request = _gravityRequests[i];
                if (!request.HasLifecycleOwner)
                    continue;

                if (request.LifecycleOwner != null)
                    continue;

                _gravityRequests.RemoveAt(i);
                removedAny = true;
            }

            if (removedAny)
            {
                ReconcileGravityScale();
            }
        }

        private float GetTargetGravityScale()
        {
            CaptureBaseGravityScale(force: false);
            int winnerIndex = GetWinningRequestIndex();
            return winnerIndex >= 0 ? _gravityRequests[winnerIndex].GravityScale : _baseGravityScale;
        }

        private int GetWinningRequestIndex()
        {
            if (_gravityRequests.Count == 0)
                return -1;

            int winnerIndex = 0;
            var winner = _gravityRequests[0];

            for (int i = 1; i < _gravityRequests.Count; i++)
            {
                var candidate = _gravityRequests[i];
                if (candidate.Priority > winner.Priority)
                {
                    winner = candidate;
                    winnerIndex = i;
                    continue;
                }

                if (candidate.Priority == winner.Priority && candidate.Sequence > winner.Sequence)
                {
                    winner = candidate;
                    winnerIndex = i;
                }
            }

            return winnerIndex;
        }

        private void ReconcileGravityScale()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (rb == null)
                return;

            float targetGravityScale = GetTargetGravityScale();
            if (Mathf.Approximately(rb.gravityScale, targetGravityScale))
                return;

            rb.gravityScale = targetGravityScale;
        }
    }
}
