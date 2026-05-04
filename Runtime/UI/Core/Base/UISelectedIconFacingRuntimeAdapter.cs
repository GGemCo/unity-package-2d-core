using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공용 선택 이미지 오브젝트의 좌우 방향을 런타임에서 반전하기 위한 보조 컴포넌트입니다.
    /// 월드맵처럼 선택 이미지 방향을 아이콘 위치에 따라 바꿔야 할 때 사용합니다.
    /// </summary>
    public sealed class UISelectedIconFacingRuntimeAdapter : MonoBehaviour
    {
        private Vector3 _defaultLocalScale;
        private bool _isCached;

        /// <summary>
        /// 현재 오브젝트의 기본 로컬 스케일을 캐시합니다.
        /// 프리팹 원본 스케일을 기준으로 좌우 반전만 적용하기 위해 사용합니다.
        /// </summary>
        private void Awake()
        {
            CacheDefaultLocalScale();
        }

        /// <summary>
        /// 선택 이미지가 왼쪽을 바라보도록 로컬 스케일 X 부호를 적용합니다.
        /// Y, Z 값은 프리팹의 기본 스케일을 유지합니다.
        /// </summary>
        /// <param name="faceLeft">왼쪽을 바라보면 true입니다.</param>
        public void SetFaceLeft(bool faceLeft)
        {
            CacheDefaultLocalScale();

            float absX = Mathf.Abs(_defaultLocalScale.x);
            transform.localScale = new Vector3(
                faceLeft ? -absX : absX,
                _defaultLocalScale.y,
                _defaultLocalScale.z);
        }

        /// <summary>
        /// 프리팹 원본 로컬 스케일을 한 번만 캐시합니다.
        /// </summary>
        private void CacheDefaultLocalScale()
        {
            if (_isCached)
            {
                return;
            }

            _defaultLocalScale = transform.localScale;
            _isCached = true;
        }
    }
}
