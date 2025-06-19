using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo.Scripts
{
    public class Item : MonoBehaviour
    {
        public int itemUid;
        public int itemCount;
        private GameObject containerItemName;
        private GameObject objectTagNameItem;
        
        [Header("드랍 애니메이션 속성")]
        public float minDistance = 40f; // 최소 드랍 거리 (픽셀)
        public float maxDistance = 80f; // 최대 드랍 거리 (픽셀)
        public float flightTime = 0.3f; // 비행 시간 (짧을수록 빠르게 떨어짐)
        public float gravity = 25f; // 중력 값
        public float scaleMultiplier = 1.2f; // 최고점에서 크기 증가
        public float minSpacing = 20f; // 아이템 간 최소 간격 (픽셀)
        public float bounceHeight = 5f; // 바운스 효과 크기
        public float rotationSpeed = 180f; // 회전 속도 (도/초)

        private static readonly List<Vector2> DroppedItemPositions = new List<Vector2>(); // 드랍된 아이템 위치 저장
        public Vector2 startPos;
        private Vector2 targetPos;
        private float timeElapsed;
        private Vector2 velocity;
        private Vector3 originalScale;
        private float peakTime;
        private bool isBouncing; // 바운스 여부 체크
        private float bounceTime; // 바운스 지속 시간
        private float rotationDirection; // 랜덤 회전 방향
        private float mapSizeHeight;

        [Tooltip("드랍 후 dropItemDestroyTimeSec 시간 후 destroy")]
        private Coroutine coroutineDropItemDestroy;
        private int dropItemDestroyTimeSec; // 드랍된 후 사라지는 시간(초)

        private Renderer itemRenderer;
        private SpriteRenderer spriteRenderer;
        private CircleCollider2D circleCollider2D;

        private bool isStart;
        private ItemManager itemManager;
        
        private void Awake()
        {
            timeElapsed = 0f;
            isBouncing = false;
            bounceTime = 0.1f;
            originalScale = transform.localScale; // 원래 크기 저장

            itemRenderer = GetComponent<Renderer>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            circleCollider2D = GetComponent<CircleCollider2D>();
            circleCollider2D.enabled = false;
            
            dropItemDestroyTimeSec = AddressableSettingsLoader.Instance.settings.dropItemDestroyTimeSec;
        }

        private void Start()
        {
            itemManager = SceneGame.Instance.ItemManager;
        }
        /// <summary>
        /// 맵에 드랍하기 시작 
        /// </summary>
        public void StartDrop()
        {
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            if (itemUid <= 0) return;
            var info = TableLoaderManager.Instance.TableItem.GetDataByUid(itemUid);
            if (info == null || info.Uid <= 0) return;
            
            itemRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop);
            itemRenderer.sortingOrder = 1;
            timeElapsed = 0f;
            isBouncing = false;
            transform.localScale = Vector3.one;
            spriteRenderer.sprite = Resources.Load<Sprite>(info.ImageItemPath);

            // 특정 반경 내에서 랜덤한 위치 선택 (X, Y 축 모두 분산)
            int maxAttempts = 10; // 겹치지 않도록 최대 시도 횟수
            bool positionValid = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                // 랜덤한 원형 반경 내에서 위치 선택
                Vector2 randomOffset = Random.insideUnitCircle * Random.Range(minDistance, maxDistance);
                Vector2 potentialTargetPos = startPos + randomOffset;

                // 아이템 간 거리 검사 (겹치지 않도록)
                bool tooClose = false;
                foreach (Vector2 pos in DroppedItemPositions)
                {
                    if (Vector2.Distance(potentialTargetPos, pos) < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    targetPos = potentialTargetPos;
                    positionValid = true;
                    break;
                }
            }

            if (!positionValid)
            {
                targetPos = startPos + new Vector2(Random.Range(minDistance, maxDistance), 0f); // 겹칠 경우 대략적인 위치 설정
            }

            DroppedItemPositions.Add(targetPos); // 새로운 아이템 위치 저장

            // 속도 계산 (목표 지점까지 flightTime 내에 도달하도록)
            velocity.x = (targetPos.x - startPos.x) / flightTime;
            velocity.y = (targetPos.y - startPos.y) / flightTime + (0.5f * gravity * flightTime); // 최고점 고려

            peakTime = flightTime / 2; // 최고점 도달 시간

            // 랜덤한 회전 방향 설정
            rotationDirection = Random.Range(-1f, 1f);

            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            mapSizeHeight = size.y;
            
            isStart = true;

            CreateTagName();
        }
        /// <summary>
        /// 아이템 이름 tag 만들기
        /// </summary>
        private void CreateTagName()
        {
            GameObject prefabTagNameItem =
                AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabTextDropItemNameTag);
            if (prefabTagNameItem == null) return;
            if (containerItemName == null)
            {
                containerItemName = SceneGame.Instance.containerDropItemName;
            }
            objectTagNameItem = Instantiate(prefabTagNameItem, containerItemName.transform);
            if (objectTagNameItem == null) return;
            TagNameItem tagNameItem = objectTagNameItem.GetComponent<TagNameItem>();
            if (tagNameItem == null) return;
            tagNameItem.Initialize(gameObject, itemCount);
        }
        /// <summary>
        /// 드랍 애니메이션 처리  
        /// </summary>
        private void Update()
        {
            if (!isStart) return;
            
            timeElapsed += Time.deltaTime;

            if (!isBouncing)
            {
                // 포물선 이동 계산
                float x = startPos.x + velocity.x * timeElapsed;
                float y = startPos.y + (velocity.y * timeElapsed) - (0.5f * gravity * timeElapsed * timeElapsed);
                transform.position = new Vector2(x, y);

                // 최고점 도달 시 크기 증가
                float scaleLerp = Mathf.Lerp(1f, scaleMultiplier, Mathf.Sin((timeElapsed / peakTime) * Mathf.PI));
                transform.localScale = originalScale * scaleLerp;

                // 회전 효과 추가 (자연스러운 낙하)
                transform.Rotate(0, 0, rotationDirection * rotationSpeed * Time.deltaTime);
            }

            // 착지하면 바운스 효과 적용
            if (timeElapsed >= flightTime && !isBouncing)
            {
                StartCoroutine(BounceEffect());
            }
        }
        /// <summary>
        /// 드랍된 후 bounce 애니메이션 처리
        /// </summary>
        /// <returns></returns>
        private IEnumerator BounceEffect()
        {
            isBouncing = true;
            Vector2 groundPos = transform.position;
            Vector2 bouncePos = groundPos + new Vector2(0, bounceHeight);

            // 위로 살짝 튀기기
            float elapsed = 0f;
            while (elapsed < bounceTime)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector2.Lerp(groundPos, bouncePos, Mathf.Sin((elapsed / bounceTime) * Mathf.PI));
                yield return null;
            }

            // 원래 위치로 복귀
            elapsed = 0f;
            while (elapsed < bounceTime)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector2.Lerp(bouncePos, groundPos, elapsed / bounceTime);
                yield return null;
            }

            // 최종적으로 원래 크기로 복귀
            OnEnd();
        }
        /// <summary>
        /// 드랍 완료
        /// </summary>
        private void OnEnd()
        {
            isStart = false;
            transform.localScale = originalScale;
            isBouncing = false;
            // 드랍된 후에는 캐릭터 layer 로 적용한다.
            itemRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.Character);
            itemRenderer.sortingOrder = MathHelper.GetSortingOrder(mapSizeHeight, transform.position.y);
            circleCollider2D.enabled = true;

            coroutineDropItemDestroy = StartCoroutine(CheckDestroyTime());
        }
        /// <summary>
        /// 플레이어가 아이템을 먹거나 맵에서 없어졌을때 
        /// </summary>
        private void OnDisable()
        {
            StopCoroutineDropItemDestroy();
            circleCollider2D.enabled = false;
            if (objectTagNameItem != null)
            {
                objectTagNameItem.SetActive(false);
            }
        }
        /// <summary>
        /// 플레이어가 아이템을 먹 후, 사라지는 시간이 다 되었을때 처리
        /// </summary>
        public void Reset()
        {
            itemUid = 0;
            gameObject.SetActive(false);
            itemManager.AddPoolDropItem(this);
        }

        IEnumerator CheckDestroyTime()
        {
            yield return new WaitForSeconds(dropItemDestroyTimeSec);
            Reset();
        }
        /// <summary>
        /// 시간 되면 자동으로 파괴되는 코루틴 정지 
        /// </summary>
        private void StopCoroutineDropItemDestroy()
        {
            if (coroutineDropItemDestroy == null) return;
            StopCoroutine(coroutineDropItemDestroy);
        }
    }
}