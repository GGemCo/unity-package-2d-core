using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCore
{
    public class MapTileCommon : DefaultMap
    {
        public Camera mainCamera;
        public float extraTileCount = 200f;
        private TilemapRenderer _tilemapRenderer;
        private CutsceneManager _cutsceneManager;

        private Bounds _cullingBounds; // 현재 컬링 범위를 저장할 변수
        private float _mainCameraZ;

        protected override void Awake()
        {
            base.Awake();
            _tilemapRenderer = GetComponent<TilemapRenderer>();
        }

        private void Start()
        {
            if (mainCamera == null && SceneGame.Instance != null)
            {
                mainCamera = SceneGame.Instance.mainCamera;
                _mainCameraZ = mainCamera.transform.position.z;
            }

            _cutsceneManager = SceneGame.Instance.CutsceneManager;
            CalculateCullingBounds();
        }

        public override void Initialize(int uid, string mapName, MapConstants.Type type, MapConstants.SubType subType)
        {
            base.Initialize(uid, mapName, type, subType);
            gameObject.transform.position = new Vector3(0, 0, 0);
        }

        /// <summary>
        /// 컬링 처리 
        /// </summary>
        protected override void CalculateCullingBounds()
        {
            if (ShouldSkipCullingUpdate()) return;

            // 카메라 크기 계산
            float verticalSize = mainCamera.orthographicSize;
            float horizontalSize = verticalSize * mainCamera.aspect;

            // Culling Bounds 설정
            _tilemapRenderer.chunkCullingBounds = new Vector3(
                horizontalSize + extraTileCount,
                verticalSize + extraTileCount,
                0
            );

            // 카메라의 현재 위치를 기준으로 컬링 영역을 갱신
            Vector3 cameraPosition = mainCamera.transform.position;
            _cullingBounds = new Bounds(cameraPosition, new Vector3(
                (horizontalSize + extraTileCount) * 2,
                (verticalSize + extraTileCount) * 2,
                0
            ));

            // 오브젝트 활성화/비활성화 처리
            UpdateObjectActivation(Monsters, _cullingBounds);
            UpdateObjectActivation(Npcs, _cullingBounds);
        }

        /// <summary>
        /// 컬링 갱신을 중단해야 하는지 판별합니다.
        /// 컷신 세션(로딩/준비/재생) 동안 월드 UI 컨테이너가 비활성화될 수 있으므로
        /// 캐릭터 활성 전환을 멈춰 비활성 계층에서 코루틴이 시작되는 문제를 방지합니다.
        /// </summary>
        /// <returns>컬링 갱신을 건너뛰어야 하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ShouldSkipCullingUpdate()
        {
            if (mainCamera == null || _tilemapRenderer == null) return true;
            if (_cutsceneManager == null) return false;

            return _cutsceneManager.IsSessionActive();
        }

        /// <summary>
        /// npc, 몬스터 컬링 처리
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="bounds"></param>
        void UpdateObjectActivation(Dictionary<int, GameObject> objects, Bounds bounds)
        {
            foreach (var info in objects)
            {
                GameObject obj = info.Value;
                if (obj == null) continue;
                
                if (TryApplyExplicitMapVisibilityPolicy(obj))
                {
                    continue;
                }

                if (ShouldKeepHiddenByDefault(obj))
                {
                    if (obj.activeSelf)
                    {
                        obj.GetComponent<Npc>()?.StartFadeOut();
                        obj.GetComponent<Monster>()?.StartFadeOut();
                    }

                    continue;
                }

                // NPC의 Z 축도 고려하여 활성화 상태 확인
                Vector3 position = obj.transform.position;
                bool isActive = bounds.Contains(new Vector3(position.x, position.y, bounds.center.z));

                // 활성화 상태 설정
                if (obj.activeSelf != isActive)
                {
                    if (isActive)
                    {
                        obj.GetComponent<Npc>()?.StartFadeIn();
                        obj.GetComponent<Monster>()?.StartFadeIn();
                    }
                    else
                    {
                        obj.GetComponent<Npc>()?.StartFadeOut();
                        obj.GetComponent<Monster>()?.StartFadeOut();
                    }
                }
            }
        }

        /// <summary>
        /// 모든 캐릭터 활성화
        /// 연출 시작시 사용
        /// </summary>
        public void ActiveAllCharacters()
        {
            foreach (var data in Monsters)
            {
                if (data.Value == null) continue;
                if (TryApplyExplicitMapVisibilityPolicy(data.Value)) continue;
                if (ShouldKeepHiddenByDefault(data.Value)) continue;
                data.Value.GetComponent<Monster>()?.StartFadeIn();
            }

            foreach (var data in Npcs)
            {
                if (data.Value == null) continue;
                if (TryApplyExplicitMapVisibilityPolicy(data.Value)) continue;
                if (ShouldKeepHiddenByDefault(data.Value)) continue;
                data.Value.GetComponent<Npc>()?.StartFadeIn();
            }
        }
        
        /// <summary>
        /// 캐릭터에 명시적으로 지정된 맵 표시 정책을 우선 적용합니다.
        /// DefaultCulling이 아닌 정책은 카메라 컬링과 기본 숨김 규칙보다 우선합니다.
        /// </summary>
        /// <param name="obj">정책을 적용할 캐릭터 오브젝트입니다.</param>
        /// <returns>명시 정책을 처리했으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryApplyExplicitMapVisibilityPolicy(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            CharacterBase character = obj.GetComponent<CharacterBase>();
            if (character == null)
            {
                return false;
            }

            switch (character.MapVisibilityPolicy)
            {
                case MapCharacterVisibilityPolicy.KeepVisible:
                    if (!obj.activeSelf)
                    {
                        obj.GetComponent<Npc>()?.StartFadeIn();
                        obj.GetComponent<Monster>()?.StartFadeIn();
                    }

                    return true;

                case MapCharacterVisibilityPolicy.KeepHidden:
                    if (obj.activeSelf)
                    {
                        obj.GetComponent<Npc>()?.StartFadeOut();
                        obj.GetComponent<Monster>()?.StartFadeOut();
                    }

                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 캐릭터가 "기본 숨김" 정책 대상인지 판별합니다.
        /// 기본 숨김 대상은 컬링/연출 강제 활성화에서도 자동으로 다시 켜지지 않도록 보호합니다.
        /// </summary>
        /// <param name="obj">판별할 캐릭터 오브젝트</param>
        /// <returns>기본 숨김 정책을 유지해야 하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool ShouldKeepHiddenByDefault(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            CharacterBase character = obj.GetComponent<CharacterBase>();
            if (character == null || character.CharacterRegenData == null)
            {
                return false;
            }

            return character.CharacterRegenData.DefaultVisible == false;
        }

        /// <summary>
        /// 퀘스트 상태가 변경되었을때, npc 에 연결된 퀘스트 버튼 업데이트 해주기 
        /// </summary>
        public void OnChangeNpcQuestStatus()
        {
            foreach (var npc in Npcs)
            {
                if (npc.Value == null) continue;
                // NpcQuestButton npcQuestButton = npc.GetComponent<NpcQuestButton>();
                // if (npcQuestButton == null) continue;
                // npcQuestButton.OnChangeQuestStatus();
            }
        }
#if UNITY_EDITOR
        /// <summary>
        /// 카메라 영역, 컬링 영역 시각화
        /// </summary>
        void OnDrawGizmos()
        {
            if (mainCamera == null) return;

            // 카메라의 가로, 세로 뷰 크기 계산
            float verticalSize = mainCamera.orthographicSize;
            float horizontalSize = verticalSize * mainCamera.aspect;

            // 카메라 뷰의 영역 시각화 (초록색)
            Vector3 cameraPosition = mainCamera.transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                cameraPosition,
                new Vector3(horizontalSize * 2 + extraTileCount * 2, verticalSize * 2 + extraTileCount * 2, 0)
            );

            // 컬링 영역 시각화 (빨간색)
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_cullingBounds.center, _cullingBounds.size);
        }
#endif
    }
}
