using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 캐릭터 공중 연출의 캐릭터별 소유권을 관리하는 정적 서비스입니다.
    /// 동일 캐릭터에 여러 컨트롤러가 동시에 접근할 때 최신 owner를 기준으로 상태를 보호합니다.
    /// </summary>
    public static class CutsceneCharacterAirborneOwnershipService
    {
        /// <summary>
        /// 캐릭터 인스턴스 ID를 키로 사용하는 소유권 테이블입니다.
        /// </summary>
        private static readonly Dictionary<int, OwnershipState> OwnershipByCharacterId = new();

        /// <summary>
        /// 대상 캐릭터의 공중 연출 소유권을 획득합니다.
        /// 기존 소유자가 다른 컨트롤러일 때 <paramref name="allowReplace"/>가 true면 소유권을 교체합니다.
        /// </summary>
        /// <param name="targetCharacter">소유권을 획득할 대상 캐릭터입니다.</param>
        /// <param name="owner">소유권 요청자입니다.</param>
        /// <param name="allowReplace">기존 소유자를 강제로 교체할지 여부입니다.</param>
        /// <param name="capturedWorldPositionY">최초 소유권 획득 시점의 캐릭터 월드 Y입니다.</param>
        /// <param name="capturedActiveState">최초 소유권 획득 시점의 캐릭터 활성화 상태입니다.</param>
        /// <returns>소유권 획득에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        public static bool TryAcquire(
            CharacterBase targetCharacter,
            object owner,
            bool allowReplace,
            out float capturedWorldPositionY,
            out bool capturedActiveState)
        {
            capturedWorldPositionY = 0f;
            capturedActiveState = true;

            if (targetCharacter == null || owner == null)
            {
                return false;
            }

            int characterId = targetCharacter.GetInstanceID();
            if (OwnershipByCharacterId.TryGetValue(characterId, out OwnershipState currentState))
            {
                if (!ReferenceEquals(currentState.Owner, owner))
                {
                    if (!allowReplace)
                    {
                        return false;
                    }

                    currentState = new OwnershipState(
                        owner,
                        currentState.CapturedWorldPositionY,
                        currentState.CapturedActiveState);
                    OwnershipByCharacterId[characterId] = currentState;
                }

                capturedWorldPositionY = currentState.CapturedWorldPositionY;
                capturedActiveState = currentState.CapturedActiveState;
                return true;
            }

            capturedWorldPositionY = targetCharacter.transform.position.y;
            capturedActiveState = targetCharacter.gameObject.activeSelf;
            OwnershipByCharacterId[characterId] = new OwnershipState(
                owner,
                capturedWorldPositionY,
                capturedActiveState);
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
        /// 캐릭터 공중 연출 소유권 상태를 나타내는 내부 데이터입니다.
        /// </summary>
        private readonly struct OwnershipState
        {
            /// <summary>
            /// 현재 소유자입니다.
            /// </summary>
            public readonly object Owner;

            /// <summary>
            /// 최초 소유권 획득 시점의 월드 Y입니다.
            /// </summary>
            public readonly float CapturedWorldPositionY;

            /// <summary>
            /// 최초 소유권 획득 시점의 활성화 상태입니다.
            /// </summary>
            public readonly bool CapturedActiveState;

            /// <summary>
            /// 소유권 상태를 생성합니다.
            /// </summary>
            /// <param name="owner">현재 소유자입니다.</param>
            /// <param name="capturedWorldPositionY">최초 소유권 획득 시점의 월드 Y입니다.</param>
            /// <param name="capturedActiveState">최초 소유권 획득 시점의 활성화 상태입니다.</param>
            public OwnershipState(
                object owner,
                float capturedWorldPositionY,
                bool capturedActiveState)
            {
                Owner = owner;
                CapturedWorldPositionY = capturedWorldPositionY;
                CapturedActiveState = capturedActiveState;
            }
        }
    }
}
