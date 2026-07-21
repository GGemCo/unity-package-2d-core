using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 로컬 데이터 초기화 전후에 저장 예약과 런타임 캐시를 정리해야 하는 참여자 계약입니다.
    /// 상위 패키지는 Core 타입을 직접 확장하지 않고 이 계약을 통해 초기화 흐름에 참여합니다.
    /// </summary>
    public interface ISaveDataResetParticipant
    {
        /// <summary>
        /// 초기화 참여 순서를 반환합니다. 값이 작은 참여자가 먼저 처리됩니다.
        /// </summary>
        int LocalDataResetOrder { get; }

        /// <summary>
        /// 저장 파일을 삭제하기 전에 예약 저장과 비동기 작업을 중단합니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        void PrepareLocalDataReset(SaveDataResetScope scope);

        /// <summary>
        /// 저장 파일 삭제 전에 메모리에 남아 있는 로드 결과와 런타임 캐시를 정리합니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        void ClearLocalDataRuntimeState(SaveDataResetScope scope);

        /// <summary>
        /// 로컬 데이터 초기화가 끝난 뒤 성공 또는 실패 상태를 전달받습니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        /// <param name="success">영구 저장소 삭제까지 성공했으면 true입니다.</param>
        void CompleteLocalDataReset(SaveDataResetScope scope, bool success);
    }

    /// <summary>
    /// Core와 상위 패키지의 저장 초기화 참여자를 관리하고 초기화 중 전역 저장 차단 상태를 제공합니다.
    /// </summary>
    public static class SaveDataResetParticipantRegistry
    {
        private static readonly List<ISaveDataResetParticipant> Participants = new();
        private static readonly List<ISaveDataResetParticipant> ActiveParticipants = new();

        /// <summary>
        /// 현재 로컬 데이터 초기화가 진행 중인지 확인합니다.
        /// </summary>
        public static bool IsResetInProgress { get; private set; }

        /// <summary>
        /// 저장 초기화 참여자를 중복 없이 등록합니다.
        /// </summary>
        /// <param name="participant">등록할 저장 초기화 참여자입니다.</param>
        public static void Register(ISaveDataResetParticipant participant)
        {
            if (participant == null)
            {
                return;
            }

            RemoveDestroyedParticipants(Participants);
            if (!Participants.Contains(participant))
            {
                Participants.Add(participant);
            }
        }

        /// <summary>
        /// 더 이상 사용하지 않는 저장 초기화 참여자를 등록 해제합니다.
        /// </summary>
        /// <param name="participant">등록 해제할 저장 초기화 참여자입니다.</param>
        public static void Unregister(ISaveDataResetParticipant participant)
        {
            if (participant == null)
            {
                return;
            }

            Participants.Remove(participant);
            ActiveParticipants.Remove(participant);
        }

        /// <summary>
        /// 모든 참여자의 저장 예약을 중단하고 전역 저장 차단 상태로 전환합니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        /// <returns>초기화 준비에 성공하면 true입니다.</returns>
        public static bool TryBeginReset(SaveDataResetScope scope)
        {
            if (IsResetInProgress)
            {
                return false;
            }

            IsResetInProgress = true;
            ActiveParticipants.Clear();
            RemoveDestroyedParticipants(Participants);
            Participants.Sort(CompareParticipantOrder);

            try
            {
                for (int i = 0; i < Participants.Count; i++)
                {
                    ISaveDataResetParticipant participant = Participants[i];
                    ActiveParticipants.Add(participant);
                    participant.PrepareLocalDataReset(scope);
                }

                return true;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"[SaveDataResetParticipantRegistry] 로컬 데이터 초기화 준비 중 오류가 발생했습니다. {ex}");
                CompleteReset(scope, false);
                return false;
            }
        }

        /// <summary>
        /// 현재 초기화에 참여 중인 모든 객체의 메모리 저장 상태를 정리합니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        public static void ClearRuntimeState(SaveDataResetScope scope)
        {
            for (int i = 0; i < ActiveParticipants.Count; i++)
            {
                ActiveParticipants[i].ClearLocalDataRuntimeState(scope);
            }
        }

        /// <summary>
        /// 모든 참여자에게 초기화 결과를 전달하고 전역 초기화 상태를 종료합니다.
        /// 성공한 현재 장면의 매니저는 자체 저장 차단 상태를 유지하며,
        /// DontDestroyOnLoad 로더는 다음 초기화 요청에도 참여할 수 있도록 등록을 유지합니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        /// <param name="success">영구 저장소 삭제까지 성공했으면 true입니다.</param>
        public static void CompleteReset(SaveDataResetScope scope, bool success)
        {
            for (int i = ActiveParticipants.Count - 1; i >= 0; i--)
            {
                try
                {
                    ActiveParticipants[i].CompleteLocalDataReset(scope, success);
                }
                catch (Exception ex)
                {
                    GcLogger.LogError($"[SaveDataResetParticipantRegistry] 로컬 데이터 초기화 완료 처리 중 오류가 발생했습니다. {ex}");
                }
            }

            ActiveParticipants.Clear();
            IsResetInProgress = false;

            RemoveDestroyedParticipants(Participants);
        }

        /// <summary>
        /// 도메인 또는 플레이 세션 시작 전에 정적 참여자 상태를 초기화합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInitialized()
        {
            Participants.Clear();
            ActiveParticipants.Clear();
            IsResetInProgress = false;
        }

        /// <summary>
        /// 두 참여자의 초기화 순서를 비교합니다.
        /// </summary>
        private static int CompareParticipantOrder(
            ISaveDataResetParticipant left,
            ISaveDataResetParticipant right)
        {
            return left.LocalDataResetOrder.CompareTo(right.LocalDataResetOrder);
        }

        /// <summary>
        /// Unity 수명주기가 끝난 참여자를 목록에서 제거합니다.
        /// </summary>
        private static void RemoveDestroyedParticipants(List<ISaveDataResetParticipant> participants)
        {
            for (int i = participants.Count - 1; i >= 0; i--)
            {
                ISaveDataResetParticipant participant = participants[i];
                if (participant == null ||
                    participant is UnityEngine.Object unityObject && unityObject == null)
                {
                    participants.RemoveAt(i);
                }
            }
        }
    }
}
