using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 말풍선 루프 애니메이션의 캐릭터별 소유권을 관리하는 정적 서비스입니다.
    /// 동일 캐릭터를 여러 컨트롤러가 동시에 제어할 때, 현재 소유자가 아닌 컨트롤러의 해제/복원이 적용되지 않도록 보호합니다.
    /// </summary>
    public static class CutsceneDialogueLoopAnimationOwnershipService
    {
        /// <summary>
        /// 캐릭터 인스턴스 ID를 키로 사용하는 소유권 테이블입니다.
        /// </summary>
        private static readonly Dictionary<int, OwnershipState> OwnershipByCharacterId = new();

        /// <summary>
        /// 대상 캐릭터의 루프 애니메이션 소유권을 획득합니다.
        /// 기존 소유자가 없거나 동일 소유자일 때만 성공합니다.
        /// </summary>
        /// <param name="targetCharacter">소유권을 획득할 대상 캐릭터입니다.</param>
        /// <param name="owner">소유권 요청자입니다.</param>
        /// <param name="capturedPlaybackTimeScale">획득 시점의 기존 애니메이션 재생 속도입니다.</param>
        /// <returns>소유권 획득에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        public static bool TryAcquire(
            CharacterBase targetCharacter,
            object owner,
            out float capturedPlaybackTimeScale)
        {
            capturedPlaybackTimeScale = 1f;

            if (targetCharacter == null || owner == null)
            {
                return false;
            }

            int characterId = targetCharacter.GetInstanceID();
            if (OwnershipByCharacterId.TryGetValue(characterId, out OwnershipState currentState) &&
                currentState.Owner != null &&
                !ReferenceEquals(currentState.Owner, owner))
            {
                return false;
            }

            var animationController = targetCharacter.CharacterAnimationController;
            capturedPlaybackTimeScale = animationController != null
                ? Mathf.Max(0f, animationController.GetPlaybackTimeScale())
                : 1f;

            OwnershipByCharacterId[characterId] = new OwnershipState(owner, capturedPlaybackTimeScale);
            return true;
        }

        /// <summary>
        /// 지정한 owner가 대상 캐릭터의 현재 소유자인지 확인합니다.
        /// </summary>
        /// <param name="targetCharacter">확인할 대상 캐릭터입니다.</param>
        /// <param name="owner">확인할 소유자입니다.</param>
        /// <returns>현재 소유자가 일치하면 <see langword="true"/>를 반환합니다.</returns>
        public static bool IsOwnedBy(CharacterBase targetCharacter, object owner)
        {
            if (targetCharacter == null || owner == null)
            {
                return false;
            }

            int characterId = targetCharacter.GetInstanceID();
            return OwnershipByCharacterId.TryGetValue(characterId, out OwnershipState state) &&
                   ReferenceEquals(state.Owner, owner);
        }

        /// <summary>
        /// 대상 캐릭터에 대해 owner가 보유한 소유권을 해제합니다.
        /// owner가 현재 소유자가 아닐 경우 아무 동작도 하지 않습니다.
        /// </summary>
        /// <param name="targetCharacter">소유권을 해제할 대상 캐릭터입니다.</param>
        /// <param name="owner">해제를 요청한 소유자입니다.</param>
        public static void Release(CharacterBase targetCharacter, object owner)
        {
            if (targetCharacter == null || owner == null)
            {
                return;
            }

            int characterId = targetCharacter.GetInstanceID();
            if (!OwnershipByCharacterId.TryGetValue(characterId, out OwnershipState state))
            {
                return;
            }

            if (!ReferenceEquals(state.Owner, owner))
            {
                return;
            }

            OwnershipByCharacterId.Remove(characterId);
        }

        /// <summary>
        /// 지정한 owner가 보유한 모든 소유권을 해제합니다.
        /// 컨트롤러 종료 시 대상 캐릭터 참조가 유실된 경우를 안전하게 정리하기 위해 사용합니다.
        /// </summary>
        /// <param name="owner">해제할 소유자입니다.</param>
        public static void ReleaseAllByOwner(object owner)
        {
            if (owner == null || OwnershipByCharacterId.Count == 0)
            {
                return;
            }

            List<int> releaseTargets = null;

            foreach (var pair in OwnershipByCharacterId)
            {
                if (!ReferenceEquals(pair.Value.Owner, owner))
                {
                    continue;
                }

                releaseTargets ??= new List<int>();
                releaseTargets.Add(pair.Key);
            }

            if (releaseTargets == null)
            {
                return;
            }

            for (int i = 0; i < releaseTargets.Count; i++)
            {
                OwnershipByCharacterId.Remove(releaseTargets[i]);
            }
        }

        /// <summary>
        /// 캐릭터 루프 애니메이션 소유권 상태를 나타내는 내부 데이터입니다.
        /// </summary>
        private readonly struct OwnershipState
        {
            /// <summary>
            /// 현재 소유자입니다.
            /// </summary>
            public readonly object Owner;

            /// <summary>
            /// 소유권 획득 시점의 기존 애니메이션 재생 속도입니다.
            /// </summary>
            public readonly float CapturedPlaybackTimeScale;

            /// <summary>
            /// 소유권 상태를 생성합니다.
            /// </summary>
            /// <param name="owner">현재 소유자입니다.</param>
            /// <param name="capturedPlaybackTimeScale">획득 시점의 기존 애니메이션 재생 속도입니다.</param>
            public OwnershipState(object owner, float capturedPlaybackTimeScale)
            {
                Owner = owner;
                CapturedPlaybackTimeScale = capturedPlaybackTimeScale;
            }
        }
    }
}
