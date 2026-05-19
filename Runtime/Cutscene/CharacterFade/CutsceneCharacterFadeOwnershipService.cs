using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 캐릭터 페이드 연출의 캐릭터별 소유권을 관리하는 정적 서비스입니다.
    /// 동일 캐릭터를 여러 컨트롤러가 동시에 제어할 때, 최신 owner의 상태를 보호합니다.
    /// </summary>
    public static class CutsceneCharacterFadeOwnershipService
    {
        /// <summary>
        /// 캐릭터 인스턴스 ID를 키로 사용하는 소유권 테이블입니다.
        /// </summary>
        private static readonly Dictionary<int, OwnershipState> OwnershipByCharacterId = new();

        /// <summary>
        /// 대상 캐릭터의 페이드 소유권을 획득합니다.
        /// 기존 소유자가 없거나 동일 소유자일 때만 성공합니다.
        /// </summary>
        /// <param name="targetCharacter">소유권을 획득할 대상 캐릭터입니다.</param>
        /// <param name="owner">소유권 요청자입니다.</param>
        /// <param name="capturedColor">획득 시점의 캐릭터 색상입니다.</param>
        /// <param name="capturedActiveState">획득 시점의 활성화 상태입니다.</param>
        /// <returns>소유권 획득에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        public static bool TryAcquire(
            CharacterBase targetCharacter,
            object owner,
            out Color capturedColor,
            out bool capturedActiveState)
        {
            capturedColor = Color.white;
            capturedActiveState = true;

            if (targetCharacter == null || owner == null)
            {
                return false;
            }

            int characterId = targetCharacter.GetInstanceID();
            if (OwnershipByCharacterId.TryGetValue(characterId, out OwnershipState currentState))
            {
                if (currentState.Owner != null && !ReferenceEquals(currentState.Owner, owner))
                {
                    return false;
                }

                capturedColor = currentState.CapturedColor;
                capturedActiveState = currentState.CapturedActiveState;
                return true;
            }

            capturedActiveState = targetCharacter.gameObject.activeSelf;
            if (!TryCaptureCurrentColor(targetCharacter, out capturedColor))
            {
                capturedColor = Color.white;
            }

            OwnershipByCharacterId[characterId] = new OwnershipState(owner, capturedColor, capturedActiveState);
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
        /// 컨트롤러 종료 시 대상 참조 유실 상황을 안전하게 정리하기 위해 사용합니다.
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
        /// 캐릭터의 현재 색상을 추출합니다.
        /// </summary>
        /// <param name="targetCharacter">색상을 추출할 대상 캐릭터입니다.</param>
        /// <param name="color">추출된 색상입니다.</param>
        /// <returns>색상 추출에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryCaptureCurrentColor(CharacterBase targetCharacter, out Color color)
        {
            color = Color.white;
            if (targetCharacter == null)
            {
                return false;
            }

            var spriteRenderers = targetCharacter.GetComponentsInChildren<SpriteRenderer>(true);
            if (spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] == null)
                    {
                        continue;
                    }

                    color = spriteRenderers[i].color;
                    return true;
                }
            }

#if GGEMCO_USE_SPINE
            var skeletonAnimation = targetCharacter.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (skeletonAnimation?.Skeleton != null)
            {
                color = skeletonAnimation.Skeleton.GetColor();
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// 캐릭터 페이드 소유권 상태를 나타내는 내부 데이터입니다.
        /// </summary>
        private readonly struct OwnershipState
        {
            /// <summary>
            /// 현재 소유자입니다.
            /// </summary>
            public readonly object Owner;

            /// <summary>
            /// 소유권 획득 시점의 원본 색상입니다.
            /// </summary>
            public readonly Color CapturedColor;

            /// <summary>
            /// 소유권 획득 시점의 활성화 상태입니다.
            /// </summary>
            public readonly bool CapturedActiveState;

            /// <summary>
            /// 소유권 상태를 생성합니다.
            /// </summary>
            /// <param name="owner">현재 소유자입니다.</param>
            /// <param name="capturedColor">획득 시점의 원본 색상입니다.</param>
            /// <param name="capturedActiveState">획득 시점의 활성화 상태입니다.</param>
            public OwnershipState(object owner, Color capturedColor, bool capturedActiveState)
            {
                Owner = owner;
                CapturedColor = capturedColor;
                CapturedActiveState = capturedActiveState;
            }
        }
    }
}
