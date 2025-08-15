using System;
using System.Collections;
using System.Threading.Tasks;

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables 기반 로더(비동기 Task + 진행률 Getter)를 감싸는 범용 스텝
    /// </summary>
    public sealed class AddressableTaskStep : GameLoadStepBase
    {
        private readonly Func<Task> _startTask;
        private readonly Func<float> _getProgress;

        /// <param name="id">예: "core.prefab.common"</param>
        /// <param name="order">실행 순서</param>
        /// <param name="localizedKey">진행률 UI 부제목용 키</param>
        /// <param name="startTask">로딩을 수행하는 Task 시작 함수</param>
        /// <param name="getProgress">0~1 진행률 Getter</param>
        public AddressableTaskStep(
            string id, int order, string localizedKey,
            Func<Task> startTask,
            Func<float> getProgress)
            : base(id, order, localizedKey)
        {
            _startTask = startTask;
            _getProgress = getProgress;
        }

        public override IEnumerator Run()
        {
            // Task를 코루틴으로 polling
            var task = _startTask?.Invoke();
            if (task == null)
            {
                progress = 1f;
                yield break;
            }

            while (!task.IsCompleted)
            {
                progress = _getProgress != null ? UnityEngine.Mathf.Clamp01(_getProgress()) : progress;
                yield return null;
            }
            progress = 1f;
        }
    }
}