using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    [RequireComponent(typeof(RawImage))]
    public class UIBlurBackdrop : MonoBehaviour
    {
        [Tooltip("활성 상태 동안 블러 렌더 요청을 자동으로 등록합니다.")]
        public bool requestBlurWhileActive = true;

        private RawImage _rawImage;
        private Texture _lastTexture;
        private bool _isRegistered;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();

            if (!Application.isPlaying)
            {
                return;
            }

            BindTexture(force: true);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (requestBlurWhileActive && !_isRegistered)
            {
                UIBlurService.RegisterRequest();
                _isRegistered = true;
            }

            BindTexture(force: true);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            BindTexture(force: false);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_isRegistered)
            {
                UIBlurService.UnregisterRequest();
                _isRegistered = false;
            }
        }

        private void BindTexture(bool force)
        {
            if (_rawImage == null)
            {
                _rawImage = GetComponent<RawImage>();
            }

            if (_rawImage == null)
            {
                return;
            }

            Texture texture = UIBlurService.GetOutputTexture();
            if (!force && ReferenceEquals(_lastTexture, texture))
            {
                return;
            }

            _lastTexture = texture;
            _rawImage.texture = texture;
        }
    }
}
