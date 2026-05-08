using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷씬 이벤트 구간 동안 캐릭터 조작, 자동 이동, 몬스터 Brain을 토큰 기반으로 잠그고 해제합니다.
    /// </summary>
    public sealed class CharacterControlLockController : CutsceneDefaultController, ICutsceneController
    {
        private readonly List<RuntimeLockHandle> _lockHandles = new List<RuntimeLockHandle>();
        private CharacterControlLockData _data;
        private float _elapsed;
        private float _duration;

        /// <summary>
        /// 캐릭터 조작 잠금 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷씬 흐름을 관리하는 매니저입니다.</param>
        public CharacterControlLockController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 별도 리소스 로드가 필요하지 않으므로 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 즉시 준비 단계입니다.
        /// 조작 잠금은 실제 트리거 시점에만 적용되어야 하므로 이 단계에서는 별도 처리를 하지 않습니다.
        /// </summary>
        /// <param name="evt">준비할 컷씬 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
        }

        /// <summary>
        /// 컷씬 이벤트 실행 전에 필요한 준비를 수행합니다.
        /// 이 컨트롤러는 즉시 준비만으로 충분하므로 바로 종료합니다.
        /// </summary>
        /// <param name="evt">준비할 컷씬 이벤트입니다.</param>
        /// <returns>준비 완료를 나타내는 코루틴입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 이벤트 데이터에 정의된 대상들을 해석하고 필요한 잠금 토큰을 획득합니다.
        /// 이미 보유한 잠금이 있으면 먼저 해제한 뒤 새 이벤트 설정을 적용합니다.
        /// </summary>
        /// <param name="evt">실행할 컷씬 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterControlLock)
            {
                return;
            }

            ReleaseLocks();

            _data = evt.characterControlLock ?? new CharacterControlLockData();
            _duration = Mathf.Max(0f, evt.duration);
            _elapsed = 0f;

            if (_data.lockMask == CharacterControlLockMask.None)
            {
                return;
            }

            List<CharacterBase> targets = ResolveTargetCharacters(_data);
            if (targets.Count <= 0)
            {
                GcLogger.LogError("CharacterControlLock을 적용할 캐릭터를 찾을 수 없습니다.");
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                AcquireLocks(targets[i], _data.lockMask, _data.stopImmediately);
            }
        }

        /// <summary>
        /// 이벤트 클립 지속 시간이 끝났을 때 설정에 따라 잠금을 해제합니다.
        /// </summary>
        public void Update()
        {
            if (_lockHandles.Count <= 0 || _data == null || !_data.releaseOnClipEnd || _duration <= 0f)
            {
                return;
            }

            _elapsed += CutsceneManager != null ? CutsceneManager.GetTimelineDeltaTime() : Time.deltaTime;
            if (_elapsed < _duration)
            {
                return;
            }

            ReleaseLocks();
        }

        /// <summary>
        /// 이벤트 재생이 중단되었을 때 설정에 따라 현재 컨트롤러가 보유한 잠금을 해제합니다.
        /// </summary>
        public void Stop()
        {
            if (_data == null || _data.releaseOnClipEnd)
            {
                ReleaseLocks();
            }
        }

        /// <summary>
        /// 컷씬 종료 시 설정에 따라 현재 컨트롤러가 보유한 잠금을 해제합니다.
        /// </summary>
        public void End()
        {
            if (_data == null || _data.releaseOnCutsceneEnd)
            {
                ReleaseLocks();
            }
        }

        /// <summary>
        /// 단일 캐릭터에 필요한 잠금 토큰을 획득하고 즉시 정지 옵션을 반영합니다.
        /// </summary>
        /// <param name="target">잠금을 적용할 캐릭터입니다.</param>
        /// <param name="lockMask">적용할 잠금 기능 마스크입니다.</param>
        /// <param name="stopImmediately">잠금 시작 시 즉시 대기 상태로 돌릴지 여부입니다.</param>
        private void AcquireLocks(CharacterBase target, CharacterControlLockMask lockMask, bool stopImmediately)
        {
            if (target == null)
            {
                return;
            }

            var handle = new RuntimeLockHandle
            {
                Character = target
            };

            if (HasMask(lockMask, CharacterControlLockMask.CharacterControl))
            {
                handle.ControlToken = target.AcquireControlLock(this);
            }

            if (HasMask(lockMask, CharacterControlLockMask.MonsterBrain))
            {
                handle.BrainToken = target.AcquireBrainLock(this);
            }

            if (HasMask(lockMask, CharacterControlLockMask.AutoMove))
            {
                handle.AutoMoveSuspendService = target.GetComponent<IAutoMoveSuspendService>();
                if (handle.AutoMoveSuspendService != null)
                {
                    handle.AutoMoveToken = handle.AutoMoveSuspendService.AcquireSuspend(AutoMoveSuspendReason.Cutscene);
                }
            }

            if (stopImmediately)
            {
                StopCharacterImmediately(target);
            }

            _lockHandles.Add(handle);
        }

        /// <summary>
        /// 잠금 시작 시점의 이동 입력과 이동 애니메이션을 즉시 멈춥니다.
        /// </summary>
        /// <param name="target">즉시 정지할 캐릭터입니다.</param>
        private static void StopCharacterImmediately(CharacterBase target)
        {
            if (target == null || target.IsStatusDead())
            {
                return;
            }

            target.directionNormalize = Vector3.zero;
            target.Stop(isForce: true);
        }

        /// <summary>
        /// 이벤트 데이터의 대상 범위를 실제 캐릭터 목록으로 변환합니다.
        /// </summary>
        /// <param name="data">대상 범위를 포함한 잠금 이벤트 데이터입니다.</param>
        /// <returns>중복이 제거된 캐릭터 목록입니다.</returns>
        private List<CharacterBase> ResolveTargetCharacters(CharacterControlLockData data)
        {
            var targets = new List<CharacterBase>();
            CharacterControlLockTargetScope scope = data?.targetScope ?? CharacterControlLockTargetScope.Target;

            switch (scope)
            {
                case CharacterControlLockTargetScope.Player:
                    AddUnique(targets, ResolvePlayerCharacter());
                    break;

                case CharacterControlLockTargetScope.CurrentMapMonsters:
                    AddCurrentMapMonsters(targets);
                    break;

                case CharacterControlLockTargetScope.PlayerAndCurrentMapMonsters:
                    AddUnique(targets, ResolvePlayerCharacter());
                    AddCurrentMapMonsters(targets);
                    break;

                case CharacterControlLockTargetScope.SceneCharacters:
                    AddSceneCharacters(targets);
                    break;

                case CharacterControlLockTargetScope.Target:
                default:
                    AddUnique(targets, ResolveTargetCharacter(data));
                    break;
            }

            return targets;
        }

        /// <summary>
        /// 플레이어 캐릭터를 현재 씬과 컷씬 매니저 등록 정보에서 순서대로 찾습니다.
        /// </summary>
        /// <returns>찾은 플레이어 캐릭터입니다. 없으면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolvePlayerCharacter()
        {
            if (SceneGame.Instance != null && SceneGame.Instance.player != null)
            {
                CharacterBase player = SceneGame.Instance.player.GetComponent<CharacterBase>();
                if (player != null)
                {
                    return player;
                }
            }

            Transform target = GetTargetTransform(CharacterConstants.Type.Player, 0);
            if (target == null && CutsceneManager != null)
            {
                target = CutsceneManager.GetCharacter(CharacterConstants.Type.Player, 0);
            }

            return target != null ? target.GetComponent<CharacterBase>() : null;
        }

        /// <summary>
        /// 현재 맵에 등록된 몬스터들을 대상 목록에 추가합니다.
        /// </summary>
        /// <param name="targets">몬스터 캐릭터를 추가할 대상 목록입니다.</param>
        private static void AddCurrentMapMonsters(List<CharacterBase> targets)
        {
            if (SceneGame.Instance == null || SceneGame.Instance.mapManager == null)
            {
                return;
            }

            List<KeyValuePair<int, GameObject>> monsters = SceneGame.Instance.mapManager.GetCurrentMapMonsterEntries();
            for (int i = 0; i < monsters.Count; i++)
            {
                GameObject monsterObject = monsters[i].Value;
                AddUnique(targets, monsterObject != null ? monsterObject.GetComponent<CharacterBase>() : null);
            }
        }

        /// <summary>
        /// 현재 씬에서 찾을 수 있는 활성 캐릭터들을 대상 목록에 추가합니다.
        /// </summary>
        /// <param name="targets">캐릭터를 추가할 대상 목록입니다.</param>
        private static void AddSceneCharacters(List<CharacterBase> targets)
        {
            CharacterBase[] characters = CompatObjectFind.FindAllUnsorted<CharacterBase>();
            for (int i = 0; i < characters.Length; i++)
            {
                AddUnique(targets, characters[i]);
            }
        }

        /// <summary>
        /// 이벤트 데이터에 정의된 단일 캐릭터 참조를 실제 캐릭터 인스턴스로 해석합니다.
        /// 런타임 오버라이드 키가 지정되어 있으면 컷씬 매니저에 등록된 대상이 우선됩니다.
        /// </summary>
        /// <param name="data">조작 잠금 이벤트 데이터입니다.</param>
        /// <returns>해석된 캐릭터입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolveTargetCharacter(CharacterControlLockData data)
        {
            CutsceneCharacterReference reference = data?.target;
            if (reference != null && reference.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                return ResolveRuntimeOverrideTarget(reference);
            }

            CharacterConstants.Type characterType = reference?.characterType ?? CharacterConstants.Type.Player;
            int characterUid = reference?.characterUid ?? 0;

            Transform target = GetTargetTransform(characterType, characterUid);
            if (target == null && CutsceneManager != null)
            {
                target = CutsceneManager.GetCharacter(characterType, characterUid);
            }

            return target != null ? target.GetComponent<CharacterBase>() : null;
        }

        /// <summary>
        /// 런타임 오버라이드 키를 사용해 조작 잠금 대상을 조회합니다.
        /// </summary>
        /// <param name="reference">런타임 대상 키를 포함한 캐릭터 참조입니다.</param>
        /// <returns>등록된 런타임 캐릭터입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolveRuntimeOverrideTarget(CutsceneCharacterReference reference)
        {
            if (reference.runtimeTargetKey == CutsceneKeyCharacterTarget.None)
            {
                GcLogger.Log("CharacterControlLock runtime override key가 None입니다.");
                return null;
            }

            if (CutsceneManager != null &&
                CutsceneManager.TryGetCharacterTargetOverride(reference.runtimeTargetKey, out CharacterBase runtimeCharacter))
            {
                return runtimeCharacter;
            }

            GcLogger.Log($"CharacterControlLock runtime override not found. key={reference.runtimeTargetKey}");
            return null;
        }

        /// <summary>
        /// 캐릭터 목록에 같은 인스턴스가 중복으로 들어가지 않도록 추가합니다.
        /// </summary>
        /// <param name="targets">대상 캐릭터 목록입니다.</param>
        /// <param name="character">추가할 캐릭터입니다.</param>
        private static void AddUnique(List<CharacterBase> targets, CharacterBase character)
        {
            if (targets == null || character == null || targets.Contains(character))
            {
                return;
            }

            targets.Add(character);
        }

        /// <summary>
        /// 현재 컨트롤러가 획득한 모든 잠금 토큰을 해제하고 내부 상태를 초기화합니다.
        /// </summary>
        private void ReleaseLocks()
        {
            for (int i = _lockHandles.Count - 1; i >= 0; i--)
            {
                RuntimeLockHandle handle = _lockHandles[i];
                if (handle.Character != null)
                {
                    if (handle.ControlToken != null)
                    {
                        handle.Character.ReleaseControlLock(handle.ControlToken);
                    }

                    if (handle.BrainToken != null)
                    {
                        handle.Character.ReleaseBrainLock(handle.BrainToken);
                    }
                }

                if (handle.AutoMoveSuspendService != null && handle.AutoMoveToken.IsValid)
                {
                    handle.AutoMoveSuspendService.ReleaseSuspend(handle.AutoMoveToken);
                }
            }

            _lockHandles.Clear();
            _elapsed = 0f;
        }

        /// <summary>
        /// 지정된 잠금 마스크가 특정 기능을 포함하는지 확인합니다.
        /// </summary>
        /// <param name="mask">검사할 전체 잠금 마스크입니다.</param>
        /// <param name="value">포함 여부를 확인할 기능 값입니다.</param>
        /// <returns>지정한 기능이 포함되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool HasMask(CharacterControlLockMask mask, CharacterControlLockMask value)
        {
            return (mask & value) == value;
        }

        /// <summary>
        /// 단일 캐릭터에 대해 획득한 잠금 토큰 모음입니다.
        /// </summary>
        private struct RuntimeLockHandle
        {
            /// <summary>잠금 대상 캐릭터입니다.</summary>
            public CharacterBase Character;

            /// <summary>캐릭터 조작 잠금 토큰입니다.</summary>
            public object ControlToken;

            /// <summary>몬스터 Brain 잠금 토큰입니다.</summary>
            public object BrainToken;

            /// <summary>자동 이동 일시정지 서비스입니다.</summary>
            public IAutoMoveSuspendService AutoMoveSuspendService;

            /// <summary>자동 이동 일시정지 토큰입니다.</summary>
            public AutoMoveSuspendToken AutoMoveToken;
        }
    }
}
