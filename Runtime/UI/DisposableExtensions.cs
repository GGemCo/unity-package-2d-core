using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    public static class DisposableExtensions
    {
        private static readonly Dictionary<GameObject, List<IDisposable>> DisposablesMap = new Dictionary<GameObject, List<IDisposable>>();

        public static IDisposable AddTo(this IDisposable disposable, MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour == null) return disposable;

            GameObject obj = monoBehaviour.gameObject;

            if (!DisposablesMap.ContainsKey(obj))
            {
                DisposablesMap[obj] = new List<IDisposable>();
                monoBehaviour.gameObject.AddComponent<DisposableCleaner>();
            }

            DisposablesMap[obj].Add(disposable);
            return disposable;
        }

        private static void DisposeAll(GameObject obj)
        {
            if (DisposablesMap.TryGetValue(obj, out var disposables))
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
                disposables.Clear();
                DisposablesMap.Remove(obj);
            }
        }

        private class DisposableCleaner : MonoBehaviour
        {
            private void OnDestroy()
            {
                DisposeAll(gameObject);
            }
        }
    }
}