using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 부모-자식 관계를 사용하지 않고 대상 Transform의 월드 위치를 추적합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TransformPositionFollower : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _worldOffset;

        public void Bind(Transform target, Vector3 worldOffset)
        {
            _target = target;
            _worldOffset = worldOffset;
            SyncNow();
        }

        public void Clear()
        {
            _target = null;
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            SyncNow();
        }

        private void SyncNow()
        {
            if (_target == null)
                return;

            transform.position = _target.position + _worldOffset;
        }
    }
}
