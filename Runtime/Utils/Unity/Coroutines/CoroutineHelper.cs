using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 코루틴 실행을 위한 전역 Runner.
    /// - 런타임에서도 안전하게 동작하도록 DontDestroyOnLoad 사용
    /// - Editor/Runtime 공통 사용 가능
    /// </summary>
    public sealed class CoroutineHelper : MonoBehaviour
    {
        private static CoroutineHelper _instance;

        public static CoroutineHelper Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var existing = FindFirstObjectByType<CoroutineHelper>();
                if (existing != null)
                {
                    _instance = existing;
                    return _instance;
                }

                var go = new GameObject("[GGemCo] CoroutineHelper");
                _instance = go.AddComponent<CoroutineHelper>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                return;
            }

            if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public Coroutine Run(IEnumerator routine)
        {
            if (routine == null) return null;
            return StartCoroutine(routine);
        }

        public void Stop(Coroutine coroutine)
        {
            if (coroutine == null) return;
            StopCoroutine(coroutine);
        }

        public void Stop(IEnumerator routine)
        {
            if (routine == null) return;
            StopCoroutine(routine);
        }
    }
}