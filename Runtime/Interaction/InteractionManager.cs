using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Npc와의 상호작용 처리 매니저입니다.
    /// </summary>
    public class InteractionManager
    {
        /// <summary>
        /// 자식 UIWindow 전환으로 일시 중단한 인터랙션 요청을 식별하는 토큰입니다.
        /// </summary>
        internal readonly struct InteractionSuspensionToken
        {
            internal readonly int Id;

            /// <summary>
            /// 유효한 일시 중단 요청인지 여부입니다.
            /// </summary>
            internal bool IsValid => Id > 0;

            /// <summary>
            /// 일시 중단 요청 식별자를 보관하는 토큰을 생성합니다.
            /// </summary>
            /// <param name="id">0보다 큰 요청 식별자입니다.</param>
            internal InteractionSuspensionToken(int id)
            {
                Id = id;
            }
        }

        private SceneGame _sceneGame;
        private TableNpc _tableNpc;
        private TableInteraction _tableInteraction;
        private UIWindowInteractionDialogue _uiWindowInteractionDialogue;
        private CharacterBase _currentNpc;
        private CharacterBase _suspendedNpc;
        private CharacterBase _interactionLockedPlayer;
        private object _interactionControlLockToken;
        private GGemCoNpcInteractionSettings _npcInteractionSettings;
        private readonly Dictionary<int, InteractionBlockReason> _interactionBlockReasons = new();
        private readonly List<InteractionChoiceContribution> _externalChoices =
            new List<InteractionChoiceContribution>();
        private int _nextInteractionBlockTokenId;
        private int _nextSuspensionTokenId;
        private int _activeSuspensionTokenId;

        public CharacterBase CurrentNpc => _currentNpc;

        /// <summary>
        /// NPC 인터랙션 활성 상태가 변경될 때 호출됩니다.
        /// 모바일 HUD, 외부 입력 표시 정책처럼 인터랙션 상태에 반응해야 하는 시스템에서 구독합니다.
        /// </summary>
        public event System.Action<bool> InteractionActiveChanged;

        /// <summary>
        /// 상호작용 매니저에 씬 의존성을 연결합니다.
        /// </summary>
        /// <param name="scene">현재 게임 씬입니다.</param>
        public void Initialize(SceneGame scene)
        {
            _sceneGame = scene;
            _tableNpc = TableLoaderManager.Instance.TableNpc;
            _tableInteraction = TableLoaderManager.Instance.TableInteraction;
            _npcInteractionSettings = ResolveNpcInteractionSettings();
        }

        /// <summary>
        /// NPC의 interaction 정보를 읽어 인터랙션 대화창을 엽니다.
        /// </summary>
        /// <param name="characterBase">대화 대상 NPC입니다.</param>
        public void SetInfo(CharacterBase characterBase)
        {
            SetInfo(characterBase, InteractionDialogueTextContext.Empty);
        }

        /// <summary>
        /// NPC의 interaction 정보를 읽어 인터랙션 대화창을 엽니다.
        /// 대사 본문과 선택지에서 사용할 위치 기반 파라미터를 함께 전달합니다.
        /// </summary>
        /// <param name="characterBase">대화 대상 NPC입니다.</param>
        /// <param name="dialogueParameters">대사 포맷에 사용할 위치 기반 파라미터입니다.</param>
        public void SetInfo(CharacterBase characterBase, params object[] dialogueParameters)
        {
            SetInfo(characterBase, InteractionDialogueTextContext.FromArgs(dialogueParameters));
        }

        /// <summary>
        /// NPC의 interaction 정보를 읽어 인터랙션 대화창을 엽니다.
        /// </summary>
        /// <param name="characterBase">대화 대상 NPC입니다.</param>
        /// <param name="textContext">대사 포맷에 사용할 텍스트 컨텍스트입니다.</param>
        public void SetInfo(CharacterBase characterBase, InteractionDialogueTextContext textContext)
        {
            // 상호작용 차단 정책이 활성화되어 있으면 대화창을 열지 않습니다.
            if (IsInteractionBlocked())
            {
                return;
            }

            if (characterBase == null)
            {
                GcLogger.LogError("Npc 스크립트가 없습니다.");
                return;
            }

            StruckTableNpc infoNpc = _tableNpc.GetDataByUid(characterBase.uid);
            if (infoNpc == null)
            {
                GcLogger.LogError("npc 테이블에 정보가 없습니다. npc uid: " + characterBase.uid);
                return;
            }

            _currentNpc = characterBase;
            _npcInteractionSettings = ResolveNpcInteractionSettings();
            if (ShouldLockPlayerControlOnInteractionStart(_npcInteractionSettings))
            {
                BeginPlayerControlLockForInteraction();
            }

            // 인터렉션 정보
            StruckTableInteraction infoInteraction = null;
            if (infoNpc.InteractionUid > 0)
            {
                infoInteraction = _tableInteraction.GetDataByUid(infoNpc.InteractionUid);
            }

            // 외부 패키지 선택지는 호출마다 재사용 목록을 비운 뒤 수집하여 불필요한 할당을 줄입니다.
            _externalChoices.Clear();
            InteractionChoiceContributorRegistry.Collect(
                _currentNpc,
                infoNpc,
                infoInteraction,
                _externalChoices);

            // 다른 윈도우가 열려있으면 닫아주기
            if (_npcInteractionSettings != null && _npcInteractionSettings.ui.hideOtherUiOnStart)
            {
                _sceneGame.uIWindowManager?.CloseAll(new List<UIWindowConstants.WindowUid>
                {
                    UIWindowConstants.WindowUid.InteractionDialogue,
                });
            }

            bool shouldUseFirstDialogue = ShouldUseFirstDialogue(infoNpc, infoInteraction);
            InteractionDialogueSelectionResult dialogueSelection =
                InteractionDialogueSelector.Select(infoInteraction, shouldUseFirstDialogue);

            Action firstDialogueCompleted = null;
            if (shouldUseFirstDialogue &&
                dialogueSelection.HasDialogue &&
                dialogueSelection.DialogueUid == infoInteraction.FirstDialogueUid)
            {
                int npcUid = infoNpc.Uid;
                int interactionUid = infoInteraction.Uid;
                firstDialogueCompleted = () =>
                    MarkFirstDialogueCompleted(npcUid, interactionUid);
            }

            // 인터렉션 대화창 보여주기
            ShowDialogue(
                _currentNpc,
                infoNpc,
                infoInteraction,
                _externalChoices,
                _npcInteractionSettings,
                dialogueSelection,
                textContext,
                firstDialogueCompleted);
        }

        /// <summary>
        /// 인터랙션 대화창에 현재 NPC 상호작용 정보를 전달합니다.
        /// </summary>
        /// <param name="npc">현재 NPC입니다.</param>
        /// <param name="struckTableNpc">NPC 테이블 데이터입니다.</param>
        /// <param name="struckTableInteraction">인터랙션 테이블 데이터입니다.</param>
        /// <param name="externalChoices">외부 패키지가 제공한 선택지 목록입니다.</param>
        /// <param name="npcInteractionSettings">NPC 인터랙션 설정입니다.</param>
        /// <param name="dialogueSelection">이번 인터랙션에서 선택된 dialogue 정보입니다.</param>
        /// <param name="textContext">대사 포맷에 사용할 텍스트 컨텍스트입니다.</param>
        /// <param name="firstDialogueCompleted">첫 대화 정상 완료 시 호출할 콜백입니다.</param>
        private void ShowDialogue(
            CharacterBase npc,
            StruckTableNpc struckTableNpc,
            StruckTableInteraction struckTableInteraction,
            List<InteractionChoiceContribution> externalChoices,
            GGemCoNpcInteractionSettings npcInteractionSettings,
            InteractionDialogueSelectionResult dialogueSelection,
            InteractionDialogueTextContext textContext,
            Action firstDialogueCompleted)
        {
            if (_uiWindowInteractionDialogue == null)
            {
                _uiWindowInteractionDialogue =
                    _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInteractionDialogue>(
                        UIWindowConstants.WindowUid.InteractionDialogue);
            }

            _uiWindowInteractionDialogue?.SetInfos(
                npc,
                struckTableNpc,
                struckTableInteraction,
                externalChoices,
                npcInteractionSettings,
                dialogueSelection,
                textContext,
                firstDialogueCompleted);
        }

        /// <summary>
        /// 현재 NPC와 Interaction 조합에서 첫 대화를 우선 재생해야 하는지 확인합니다.
        /// 첫 대화 UID가 없거나 저장 데이터에 완료 기록이 있으면 기존 대화 선택 규칙을 사용합니다.
        /// </summary>
        /// <param name="npcData">현재 NPC 테이블 데이터입니다.</param>
        /// <param name="interactionData">현재 Interaction 테이블 데이터입니다.</param>
        /// <returns>첫 대화를 우선 선택해야 하면 <see langword="true"/>입니다.</returns>
        private bool ShouldUseFirstDialogue(
            StruckTableNpc npcData,
            StruckTableInteraction interactionData)
        {
            if (npcData == null ||
                npcData.Uid <= 0 ||
                interactionData == null ||
                interactionData.Uid <= 0 ||
                interactionData.FirstDialogueUid <= 0)
            {
                return false;
            }

            NpcInteractionProgressData progress =
                _sceneGame?.saveDataManager?.NpcInteractionProgress;
            return progress == null ||
                   !progress.IsFirstDialogueCompleted(npcData.Uid, interactionData.Uid);
        }

        /// <summary>
        /// 첫 대화가 정상적으로 끝난 NPC와 Interaction 조합을 저장 데이터에 기록합니다.
        /// </summary>
        /// <param name="npcUid">첫 대화를 완료한 NPC UID입니다.</param>
        /// <param name="interactionUid">첫 대화를 완료한 Interaction UID입니다.</param>
        private void MarkFirstDialogueCompleted(int npcUid, int interactionUid)
        {
            _sceneGame?.saveDataManager?.NpcInteractionProgress
                ?.MarkFirstDialogueCompleted(npcUid, interactionUid);
        }

        /// <summary>
        /// 현재 플레이어와 NPC 공격 범위의 겹침 상태를 1회 검사하고,
        /// 상호작용 가능한 NPC가 있으면 인터랙션 대화창을 엽니다.
        /// Trigger 재진입 이벤트가 발생하지 않는 상황에서 Intro 종료 후 후처리로 사용합니다.
        /// </summary>
        /// <returns>상호작용을 시작했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryRefreshPlayerNpcInteractionOnce()
        {
            if (IsInteractionBlocked() || IsInteractioning())
            {
                return false;
            }

            CharacterBase player = ResolvePlayer();
            if (player == null || player.colliderHitArea == null || !player.colliderHitArea.enabled)
            {
                return false;
            }

            Npc nearestNpc = FindNearestInteractableNpc(player);
            if (nearestNpc == null)
            {
                return false;
            }

            SetInfo(nearestNpc, nearestNpc.BuildInteractionTextContext());
            return true;
        }

        /// <summary>
        /// 현재 게임 씬에서 플레이어 캐릭터를 안전하게 찾습니다.
        /// </summary>
        /// <returns>현재 플레이어 캐릭터입니다. 없으면 null을 반환합니다.</returns>
        private CharacterBase ResolvePlayer()
        {
            if (_sceneGame == null || _sceneGame.player == null)
            {
                return null;
            }

            CharacterBase player = _sceneGame.player.GetComponent<CharacterBase>();
            return player != null && player.IsPlayer() && !player.IsStatusDead() ? player : null;
        }

        /// <summary>
        /// 현재 플레이어가 상호작용 범위 안에 있는 NPC 중 가장 가까운 NPC를 찾습니다.
        /// </summary>
        /// <param name="player">상호작용 기준이 되는 플레이어입니다.</param>
        /// <returns>상호작용 가능한 가장 가까운 NPC입니다. 없으면 null을 반환합니다.</returns>
        private Npc FindNearestInteractableNpc(CharacterBase player)
        {
            if (player == null || player.colliderHitArea == null)
            {
                return null;
            }

            IEnumerable<Npc> activeNpcs = _sceneGame?.mapManager?.GetActiveNpcs();
            if (activeNpcs == null)
            {
                return null;
            }

            Npc nearestNpc = null;
            float nearestDistanceSqr = float.MaxValue;
            Vector3 playerPosition = player.transform.position;

            foreach (Npc npc in activeNpcs)
            {
                if (!IsPlayerInsideNpcInteractionRange(npc, player.colliderHitArea))
                {
                    continue;
                }

                float distanceSqr = (npc.transform.position - playerPosition).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                nearestNpc = npc;
            }

            return nearestNpc;
        }

        /// <summary>
        /// 지정한 NPC의 상호작용 범위와 플레이어 HitArea가 현재 겹쳐 있는지 검사합니다.
        /// </summary>
        /// <param name="npc">검사할 NPC입니다.</param>
        /// <param name="playerHitArea">플레이어 HitArea Collider입니다.</param>
        /// <returns>현재 겹쳐 있으면 <see langword="true"/>입니다.</returns>
        private static bool IsPlayerInsideNpcInteractionRange(Npc npc, Collider2D playerHitArea)
        {
            if (npc == null || playerHitArea == null)
            {
                return false;
            }

            Collider2D npcAttackRange = npc.colliderAttackRange;
            if (npcAttackRange == null || !npcAttackRange.enabled || !playerHitArea.enabled)
            {
                return false;
            }

            if (!npcAttackRange.gameObject.activeInHierarchy || !playerHitArea.gameObject.activeInHierarchy)
            {
                return false;
            }

            ColliderDistance2D distance = npcAttackRange.Distance(playerHitArea);
            return distance.isOverlapped;
        }

        /// <summary>
        /// NPC 상호작용 시작 시 플레이어 조작을 잠글지 여부를 설정에서 확인합니다.
        /// </summary>
        /// <param name="settings">현재 NPC 상호작용 설정입니다.</param>
        /// <returns>플레이어 조작 잠금이 필요하면 <see langword="true"/>입니다.</returns>
        private static bool ShouldLockPlayerControlOnInteractionStart(GGemCoNpcInteractionSettings settings)
        {
            return settings != null && settings.ui.lockPlayerControlOnStart;
        }


        /// <summary>
        /// NPC 인터랙션 시작에 맞춰 플레이어 조작을 잠그고, 이미 진행 중인 조작 액션을 정리합니다.
        /// 대화 UI 터치만 남기기 위해 게임플레이 입력은 <see cref="CharacterBase.IsDontControl"/> 경로에서 차단되도록 합니다.
        /// </summary>
        private void BeginPlayerControlLockForInteraction()
        {
            CharacterBase player = ResolvePlayer();
            if (player == null)
            {
                return;
            }

            CancelPlayerActionsForInteraction(player);

            bool wasLocked = _interactionControlLockToken != null;
            if (!wasLocked)
            {
                _interactionLockedPlayer = player;
                _interactionControlLockToken = player.AcquireControlLock(this);
            }
            else if (_interactionLockedPlayer != player)
            {
                ReleasePlayerControlLockForInteraction();
                _interactionLockedPlayer = player;
                _interactionControlLockToken = player.AcquireControlLock(this);
                wasLocked = false;
            }

            if (!wasLocked)
            {
                InteractionActiveChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// 플레이어에 연결된 Control 패키지 어댑터를 찾아 인터랙션 시작 전 액션 정리를 요청합니다.
        /// Core는 인터페이스만 호출하여 패키지 의존성 방향을 유지합니다.
        /// </summary>
        /// <param name="player">액션을 정리할 플레이어 캐릭터입니다.</param>
        private static void CancelPlayerActionsForInteraction(CharacterBase player)
        {
            if (player == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = player.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInteractionActionCanceler canceler)
                {
                    canceler.CancelActionsOnInteractionStart();
                    return;
                }
            }
        }

        /// <summary>
        /// NPC 인터랙션이 종료될 때 플레이어 조작 잠금 토큰을 해제합니다.
        /// 씬 종료, NPC 범위 이탈, 선택지 종료 등 모든 종료 경로에서 호출되어 잠금 누락을 방지합니다.
        /// </summary>
        private void ReleasePlayerControlLockForInteraction()
        {
            bool wasLocked = _interactionControlLockToken != null;
            if (_interactionLockedPlayer != null && _interactionControlLockToken != null)
            {
                _interactionLockedPlayer.ReleaseControlLock(_interactionControlLockToken);
            }

            _interactionLockedPlayer = null;
            _interactionControlLockToken = null;

            if (wasLocked)
            {
                InteractionActiveChanged?.Invoke(false);
            }
        }


        /// <summary>
        /// 현재 NPC 상호작용 시작이 차단되어 있는지 확인합니다.
        /// </summary>
        /// <returns>컷씬 재생 중이거나 외부 차단 토큰이 하나 이상 있으면 <see langword="true"/>입니다.</returns>
        public bool IsInteractionBlocked()
        {
            return IsCutscenePlaying() || _interactionBlockReasons.Count > 0;
        }

        /// <summary>
        /// NPC 상호작용 시작을 일시 차단하는 토큰을 획득합니다.
        /// </summary>
        /// <param name="reason">상호작용 차단 사유입니다.</param>
        /// <param name="endCurrentInteraction">이미 열린 상호작용 창을 즉시 종료할지 여부입니다.</param>
        /// <returns>해제 시 사용할 상호작용 차단 토큰입니다.</returns>
        public InteractionBlockToken AcquireInteractionBlock(
            InteractionBlockReason reason,
            bool endCurrentInteraction = true)
        {
            int id = ++_nextInteractionBlockTokenId;
            if (id == 0)
            {
                id = ++_nextInteractionBlockTokenId;
            }

            _interactionBlockReasons[id] = reason;

            if (endCurrentInteraction)
            {
                EndInteraction();
            }

            return new InteractionBlockToken(id, reason);
        }

        /// <summary>
        /// 이전에 획득한 NPC 상호작용 차단 토큰을 해제합니다.
        /// </summary>
        /// <param name="token">해제할 상호작용 차단 토큰입니다.</param>
        public void ReleaseInteractionBlock(InteractionBlockToken token)
        {
            if (!token.IsValid)
            {
                return;
            }

            _interactionBlockReasons.Remove(token.id);
        }

        /// <summary>
        /// 모든 NPC 상호작용 차단 토큰을 제거합니다.
        /// 씬 종료나 매니저 정리처럼 소유자가 더 이상 유효하지 않은 시점에만 사용합니다.
        /// </summary>
        public void ClearInteractionBlocks()
        {
            _interactionBlockReasons.Clear();
        }

        /// <summary>
        /// 현재 컷씬 연출이 재생 중인지 안전하게 확인합니다.
        /// </summary>
        /// <returns>컷씬 매니저가 있고 연출이 재생 중이면 <see langword="true"/>입니다.</returns>
        private bool IsCutscenePlaying()
        {
            return _sceneGame != null &&
                   _sceneGame.CutsceneManager != null &&
                   _sceneGame.CutsceneManager.IsPlaying();
        }

        /// <summary>
        /// 자식 UIWindow를 표시하는 동안 현재 NPC 인터랙션을 재개 가능한 상태로 일시 중단합니다.
        /// 플레이어 조작 잠금과 현재 NPC 참조는 유지하여 자식 창 종료 전까지 인터랙션 범위를 벗어나지 않게 합니다.
        /// </summary>
        /// <param name="npc">현재 인터랙션 중인 NPC입니다.</param>
        /// <param name="token">성공 시 재개 또는 종료 요청에 사용할 일시 중단 토큰입니다.</param>
        /// <returns>현재 인터랙션을 일시 중단했으면 <see langword="true"/>입니다.</returns>
        internal bool TrySuspendCurrentInteraction(
            CharacterBase npc,
            out InteractionSuspensionToken token)
        {
            token = default;
            if (npc == null ||
                _currentNpc != npc ||
                _activeSuspensionTokenId > 0)
            {
                return false;
            }

            _nextSuspensionTokenId =
                _nextSuspensionTokenId >= int.MaxValue
                    ? 1
                    : _nextSuspensionTokenId + 1;

            _suspendedNpc = npc;
            _activeSuspensionTokenId = _nextSuspensionTokenId;
            token = new InteractionSuspensionToken(
                _activeSuspensionTokenId);
            return true;
        }

        /// <summary>
        /// 자식 UIWindow 열기에 실패한 경우 일시 중단 표시만 해제하고 현재 인터랙션은 유지합니다.
        /// </summary>
        /// <param name="token">취소할 일시 중단 요청 토큰입니다.</param>
        internal void CancelCurrentInteractionSuspension(
            InteractionSuspensionToken token)
        {
            if (!IsActiveSuspension(token))
            {
                return;
            }

            _activeSuspensionTokenId = 0;
            _suspendedNpc = null;
        }

        /// <summary>
        /// 일시 중단한 NPC와 플레이어의 현재 범위를 다시 검증한 뒤 인터랙션 데이터를 새로 바인딩합니다.
        /// 동적 대사 파라미터와 외부 선택지도 현재 런타임 상태를 기준으로 다시 수집합니다.
        /// </summary>
        /// <param name="token">재개할 일시 중단 요청 토큰입니다.</param>
        /// <returns>유효한 NPC 인터랙션을 다시 시작했으면 <see langword="true"/>입니다.</returns>
        internal bool ResumeSuspendedInteraction(
            InteractionSuspensionToken token)
        {
            if (!IsActiveSuspension(token))
            {
                return false;
            }

            CharacterBase suspendedNpc = _suspendedNpc;
            _activeSuspensionTokenId = 0;
            _suspendedNpc = null;

            if (suspendedNpc == null ||
                _currentNpc != suspendedNpc ||
                IsInteractionBlocked())
            {
                if (_currentNpc == suspendedNpc)
                {
                    RemoveCurrentNpc();
                }

                return false;
            }

            Npc npc = suspendedNpc as Npc;
            CharacterBase player = ResolvePlayer();
            if (npc == null ||
                player == null ||
                !IsPlayerInsideNpcInteractionRange(
                    npc,
                    player.colliderHitArea))
            {
                RemoveCurrentNpc();
                return false;
            }

            SetInfo(npc, npc.BuildInteractionTextContext());
            return true;
        }

        /// <summary>
        /// 지정한 자식 UIWindow에 연결된 일시 중단 요청이 아직 현재 세션인지 확인하고 인터랙션을 종료합니다.
        /// 이미 범위 이탈이나 다른 종료 경로로 무효화된 토큰은 새 인터랙션에 영향을 주지 않습니다.
        /// </summary>
        /// <param name="token">종료할 일시 중단 요청 토큰입니다.</param>
        internal void CompleteSuspendedInteraction(
            InteractionSuspensionToken token)
        {
            if (!IsActiveSuspension(token))
            {
                return;
            }

            EndInteraction();
        }

        /// <summary>
        /// 전달된 토큰이 현재 활성 일시 중단 요청과 일치하는지 확인합니다.
        /// </summary>
        /// <param name="token">검증할 일시 중단 요청 토큰입니다.</param>
        /// <returns>현재 활성 토큰과 일치하면 <see langword="true"/>입니다.</returns>
        private bool IsActiveSuspension(
            InteractionSuspensionToken token)
        {
            return token.IsValid &&
                   token.Id == _activeSuspensionTokenId;
        }

        /// <summary>
        /// 현재 상호작용 중인 NPC 참조를 제거합니다.
        /// </summary>
        public void RemoveCurrentNpc()
        {
            _activeSuspensionTokenId = 0;
            _suspendedNpc = null;
            _currentNpc = null;
            ReleasePlayerControlLockForInteraction();
        }

        /// <summary>
        /// 현재 인터랙션 대화창을 종료합니다.
        /// </summary>
        public void EndInteraction()
        {
            _uiWindowInteractionDialogue?.OnEndInteraction();
            _activeSuspensionTokenId = 0;
            _suspendedNpc = null;
            _currentNpc = null;
            ReleasePlayerControlLockForInteraction();
        }

        /// <summary>
        /// 현재 인터랙션이 활성 상태인지 여부를 반환합니다.
        /// </summary>
        /// <returns>현재 NPC 참조가 있으면 true입니다.</returns>
        public bool IsInteractioning()
        {
            return _currentNpc != null;
        }

        /// <summary>
        /// Addressables 로더에서 NPC 인터랙션 설정을 가져오고,
        /// 없으면 런타임 기본값을 사용합니다.
        /// </summary>
        /// <returns>사용 가능한 NPC 인터랙션 설정입니다.</returns>
        private GGemCoNpcInteractionSettings ResolveNpcInteractionSettings()
        {
            if (AddressableLoaderSettings.Instance != null &&
                AddressableLoaderSettings.Instance.npcInteractionSettings != null)
            {
                _npcInteractionSettings = AddressableLoaderSettings.Instance.npcInteractionSettings;
            }

            if (_npcInteractionSettings == null)
            {
                _npcInteractionSettings = GGemCoNpcInteractionSettings.CreateRuntimeDefault();
            }

            return _npcInteractionSettings;
        }

        /// <summary>
        /// 매니저 종료 시 정리할 작업을 처리합니다.
        /// </summary>
        public void OnDestroy()
        {
            ClearInteractionBlocks();
            _activeSuspensionTokenId = 0;
            _suspendedNpc = null;
            _currentNpc = null;
            ReleasePlayerControlLockForInteraction();
            _uiWindowInteractionDialogue = null;
        }
    }
}
