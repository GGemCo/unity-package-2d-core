using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace GGemCo2DCore
{
    public class UIBlurService : MonoBehaviour
    {
        private static UIBlurService _instance;

        public static UIBlurService Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                if (!Application.isPlaying)
                {
                    return null;
                }

                CreateSingleton();
                return _instance;
            }
        }

        public static bool HasActiveRequestSafe => _instance != null && _instance._requestCount > 0;

        private int _requestCount;

        public RTHandle OutputHandle { get; private set; }
        public Texture OutputTexture => OutputHandle != null ? OutputHandle.rt : null;

        private static void CreateSingleton()
        {
            if (_instance != null || !Application.isPlaying)
            {
                return;
            }

            var go = new GameObject(nameof(UIBlurService));
            _instance = go.AddComponent<UIBlurService>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
                return;
            }

            _instance = this;

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ReleaseOutput();
                _instance = null;
            }
        }

        public static void RegisterRequest()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_instance == null)
            {
                CreateSingleton();
            }

            if (_instance == null)
            {
                return;
            }

            _instance._requestCount++;
        }

        public static void UnregisterRequest()
        {
            if (_instance == null)
            {
                return;
            }

            _instance._requestCount = Mathf.Max(0, _instance._requestCount - 1);
        }

        public static Texture GetOutputTexture()
        {
            return _instance != null ? _instance.OutputTexture : null;
        }

        public static bool EnsureOutput(int width, int height, GraphicsFormat format)
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            if (_instance == null)
            {
                CreateSingleton();
            }

            if (_instance == null)
            {
                return false;
            }

            return _instance.InternalEnsureOutput(width, height, format);
        }

        private bool InternalEnsureOutput(int width, int height, GraphicsFormat format)
        {
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            if (OutputHandle != null &&
                OutputHandle.rt != null &&
                OutputHandle.rt.width == width &&
                OutputHandle.rt.height == height &&
                OutputHandle.rt.graphicsFormat == format)
            {
                return true;
            }

            ReleaseOutput();

            var descriptor = new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = format,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
                useMipMap = false,
                autoGenerateMips = false
            };

            OutputHandle = RTHandles.Alloc(descriptor, name: "_UIBlurOutput");
            return OutputHandle != null;
        }

        private void ReleaseOutput()
        {
            if (OutputHandle == null)
            {
                return;
            }

            OutputHandle.Release();
            OutputHandle = null;
        }
    }
}
