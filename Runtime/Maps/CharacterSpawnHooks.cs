using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 로딩(스폰) 파이프라인에서 캐릭터 스폰 직후 추가 초기화를 수행하기 위한 Hook.
    /// </summary>
    /// <remarks>
    /// - Core는 BT/Skill 등 외부 패키지를 직접 참조하지 않는다.
    /// - 외부 패키지는 본 Hook에 등록하여 스폰 이후 비동기 초기화(예: Addressables 로드)를 수행할 수 있다.
    /// - 예외는 Core에서 흡수(로그)하고 스폰/맵 로딩을 계속 진행한다.
    /// </remarks>
    public static class CharacterSpawnHooks
    {
        /// <summary>
        /// 캐릭터 스폰 직후 실행되는 비동기 Hook.
        /// </summary>
        public static event Func<CharacterBase, Task> OnCharacterSpawnedAsync;

        /// <summary>
        /// 맵 언로드(다음 맵 로딩 전, 기존 맵 리소스 정리 시점) 시 호출된다.
        /// </summary>
        public static event Action OnMapUnload;

        /// <summary>
        /// 등록된 Hook을 모두 실행한다. 실패해도 로그만 남기고 계속 진행한다.
        /// </summary>
        public static async Task InvokeAsync(CharacterBase ch)
        {
            if (ch == null) return;
            var handlers = OnCharacterSpawnedAsync;
            if (handlers == null) return;

            foreach (Func<CharacterBase, Task> handler in handlers.GetInvocationList())
            {
                try
                {
                    var task = handler?.Invoke(ch);
                    if (task != null)
                        await task;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// 여러 캐릭터에 대한 Hook을 병렬 실행한다. 실패해도 로그만 남기고 계속 진행한다.
        /// </summary>
        public static async Task InvokeAllAsync(IReadOnlyList<CharacterBase> characters)
        {
            if (characters == null || characters.Count <= 0) return;

            var tasks = new List<Task>(characters.Count);
            for (int i = 0; i < characters.Count; i++)
            {
                var ch = characters[i];
                if (ch == null) continue;
                tasks.Add(InvokeAsync(ch));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                // 개별 InvokeAsync에서 예외는 흡수하지만, 방어적으로 한 번 더 로깅한다.
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Core 내부에서만 호출: 맵 언로드 알림.
        /// </summary>
        internal static void NotifyMapUnload()
        {
            try
            {
                OnMapUnload?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
