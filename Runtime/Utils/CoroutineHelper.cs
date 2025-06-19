using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class CoroutineHelper : MonoBehaviour
    {
        private static CoroutineHelper _instance;

        public static CoroutineHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    // GcLogger.Log("CoroutineHelper 인스턴스가 생성됩니다.");
                    var obj = new GameObject("CoroutineHelper");
                    _instance = obj.AddComponent<CoroutineHelper>();
                    obj.hideFlags = HideFlags.HideAndDontSave;
                }
                return _instance;
            }
        }

        public Coroutine StartEditorCoroutine(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }

        public void StopEditorCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    }
}