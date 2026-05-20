using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에서 캐릭터 생성 연출을 담당하는 컨트롤러입니다.
    /// CharacterAnimation 이벤트의 자동 생성 책임을 분리해 생성 전용 정책을 처리합니다.
    /// </summary>
    public sealed class CharacterSpawnController : CutsceneDefaultController, ICutsceneController
    {
        private CharacterSpawnData _data;

        /// <summary>
        /// 캐릭터 생성 연출 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public CharacterSpawnController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 생성 대상 준비를 위해 한 프레임 대기가 필요하므로 즉시 준비를 지원하지 않습니다.
        /// </summary>
        public bool SupportsImmediateReady => false;

        /// <summary>
        /// 즉시 준비 경로에서는 별도 동작을 수행하지 않습니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
        }

        /// <summary>
        /// 생성 대상 캐릭터를 미리 준비합니다.
        /// 대상이 이미 존재하면 생성을 생략하고, 없으면 CharacterManager를 통해 안전하게 생성합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>생성 초기화 완료를 기다리기 위한 코루틴 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterSpawn)
            {
                yield break;
            }

            _data = evt.characterSpawn ?? new CharacterSpawnData();

            if (!IsSupportedSpawnType(_data.characterType))
            {
                GcLogger.LogError(
                    "CharacterSpawn는 Monster/Npc 타입만 지원합니다. type: " +
                    _data.characterType + "/ uid: " + _data.characterUid);
                yield break;
            }

            Transform existingTarget = ResolveTargetTransform(_data.characterType, _data.characterUid);
            if (existingTarget != null)
            {
                yield return null;
                yield break;
            }

            if (SceneGame.Instance == null || SceneGame.Instance.CharacterManager == null)
            {
                GcLogger.LogError("CharacterSpawn 준비에 필요한 SceneGame.CharacterManager가 없습니다.");
                yield break;
            }

            Transform created = SceneGame.Instance.CharacterManager
                .CreateCharacter(_data.characterType, _data.characterUid)?.transform;
            if (created == null)
            {
                GcLogger.LogError(
                    "CharacterSpawn 대상 생성에 실패했습니다. type: " +
                    _data.characterType + "/ uid: " + _data.characterUid);
                yield break;
            }

            Transform currentMap = SceneGame.Instance.mapManager?.GetCurrentMap()?.transform;
            if (currentMap != null)
            {
                created.SetParent(currentMap);
            }

            CharacterBase createdCharacter = created.GetComponent<CharacterBase>();
            if (createdCharacter != null)
            {
                createdCharacter.uid = _data.characterUid;
                if (_data.characterScale > 0f)
                {
                    createdCharacter.SetScale(_data.characterScale);
                }
            }

            // 생성 직후 1프레임 대기 중 CharacterBase.Update가 실행되면
            // 하단 경계 체크로 즉시 Destroy 될 수 있어 먼저 비활성화합니다.
            created.gameObject.SetActive(false);

            // 생성 직후 최소 1프레임을 확보해 Instantiate 후속 초기화를 마무리합니다.
            yield return null;

            if (created == null || created.gameObject == null)
            {
                yield break;
            }

            if (CutsceneManager.GetCharacter(_data.characterType, _data.characterUid) == null)
            {
                CutsceneManager.AddCharacter(
                    _data.characterType,
                    _data.characterUid,
                    created.gameObject,
                    _data.settleToMapOnCutsceneEnd);
            }
        }

        /// <summary>
        /// 캐릭터를 지정 위치에 배치하고 표시 정책을 반영합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterSpawn)
            {
                return;
            }

            _data = evt.characterSpawn ?? new CharacterSpawnData();
            if (!IsSupportedSpawnType(_data.characterType))
            {
                GcLogger.LogError(
                    "CharacterSpawn는 Monster/Npc 타입만 지원합니다. type: " +
                    _data.characterType + "/ uid: " + _data.characterUid);
                return;
            }

            Transform target = ResolveTargetTransform(_data.characterType, _data.characterUid);
            if (target == null)
            {
                GcLogger.LogError(
                    "CharacterSpawn 대상 캐릭터를 찾을 수 없습니다. type: " +
                    _data.characterType + "/ uid: " + _data.characterUid);
                return;
            }

            CharacterBase characterBase = target.GetComponent<CharacterBase>();
            if (characterBase != null && _data.characterScale > 0f)
            {
                characterBase.SetScale(_data.characterScale);
            }

            Vector2 spawnPosition = ResolveSpawnWorldPosition(_data, target.position);
            target.position = new Vector3(spawnPosition.x, spawnPosition.y, target.position.z);

            if (target.gameObject.activeSelf != _data.spawnVisible)
            {
                target.gameObject.SetActive(_data.spawnVisible);
            }
        }

        /// <summary>
        /// 생성 연출은 프레임 갱신형 로직이 없어 Update 단계에서 처리할 내용이 없습니다.
        /// </summary>
        public void Update()
        {
        }

        /// <summary>
        /// 생성 이벤트는 일회성 처리이므로 Stop 단계에서 추가 정리를 수행하지 않습니다.
        /// </summary>
        public void Stop()
        {
        }

        /// <summary>
        /// 컷신 종료 시 추가 정리는 수행하지 않습니다.
        /// </summary>
        public void End()
        {
        }

        /// <summary>
        /// 현재 이벤트에서 지원하는 생성 대상 타입인지 검사합니다.
        /// </summary>
        /// <param name="characterType">검사할 캐릭터 타입입니다.</param>
        /// <returns>Monster 또는 Npc이면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsSupportedSpawnType(CharacterConstants.Type characterType)
        {
            return characterType == CharacterConstants.Type.Monster ||
                   characterType == CharacterConstants.Type.Npc;
        }

        /// <summary>
        /// 현재 씬/컷신 컨텍스트에서 대상 캐릭터 Transform을 조회합니다.
        /// </summary>
        /// <param name="characterType">조회할 캐릭터 타입입니다.</param>
        /// <param name="characterUid">조회할 캐릭터 uid입니다.</param>
        /// <returns>대상을 찾으면 Transform, 없으면 <see langword="null"/>을 반환합니다.</returns>
        private Transform ResolveTargetTransform(CharacterConstants.Type characterType, int characterUid)
        {
            Transform target = GetTargetTransform(characterType, characterUid);
            if (target != null)
            {
                return target;
            }

            return CutsceneManager.GetCharacter(characterType, characterUid);
        }

        /// <summary>
        /// 설정된 정책에 따라 월드 생성 위치를 계산합니다.
        /// </summary>
        /// <param name="data">생성 위치 계산에 사용할 데이터입니다.</param>
        /// <param name="fallbackPosition">플레이어 참조가 없을 때 대체로 사용할 위치입니다.</param>
        /// <returns>최종 월드 생성 위치입니다.</returns>
        private Vector2 ResolveSpawnWorldPosition(CharacterSpawnData data, Vector3 fallbackPosition)
        {
            if (data == null)
            {
                return new Vector2(fallbackPosition.x, fallbackPosition.y);
            }

            Vector2 offset = data.positionOffset.ToVector2();
            if (data.positionMode == CutsceneCharacterSpawnPositionMode.WorldPosition)
            {
                return data.worldPosition.ToVector2() + offset;
            }

            Transform player = SceneGame.Instance != null && SceneGame.Instance.player != null
                ? SceneGame.Instance.player.transform
                : null;

            if (player == null)
            {
                GcLogger.Log("CharacterSpawn RelativeToPlayer 모드에서 플레이어를 찾지 못해 WorldPosition으로 대체합니다.");
                return data.worldPosition.ToVector2() + offset;
            }

            Vector2 direction = CharacterConstants.FacingToVector2(data.playerRelativeDirection);
            float distance = Mathf.Max(0f, data.playerRelativeDistance);
            Vector2 playerPosition = player.position;
            return playerPosition + direction * distance + offset;
        }
    }
}
