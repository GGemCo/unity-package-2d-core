using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 중첩될 수 있는 캐릭터 데미지 요청을 FIFO 순서로 직렬화합니다.
    /// </summary>
    /// <remarks>
    /// 피격 처리 중 Affect OnHit, 반사 피해 등으로 다시 데미지가 발생할 수 있으므로
    /// 현재 요청이 끝난 뒤 후속 요청을 처리하여 HP 갱신 순서가 뒤섞이지 않도록 합니다.
    /// </remarks>
    internal sealed class DamageRequestQueue
    {
        private const int MaxRequestsPerDrain = 128;

        private readonly Queue<MetadataDamage> _pendingRequests = new Queue<MetadataDamage>();
        private Action<MetadataDamage> _processor;
        private string _targetName;
        private bool _isDraining;

        /// <summary>
        /// 큐에서 사용할 대상 정보와 단일 요청 처리 함수를 연결합니다.
        /// </summary>
        /// <param name="targetName">과도한 중첩 발생 시 로그에 표시할 대상 이름입니다.</param>
        /// <param name="processor">단일 데미지 요청을 처리할 함수입니다.</param>
        public void Initialize(string targetName, Action<MetadataDamage> processor)
        {
            _targetName = targetName;
            _processor = processor;
            Clear();
        }

        /// <summary>
        /// 데미지 요청의 독립 복사본을 큐에 추가하고 가능한 경우 즉시 처리합니다.
        /// </summary>
        /// <param name="metadataDamage">큐에 추가할 데미지 메타데이터입니다.</param>
        public void EnqueueAndDrain(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null || _processor == null)
                return;

            _pendingRequests.Enqueue(metadataDamage.Clone());
            if (_isDraining)
                return;

            Drain();
        }

        /// <summary>
        /// 대기 중인 요청과 처리 상태를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            _pendingRequests.Clear();
            _isDraining = false;
        }

        /// <summary>
        /// 큐에 쌓인 데미지 요청을 FIFO 순서로 처리합니다.
        /// </summary>
        private void Drain()
        {
            _isDraining = true;

            try
            {
                int processedCount = 0;
                while (_pendingRequests.Count > 0)
                {
                    if (++processedCount > MaxRequestsPerDrain)
                    {
                        GcLogger.LogError(
                            $"[DamageQueue] 데미지 요청이 과도하게 중첩되어 남은 요청을 폐기합니다. " +
                            $"Target={_targetName ?? "None"}, Limit={MaxRequestsPerDrain}, " +
                            $"Remaining={_pendingRequests.Count}");
                        _pendingRequests.Clear();
                        break;
                    }

                    _processor(_pendingRequests.Dequeue());
                }
            }
            finally
            {
                _isDraining = false;
            }
        }
    }
}
